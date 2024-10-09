using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Applies bloom to the last FBO.
    /// </summary>
    public class VPRC_BloomPass : ViewportRenderCommand
    {
        public const string BloomBlur1FBOName = "BloomBlurFBO1";
        public const string BloomBlur2FBOName = "BloomBlurFBO2";
        public const string BloomBlur4FBOName = "BloomBlurFBO4";
        public const string BloomBlur8FBOName = "BloomBlurFBO8";
        public const string BloomBlur16FBOName = "BloomBlurFBO16";

        public BoundingRectangle BloomRect16;
        public BoundingRectangle BloomRect8;
        public BoundingRectangle BloomRect4;
        public BoundingRectangle BloomRect2;
        //public BoundingRectangle BloomRect1;

        /// <summary>
        /// The name of the FBO that will be used as input for the bloom pass.
        /// </summary>
        public string InputFBOName { get; set; } = "BloomInputFBO";

        /// <summary>
        /// This is the texture that will contain the final bloom output.
        /// </summary>
        public string BloomOutputTextureName { get; set; } = "BloomOutputTexture";

        public void SetTargetFBONames(string inputFBOName, string outputTextureName)
        {
            InputFBOName = inputFBOName;
            BloomOutputTextureName = outputTextureName;
        }

        private uint _lastWidth = 0u;
        private uint _lastHeight = 0u;

        private void ResizeFBOs(uint width, uint height)
        {
            width = Math.Max(1u, width);
            height = Math.Max(1u, height);

            Debug.Out($"Resizing bloom pass FBOs to {width} x {height}.");

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

            var outputTexture = Pipeline.GetTexture<XRTexture2D>(BloomOutputTextureName);
            if (outputTexture is null)
            {
                Debug.Out($"Failed to resize bloom pass FBOs. Output texture not found.");
                return;
            }

            outputTexture.Resize(width, height);
        }

        private void RegenerateFBOs(uint width, uint height)
        {
            width = Math.Max(1u, width);
            height = Math.Max(1u, height);

            Debug.Out($"Regenerating bloom pass FBOs at {width} x {height}.");

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

            var outputTexture = XRTexture2D.CreateFrameBufferTexture(
                width,
                height,
                EPixelInternalFormat.Rgb8,
                EPixelFormat.Rgb,
                EPixelType.UnsignedByte);
            outputTexture.Name = BloomOutputTextureName;
            outputTexture.MagFilter = ETexMagFilter.Linear;
            outputTexture.MinFilter = ETexMinFilter.LinearMipmapLinear;
            outputTexture.UWrap = ETexWrapMode.ClampToEdge;
            outputTexture.VWrap = ETexWrapMode.ClampToEdge;
            Pipeline.SetTexture(outputTexture);

            XRMaterial bloomBlurMat = new
            (
                [new ShaderFloat(0.0f, "Ping"), new ShaderInt(0, "LOD")],
                [outputTexture],
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

            var blur1 = new XRQuadFrameBuffer(bloomBlurMat) { Name = BloomBlur1FBOName };
            var blur2 = new XRQuadFrameBuffer(bloomBlurMat) { Name = BloomBlur2FBOName };
            var blur4 = new XRQuadFrameBuffer(bloomBlurMat) { Name = BloomBlur4FBOName };
            var blur8 = new XRQuadFrameBuffer(bloomBlurMat) { Name = BloomBlur8FBOName };
            var blur16 = new XRQuadFrameBuffer(bloomBlurMat) { Name = BloomBlur16FBOName };

            blur1.SetRenderTargets((outputTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));
            blur2.SetRenderTargets((outputTexture, EFrameBufferAttachment.ColorAttachment0, 1, -1));
            blur4.SetRenderTargets((outputTexture, EFrameBufferAttachment.ColorAttachment0, 2, -1));
            blur8.SetRenderTargets((outputTexture, EFrameBufferAttachment.ColorAttachment0, 3, -1));
            blur16.SetRenderTargets((outputTexture, EFrameBufferAttachment.ColorAttachment0, 4, -1));

            Pipeline.SetFBO(blur1);
            Pipeline.SetFBO(blur2);
            Pipeline.SetFBO(blur4);
            Pipeline.SetFBO(blur8);
            Pipeline.SetFBO(blur16);
        }

        protected override void Execute()
        {
            var inputFBO = Pipeline.GetFBO<XRQuadFrameBuffer>(InputFBOName);
            if (inputFBO is null)
                return;

            var blur16 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur16FBOName);
            var blur8 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur8FBOName);
            var blur4 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur4FBOName);
            var blur2 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur2FBOName);
            var blur1 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur1FBOName);

            if (blur16 is null ||
                blur8 is null ||
                blur4 is null ||
                blur2 is null ||
                blur1 is null)
            {
                RegenerateFBOs(inputFBO.Width, inputFBO.Height);
                blur16 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur16FBOName);
                blur8 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur8FBOName);
                blur4 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur4FBOName);
                blur2 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur2FBOName);
                blur1 = Pipeline.GetFBO<XRQuadFrameBuffer>(BloomBlur1FBOName);
            }
            else if (inputFBO.Width != _lastWidth ||
                inputFBO.Height != _lastHeight)
                RegenerateFBOs(inputFBO.Width, inputFBO.Height);

            using (blur1!.BindForWriting())
                inputFBO!.Render();

            var tex = Pipeline.GetTexture<XRTexture2D>(BloomOutputTextureName);
            tex!.Bind();
            tex.GenerateMipmapsGPU();

            BloomScaledPass(blur16!, BloomRect16, 4);
            BloomScaledPass(blur8!, BloomRect8, 3);
            BloomScaledPass(blur4!, BloomRect4, 2);
            BloomScaledPass(blur2!, BloomRect2, 1);
            //Don't blur original image, barely makes a difference to result
        }
        private static void BloomScaledPass(XRQuadFrameBuffer fbo, BoundingRectangle rect, int mipmap)
        {
            using (fbo.BindForWriting())
            {
                using (Pipeline.State.PushRenderArea(rect))
                {
                    BloomBlur(fbo, mipmap, 0.0f);
                    BloomBlur(fbo, mipmap, 1.0f);
                }
            }
        }
        private static void BloomBlur(XRQuadFrameBuffer fbo, int mipmap, float dir)
        {
            fbo.Material.Parameter<ShaderFloat>(0)!.Value = dir;
            fbo.Material.Parameter<ShaderInt>(1)!.Value = mipmap;
            fbo.Render();
        }
    }
}
