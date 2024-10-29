using SkiaSharp;
using XREngine.Core.Files;
using XREngine.Data;
using SharpFont;

namespace XREngine.Rendering
{
    [XR3rdPartyExtensions("otf", "ttf")]
    public class FontGlyphSet : XRAsset
    {
        public List<GlyphInfo>? GlyphInfos { get; set; }

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
                {
                    string s = char.ConvertFromUtf32((int)codepoint);
                    characters.Add(s);
                }
            }

            SKTypeface typeface = SKTypeface.FromFile(filePath);
            GenerateFontAtlas(typeface, characters, Path.Combine(folder, $"{name}.png"));
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
                characterSet.Add(0);
                code = face.GetNextChar(0, out glyphIndex);
            }

            return characterSet;
        }

        private void GenerateFontAtlas(SKTypeface typeface, List<string> characters, string outputAtlasPath)
        {
            // List to hold glyph data
            List<GlyphInfo> glyphInfos = [];
            List<SKBitmap> glyphBitmaps = [];

            // Define text size (adjust as needed)
            float textSize = 64f;

            // Create a paint object
            using SKPaint paint = new()
            {
                Typeface = typeface,
                TextSize = textSize,
                IsAntialias = true,
                Color = SKColors.White,
                IsStroke = false
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
                SKRect glyphBounds = bounds[0];

                int width = (int)Math.Ceiling(glyphBounds.Width);
                int height = (int)Math.Ceiling(glyphBounds.Height);

                if (width == 0 || height == 0)
                {
                    width = 1;
                    height = 1;
                }

                SKBitmap bitmap = new(width, height);
                using (SKCanvas canvas = new(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    float x = -glyphBounds.Left;
                    float y = -glyphBounds.Top;
                    canvas.DrawText(character, x, y, paint);
                }

                glyphBitmaps.Add(bitmap);

                GlyphInfo info = new()
                {
                    Character = character,
                    Width = width,
                    Height = height,
                    BearingX = glyphBounds.Left,
                    BearingY = glyphBounds.Top,
                    AdvanceX = widths[0]
                };

                glyphInfos.Add(info);
            }

            // Pack glyphs into an atlas
            int glyphsPerRow = (int)Math.Ceiling(Math.Sqrt(glyphBitmaps.Count));
            int maxGlyphWidth = 0;
            int maxGlyphHeight = 0;

            foreach (var info in glyphInfos)
            {
                if (info.Width > maxGlyphWidth)
                    maxGlyphWidth = info.Width;

                if (info.Height > maxGlyphHeight)
                    maxGlyphHeight = info.Height;
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

                    // Update glyph info
                    glyphInfos[i].AtlasX = x;
                    glyphInfos[i].AtlasY = y;
                }
            }

            // Save the atlas texture
            using (var image = SKImage.FromBitmap(atlasBitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(outputAtlasPath))
            {
                data.SaveTo(stream);
            }

            GlyphInfos = glyphInfos;

            // Output glyph data
            //using (StreamWriter writer = new("glyphs.txt"))
            //{
            //    writer.WriteLine("Char\tUnicode\tX\tY\tWidth\tHeight\tBearingX\tBearingY\tAdvanceX");
            //    foreach (var info in glyphInfos)
            //    {
            //        int codepoint = char.ConvertToUtf32(info.Character, 0);
            //        writer.WriteLine($"{info.Character}\tU+{codepoint:X4}\t{info.AtlasX}\t{info.AtlasY}\t{info.Width}\t{info.Height}\t{info.BearingX}\t{info.BearingY}\t{info.AdvanceX}");
            //    }
            //}

            foreach (var bitmap in glyphBitmaps)
                bitmap.Dispose();
        }

        public class GlyphInfo
        {
            public string? Character;
            public int AtlasX;
            public int AtlasY;
            public int Width;
            public int Height;
            public float BearingX;
            public float BearingY;
            public float AdvanceX;
        }
    }
}