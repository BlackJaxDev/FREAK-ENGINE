using SharpFont;
using SkiaSharp;
using System.Numerics;
using XREngine.Core.Files;
using XREngine.Data;
using XREngine.Data.Vectors;

namespace XREngine.Rendering
{
    [XR3rdPartyExtensions("otf", "ttf")]
    public class FontGlyphSet : XRAsset
    {
        private Dictionary<string, Glyph>? _glyphs;
        private XRTexture2D? _atlas;
        private List<string> _characters = [];

        public List<string> Characters
        {
            get => _characters;
            set => SetField(ref _characters, value);
        }
        public Dictionary<string, Glyph>? Glyphs
        {
            get => _glyphs;
            set => SetField(ref _glyphs, value);
        }
        public XRTexture2D? Atlas
        {
            get => _atlas;
            set => SetField(ref _atlas, value);
        }

        public override void Load3rdParty(string filePath)
        {
            string folder = Path.GetDirectoryName(filePath)!;
            string name = Path.GetFileNameWithoutExtension(filePath);

            // Load the font using SharpFont
            using Library lib = new();
            using Face face = new(lib, filePath);

            // Get the list of supported characters
            HashSet<uint> characterSet = GetSupportedCharacters(face);

            // Convert character set to list of strings (to handle code points beyond BMP)
            List<string> characters = [];
            foreach (uint codepoint in characterSet)
            {
                // Exclude control characters and invalid code points
                if (codepoint >= 0x20 && codepoint <= 0x10FFFF)
                    characters.Add(char.ConvertFromUtf32((int)codepoint));
            }
            Characters = characters;
            SKTypeface typeface = SKTypeface.FromFile(filePath);
            GenerateFontAtlas(typeface, characters, Path.Combine(folder, $"{name}.png"), 100.0f);
        }

        public static HashSet<uint> GetSupportedCharacters(Face face)
        {
            HashSet<uint> characterSet = [];

            // Select Unicode charmap
            face.SetCharmap(face.CharMaps[0]);

            // Iterate over all characters
            uint code = face.GetFirstChar(out uint glyphIndex);
            while (glyphIndex != 0)
            {
                characterSet.Add(code);
                code = face.GetNextChar(code, out glyphIndex);
            }
            return characterSet;
        }

        public void GenerateFontAtlas(
            SKTypeface typeface,
            List<string> characters,
            string outputAtlasPath,
            float textSize,
            float pad = 5.0f,
            SKPaintStyle style = SKPaintStyle.Fill,
            float strokeWidth = 0.0f)
        {
            // List to hold glyph data
            List<(string character, Glyph info)> glyphInfos = [];
            List<SKBitmap> glyphBitmaps = [];

            // Create a paint object
            using SKPaint paint = new()
            {
                Typeface = typeface,
                TextSize = textSize,
                IsAntialias = true,
                Color = SKColors.White,
                Style = style,
                StrokeWidth = strokeWidth,
            };

            // Process each character
            foreach (string character in characters)
            {
                // Get glyph indices
                ushort[] glyphs = paint.GetGlyphs(character);
                if (glyphs.Length == 0 || glyphs[0] == 0)
                {
                    // Skip characters without glyphs
                    continue;
                }

                float[] widths = new float[glyphs.Length];
                paint.GetGlyphWidths(character, out SKRect[] bounds);
                if (bounds.Length > 1)
                    Debug.LogWarning($"Multiple glyphs for character '{character}'");

                SKRect glyphBounds = bounds[0];

                int width = (int)Math.Ceiling(glyphBounds.Width);
                int height = (int)Math.Ceiling(glyphBounds.Height);

                if (width == 0 || height == 0)
                {
                    width = 1;
                    height = 1;
                }

                widths[0] = width;

                SKBitmap bitmap = new(width, height);
                using (SKCanvas canvas = new(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    float x = -glyphBounds.Left;
                    float y = -glyphBounds.Top;
                    canvas.DrawText(character, x, y, paint);
                }

                glyphBitmaps.Add(bitmap);
                glyphInfos.Add((character, new(
                    new IVector2(width, height),
                    new Vector2(-glyphBounds.Left, -glyphBounds.Top),
                    widths[0] + pad)));
            }

            // Pack glyphs into an atlas
            int glyphsPerRow = (int)Math.Ceiling(Math.Sqrt(glyphBitmaps.Count));
            int maxGlyphWidth = 0;
            int maxGlyphHeight = 0;

            foreach (var g in glyphInfos)
            {
                if (g.info.Size.X > maxGlyphWidth)
                    maxGlyphWidth = g.info.Size.X;

                if (g.info.Size.Y > maxGlyphHeight)
                    maxGlyphHeight = g.info.Size.Y;
            }

            int atlasWidth = maxGlyphWidth * glyphsPerRow;
            int numRows = (int)Math.Ceiling((double)glyphBitmaps.Count / glyphsPerRow);
            int atlasHeight = maxGlyphHeight * numRows;

            using SKBitmap atlasBitmap = new(atlasWidth, atlasHeight);
            using (SKCanvas atlasCanvas = new(atlasBitmap))
            {
                atlasCanvas.Clear(SKColors.Transparent);

                // Draw each glyph onto the atlas
                for (int i = 0; i < glyphBitmaps.Count; i++)
                {
                    int row = i / glyphsPerRow;
                    int col = i % glyphsPerRow;

                    int x = col * maxGlyphWidth;
                    int y = row * maxGlyphHeight;

                    SKBitmap glyphBitmap = glyphBitmaps[i];
                    atlasCanvas.DrawBitmap(glyphBitmap, x, y);

                    glyphInfos[i].info.Position = new Vector2(x, y);
                }
            }

            // Save the atlas texture
            using (var image = SKImage.FromBitmap(atlasBitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(outputAtlasPath))
            {
                data.SaveTo(stream);
            }

            Atlas = new XRTexture2D(outputAtlasPath);
            Glyphs = glyphInfos.ToDictionary(g => g.character, g => g.info);

            foreach (var bitmap in glyphBitmaps)
                bitmap.Dispose();
        }

        public void GetQuads(string? str, out List<(Vector4 transform, Vector4 uvs)> quads)
            => GetQuads(str, out quads, Vector2.Zero);
        public void GetQuads(string? str, out List<(Vector4 transform, Vector4 uvs)> quads, Vector2 offset)
        {
            if (Glyphs is null)
                throw new InvalidOperationException("Glyphs are not initialized.");

            if (Atlas is null)
                throw new InvalidOperationException("Atlas is not initialized.");

            GetQuads(str, Glyphs, new IVector2((int)Atlas.Width, (int)Atlas.Height), out quads, offset);
        }

        private static void GetQuads(
            string? str,
            Dictionary<string, Glyph> glyphs,
            IVector2 atlasSize,
            out List<(Vector4 transform, Vector4 uvs)> quads,
            Vector2 offset)
        {
            quads = [];
            if (str is null)
                return;

            float xOffset = offset.X;
            foreach (char ch in str)
            {
                string character = ch.ToString();
                if (!glyphs.ContainsKey(character))
                {
                    // Handle missing glyphs (e.g., skip or substitute)
                    continue;
                }

                Glyph glyph = glyphs[character];
                float translateX = xOffset + glyph.Bearing.X;
                float translateY = offset.Y + glyph.Bearing.Y;
                float scaleX = glyph.Size.X;
                float scaleY = -glyph.Size.Y;

                Vector4 transform = new(
                    translateX,
                    translateY,
                    scaleX,
                    scaleY);

                float u0 = glyph.Position.X / atlasSize.X;
                float v0 = glyph.Position.Y / atlasSize.Y;
                float u1 = (glyph.Position.X + glyph.Size.X) / atlasSize.X;
                float v1 = (glyph.Position.Y + glyph.Size.Y) / atlasSize.Y;

                // Add UVs in the order matching the quad vertices
                // Assuming quad vertices are defined in this order:
                // Bottom-left (0, 0)
                // Bottom-right (1, 0)
                // Top-right (1, 1)
                // Top-left (0, 1)
                Vector4 uvs = new(u0, v0, u1, v1); // Bottom-left to Top-right
                
                quads.Add((transform, uvs));

                xOffset += glyph.AdvanceX;
            }
        }

        public class Glyph
        {
            public Vector2 Position;
            public IVector2 Size;
            public Vector2 Bearing;
            public float AdvanceX;

            public Glyph() { }
            public Glyph(IVector2 size, Vector2 bearing, float advanceX)
            {
                Size = size;
                Bearing = bearing;
                AdvanceX = advanceX;
            }
        }
    }
}