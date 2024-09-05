//using System;
//using System.ComponentModel;
//using System.Drawing.Text;
//using XREngine.ComponentModel;
//using XREngine.Core.Maths.Transforms;
//using XREngine.Core.Shapes;
//using XREngine.Rendering.Cameras;
//using XREngine.Rendering.Models.Materials;
//using XREngine.Rendering.Text;

//namespace XREngine.Rendering.UI
//{
//    /// <summary>
//    /// Renders text to a texture and binds the texure to a material rendered on a quad.
//    /// If any text in the drawer is modified, the portion of the teture it inhabits will be redrawn.
//    /// This component is best for text that does not change often or has a LOT of characters in it.
//    /// </summary>
//    public class UITextRasterComponent : UIInteractableComponent, IPreRendered
//    {
//        public UITextRasterComponent() : base(XRMaterial.CreateUnlitTextureMaterialForward(MakeDrawSurface()), true) => Init();
//        public UITextRasterComponent(XRMaterial material, bool appendDrawSurfaceTexture = true) : base(material, true)
//        {
//            if (appendDrawSurfaceTexture)
//                material.Textures.Add(MakeDrawSurface());

//            Init();
//        }
//        private void Init()
//        {
//            _textDrawer = new TextRasterizer();
//            _textDrawer.Invalidated += OnInvalidated;

//            RenderCommand.RenderPass = ERenderPass.TransparentForward;
//            RenderCommand.Mesh.Material.RenderParams = new RenderingParameters(true);
//        }

//        public static XRTexture2D MakeDrawSurface()
//            => new XRTexture2D("DrawSurface", 1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
//            {
//                MagFilter = ETexMagFilter.Nearest,
//                MinFilter = ETexMinFilter.Nearest,
//                UWrap = ETexWrapMode.ClampToEdge,
//                VWrap = ETexWrapMode.ClampToEdge,
//                Resizable = true
//            };

//        private void OnInvalidated(bool forceFullRedraw)
//        {
//            Invalidated = true;
//            ForceFullRedraw = forceFullRedraw;
//        }

//        [TSerialize(nameof(TextQuality))]
//        private TextRenderingHint _textQuality = TextRenderingHint.AntiAlias;
//        [TSerialize(nameof(TextureResolutionMultiplier))]
//        private Vector2 _texRes = new Vector2(1.0f);
//        [TSerialize(nameof(TextDrawer))]
//        private TextRasterizer _textDrawer;

//        public XRTexture2D TextTexture => Texture<XRTexture2D>(0);
//        public TextRasterizer TextDrawer => _textDrawer;

//        public Vector2 TextureResolutionMultiplier
//        {
//            get => _texRes;
//            set
//            {
//                _texRes = value;

//                Invalidated = true;
//                ForceFullRedraw = true;
//                InvalidateLayout();
//            }
//        }
//        public TextRenderingHint TextQuality
//        {
//            get => _textQuality;
//            set
//            {
//                _textQuality = value;

//                Invalidated = true;
//                ForceFullRedraw = true;
//            }
//        }

//        public bool ForceFullRedraw { get; set; } = true;
//        public IVector2? NeedsResize { get; set; } = null;
//        public bool Invalidated { get; set; } = false;

//        protected override void OnResizeLayout(BoundingRectangleF parentRegion)
//        {
//            base.OnResizeLayout(parentRegion);

//            int w = (int)(ActualWidth * TextureResolutionMultiplier.X);
//            int h = (int)(ActualHeight * TextureResolutionMultiplier.Y);
//            if (w != TextTexture.Width || h != TextTexture.Height)
//            {
//                NeedsResize = new IVector2(w, h);
//                Invalidated = true;
//                ForceFullRedraw = true;
//            }
//        }
//        [Browsable(false)]
//        public bool PreRenderEnabled => NeedsResize != null || Invalidated;

//        public void PreRenderUpdate(ICamera camera) { }
//        public void PreRender(Viewport viewport, ICamera camera) { }
//        public void PreRenderSwap()
//        {
//            if (NeedsResize != null)
//            {
//                TextTexture.Resize(NeedsResize.Value.X, NeedsResize.Value.Y);
//                NeedsResize = null;
//            }

//            if (Invalidated)
//            {
//                TextDrawer.Draw(TextTexture, TextureResolutionMultiplier, TextQuality, ForceFullRedraw);
//                Invalidated = false;
//            }
//        }
//    }
//}
