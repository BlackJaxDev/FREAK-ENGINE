//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Numerics;

//namespace XREngine.Rendering.UI
//{
//    public enum EGDICharSet : byte
//    {
//        ANSI = 0,
//        DEFAULT = 1,
//        SYMBOL = 2,
//        SHIFTJIS = 128,
//        HANGEUL = 129,
//        HANGUL = 129,
//        GB2312 = 134,
//        CHINESEBIG5 = 136,
//        OEM = 255,
//        JOHAB = 130,
//        HEBREW = 177,
//        ARABIC = 178,
//        GREEK = 161,
//        TURKISH = 162,
//        VIETNAMESE = 163,
//        THAI = 222,
//        EASTEUROPE = 238,
//        RUSSIAN = 204,
//        MAC = 77,
//        BALTIC = 186,
//    }

//    public class UITextQuadComponent : UIInteractableComponent
//    {
//        private Font _font;
//        private string _text;

//        public string Text
//        {
//            get => _text;
//            set 
//            {
//                if (Set(ref _text, value))
//                {
//                    InvalidateLayout();
//                }
//            }
//        }

//        public Font Font
//        {
//            get => _font;
//            set
//            {
//                if (Set(ref _font, value))
//                {
//                    GenerateFontTexture();
//                    InvalidateLayout();
//                }
//            }
//        }

//        public TextRasterizer TextDrawer { get; }

//        //Region = X, Y, W, H in atlas texture for glyph to use
//        //UV = X, Y, W, H to translate and scale quads
//        private readonly List<(Vector4 Region, Vector4 UV)> _glyphs = new List<(Vector4 Region, Vector4 UV)>();

//        public const int MaxTextLength = 256;
//        public const string AllGlyphs = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=`~,.<>/?\\|{}[];:\"'";
        
//        private void GenerateFontTexture()
//        {
//            _glyphs.Clear();

//            //Vector2 atlasSize = new Vector2(256.0f);

//            //TODO: get graphics from atlas bitmap
//            using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
//            float height = Font.GetHeight(g);
//            //TODO: dynamic wrap and scale all glyphs to fit in atlas texture
//            SizeF size = g.MeasureString(AllGlyphs, Font);
//            StringFormat fmt = new StringFormat(StringFormatFlags.NoClip);
//            var regions = g.MeasureCharacterRanges(AllGlyphs, Font, new RectangleF(new PointF(), size), fmt);
//            foreach (var region in regions)
//            {
//                var rect = region.GetBounds(g);
//            }
//        }

//        private RenderCommandMesh3D Command { get; }
//        private XRMeshRenderer CharQuad => Command.Mesh;

//        public UITextQuadComponent() : base(null)
//        {
//            TextDrawer = new TextRasterizer();

//            var mat = XRMaterial.CreateUnlitAlphaTextureMaterialForward(
//                new XRTexture2D("CharTex", 64, 64, PixelFormat.Format32bppArgb));

//            var data = TMesh.Create(
//                XRMeshDescriptor.PosTex(),
//                TVertexQuad.PosZ(1.0f, true, 0.0f, true));

//            //TODO: resize buffer length if text length reaches capacity
//            //Or use multiple primitive managers all set to same fixed size buffer
//            int textLengthCapacity = MaxTextLength;
//            var positionsBuffer = data.AddBuffer(new Vector4[textLengthCapacity], new VertexAttribInfo(EBufferType.Aux, 0), false, false, true, 1);
//            var textureRegionsBuffer = data.AddBuffer(new Vector4[textLengthCapacity], new VertexAttribInfo(EBufferType.Aux, 1), false, false, true, 1);

//            positionsBuffer.Location = 1;
//            textureRegionsBuffer.Location = 2;

//            Command = new RenderCommandMesh3D(ERenderPass.TransparentForward) { Mesh = new MeshRenderer(data, mat) };
//        }

//        protected override void OnResizeLayout(BoundingRectangleF parentRegion)
//        {
//            base.OnResizeLayout(parentRegion);

//            int totalChars = 0;
//            if (Text != null)
//                for (int i = 0; i < Text.Length; ++i)
//                {

//                }

//            CharQuad.Instances = totalChars;
//        }

//        public override void AddRenderables(RenderPasses passes, ICamera camera)
//        {
//            base.AddRenderables(passes, camera);

//            passes.Add(Command);
//        }
//    }
//}
