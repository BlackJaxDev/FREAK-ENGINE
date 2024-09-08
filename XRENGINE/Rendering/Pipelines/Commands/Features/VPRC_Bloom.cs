using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Applies bloom to the last FBO.
    /// </summary>
    public class VPRC_Bloom(XRRenderPipeline pipeline) : ViewportRenderCommand(pipeline)
    {
        public XRQuadFrameBuffer? BloomBlurFBO1 { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO2 { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO4 { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO8 { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO16 { get; private set; }

        public BoundingRectangle BloomRect16;
        public BoundingRectangle BloomRect8;
        public BoundingRectangle BloomRect4;
        public BoundingRectangle BloomRect2;
        //public BoundingRectangle BloomRect1;

        public XRTexture2D? BloomOutputTexture { get; private set; }

        /// <summary>
        /// The name of the FBO that will be used as input for the bloom pass.
        /// </summary>
        public string InputFBOName { get; set; } = "BloomInputFBO";

        /// <summary>
        /// This is the texture that will contain the final bloom output.
        /// </summary>
        public string BloomOutputTextureName { get; set; } = "BloomOutputTexture";

        private uint _lastWidth = 0;
        private uint _lastHeight = 0;

        public override void DestroyFBOs()
        {
            BloomBlurFBO1?.Destroy();
            BloomBlurFBO1 = null;

            BloomBlurFBO2?.Destroy();
            BloomBlurFBO2 = null;

            BloomBlurFBO4?.Destroy();
            BloomBlurFBO4 = null;

            BloomBlurFBO8?.Destroy();
            BloomBlurFBO8 = null;

            BloomBlurFBO16?.Destroy();
            BloomBlurFBO16 = null;
        }

        private void RegenerateFBOs(uint width, uint height)
        {
            _lastWidth = width;
            _lastHeight = height;

            BloomRect16.Width = (int)(width * 0.0625f);
            BloomRect16.Height = (int)(height * 0.0625f);
            BloomRect8.Width = (int)(width * 0.125f);
            BloomRect8.Height = (int)(height * 0.125f);
            BloomRect4.Width = (int)(width * 0.25f);
            BloomRect4.Height = (int)(height * 0.25f);
            BloomRect2.Width = (int)(width * 0.5f);
            BloomRect2.Height = (int)(height * 0.5f);
            //BloomRect1.Width = width;
            //BloomRect1.Height = height;

            BloomOutputTexture = XRTexture2D.CreateFrameBufferTexture(
                width,
                height,
                EPixelInternalFormat.Rgb8,
                EPixelFormat.Rgb,
                EPixelType.UnsignedByte);
            BloomOutputTexture.Name = BloomOutputTextureName;
            BloomOutputTexture.MagFilter = ETexMagFilter.Linear;
            BloomOutputTexture.MinFilter = ETexMinFilter.LinearMipmapLinear;
            BloomOutputTexture.UWrap = ETexWrapMode.ClampToEdge;
            BloomOutputTexture.VWrap = ETexWrapMode.ClampToEdge;

            Pipeline.SetTexture(BloomOutputTexture);

            XRMaterial bloomBlurMat = new
            (
                [new ShaderFloat(0.0f, "Ping"), new ShaderInt(0, "LOD")],
                [BloomOutputTexture],
                XRShader.EngineShader(Path.Combine(SceneShaderPath, "BloomBlur.fs"), EShaderType.Fragment))
            {
                RenderOptions = new RenderingParameters()
                {
                    DepthTest =
                    {
                        Enabled = ERenderParamUsage.Unchanged,
                        UpdateDepth = false,
                        Function = EComparison.Always,
                    }
                }
            };

            BloomBlurFBO1 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO2 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO4 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO8 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO16 = new XRQuadFrameBuffer(bloomBlurMat);

            BloomBlurFBO1.SetRenderTargets((BloomOutputTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));
            BloomBlurFBO2.SetRenderTargets((BloomOutputTexture, EFrameBufferAttachment.ColorAttachment0, 1, -1));
            BloomBlurFBO4.SetRenderTargets((BloomOutputTexture, EFrameBufferAttachment.ColorAttachment0, 2, -1));
            BloomBlurFBO8.SetRenderTargets((BloomOutputTexture, EFrameBufferAttachment.ColorAttachment0, 3, -1));
            BloomBlurFBO16.SetRenderTargets((BloomOutputTexture, EFrameBufferAttachment.ColorAttachment0, 4, -1));

            Pipeline.SetFBO("BloomBlur1", BloomBlurFBO1);
            Pipeline.SetFBO("BloomBlur2", BloomBlurFBO2);
            Pipeline.SetFBO("BloomBlur4", BloomBlurFBO4);
            Pipeline.SetFBO("BloomBlur8", BloomBlurFBO8);
            Pipeline.SetFBO("BloomBlur16", BloomBlurFBO16);
        }

        protected override void Execute()
        {
            var inputFBO = Pipeline.GetFBO<XRQuadFrameBuffer>(InputFBOName) 
                ?? throw new Exception($"FBO {InputFBOName} not found.");

            if (inputFBO.Width != _lastWidth ||
                inputFBO.Height != _lastHeight)
                RegenerateFBOs(inputFBO.Width, inputFBO.Height);

            using (BloomBlurFBO1!.BindForWriting())
                inputFBO!.Render();

            var tex = BloomOutputTexture;
            tex!.Bind();
            tex.GenerateMipmapsGPU();

            BloomScaledPass(BloomBlurFBO16!, BloomRect16, 4);
            BloomScaledPass(BloomBlurFBO8!, BloomRect8, 3);
            BloomScaledPass(BloomBlurFBO4!, BloomRect4, 2);
            BloomScaledPass(BloomBlurFBO2!, BloomRect2, 1);
            //Don't blur original image, barely makes a difference to result
        }
        private void BloomScaledPass(XRQuadFrameBuffer fbo, BoundingRectangle rect, int mipmap)
        {
            using (fbo.BindForWriting())
            {
                using (Engine.Rendering.State.PushRenderArea(rect))
                {
                    BloomBlur(fbo, mipmap, 0.0f);
                    BloomBlur(fbo, mipmap, 1.0f);
                }
            }
        }
        private static void BloomBlur(XRQuadFrameBuffer fbo, int mipmap, float dir)
        {
            fbo.Material.Parameter<ShaderFloat>(0).Value = dir;
            fbo.Material.Parameter<ShaderInt>(1).Value = mipmap;
            fbo.Render();
        }
    }
}
