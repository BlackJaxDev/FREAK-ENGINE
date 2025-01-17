using System.Numerics;
using XREngine.Data;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Generates the necessary textures and framebuffers for SSAO in the render pipeline depending on the current render area.
    /// </summary>
    /// <param name="pipeline"></param>
    public class VPRC_SSAO : ViewportRenderCommand
    {
        public string SSAONoiseTextureName { get; set; } = "SSAONoiseTexture";
        public string SSAOIntensityTextureName { get; set; } = "SSAOFBOTexture";
        public string SSAOFBOName { get; set; } = "SSAOFBO";
        public string SSAOBlurFBOName { get; set; } = "SSAOBlurFBO";
        public string GBufferFBOFBOName { get; set; } = "GBufferFBO";

        const int DefaultSamples = 128;
        const uint DefaultNoiseWidth = 4u, DefaultNoiseHeight = 4u;
        const float DefaultMinSampleDist = 0.1f, DefaultMaxSampleDist = 1.0f;

        public Vector2[]? Noise { get; private set; }
        public Vector3[]? Kernel { get; private set; }

        public int Samples { get; private set; } = DefaultSamples;
        public uint NoiseWidth { get; private set; } = DefaultNoiseWidth;
        public uint NoiseHeight { get; private set; } = DefaultNoiseHeight;
        public float MinSampleDist { get; private set; } = DefaultMinSampleDist;
        public float MaxSampleDist { get; private set; } = DefaultMaxSampleDist;

        private XRTexture2D? NoiseTexture { get; set; } = null;

        public void GenerateNoiseKernel()
        {
            Random r = new();

            Kernel = new Vector3[Samples];
            Noise = new Vector2[NoiseWidth * NoiseHeight];

            float scale;
            Vector3 sample;

            for (int i = 0; i < Samples; ++i)
            {
                sample = Vector3.Normalize(new Vector3(
                    (float)r.NextDouble() * 2.0f - 1.0f,
                    (float)r.NextDouble() * 2.0f - 1.0f,
                    (float)r.NextDouble() + 0.1f));
                scale = i / (float)Samples;
                sample *= Interp.Lerp(MinSampleDist, MaxSampleDist, scale * scale);
                Kernel[i] = sample;
            }

            for (int i = 0; i < Noise.Length; ++i)
                Noise[i] = Vector2.Normalize(new Vector2((float)r.NextDouble(), (float)r.NextDouble()));
        }

        private Vector2 NoiseScale;

        private int _lastWidth = 0;
        private int _lastHeight = 0;

        public string NormalTextureName { get; set; } = "Normal";
        public string DepthViewTextureName { get; set; } = "DepthView";
        public string AlbedoTextureName { get; set; } = "AlbedoOpacity";
        public string RMSITextureName { get; set; } = "RMSI";
        public string DepthStencilTextureName { get; set; } = "DepthStencil";

        public void SetOptions(int samples, uint noiseWidth, uint noiseHeight, float minSampleDist, float maxSampleDist)
        {
            Samples = samples;
            NoiseWidth = noiseWidth;
            NoiseHeight = noiseHeight;
            MinSampleDist = minSampleDist;
            MaxSampleDist = maxSampleDist;
        }
        public void SetGBufferInputTextureNames(string normal, string depthView, string albedo, string rmsi, string depthStencil)
        {
            NormalTextureName = normal;
            DepthViewTextureName = depthView;
            AlbedoTextureName = albedo;
            RMSITextureName = rmsi;
            DepthStencilTextureName = depthStencil;
        }
        public void SetOutputNames(string noise, string ssaoIntensityTexture, string ssaoFBO, string ssaoBlurFBO, string gBufferFBO)
        {
            SSAONoiseTextureName = noise;
            SSAOIntensityTextureName = ssaoIntensityTexture;
            SSAOFBOName = ssaoFBO;
            SSAOBlurFBOName = ssaoBlurFBO;
            GBufferFBOFBOName = gBufferFBO;
        }

        protected override void Execute()
        {
            XRTexture2D? normalTex = Pipeline.GetTexture<XRTexture2D>(NormalTextureName);
            XRTexture2DView? depthViewTex = Pipeline.GetTexture<XRTexture2DView>(DepthViewTextureName);
            XRTexture2D? albedoTex = Pipeline.GetTexture<XRTexture2D>(AlbedoTextureName);
            XRTexture2D? rmsiTex = Pipeline.GetTexture<XRTexture2D>(RMSITextureName);
            XRTexture2D? depthStencilTex = Pipeline.GetTexture<XRTexture2D>(DepthStencilTextureName);

            if (normalTex is null ||
                depthViewTex is null ||
                albedoTex is null ||
                rmsiTex is null ||
                depthStencilTex is null)
                return;

            var area = Engine.Rendering.State.RenderArea;
            int width = area.Width;
            int height = area.Height;
            if (width == _lastWidth && 
                height == _lastHeight)
                return;

            RegenerateFBOs(
                normalTex,
                depthViewTex,
                albedoTex,
                rmsiTex,
                depthStencilTex,
                width,
                height);
        }

        private void RegenerateFBOs(XRTexture2D normalTex, XRTexture2DView depthViewTex, XRTexture2D albedoTex, XRTexture2D rmsiTex, XRTexture2D depthStencilTex, int width, int height)
        {
            Debug.Out($"SSAO: Regenerating FBOs for {width}x{height}");
            _lastWidth = width;
            _lastHeight = height;

            GenerateNoiseKernel();

            NoiseScale = new Vector2(
                (float)width / NoiseWidth,
                (float)height / NoiseHeight);

            XRTexture2D ssaoTex = XRTexture2D.CreateFrameBufferTexture(
                (uint)width,
                (uint)height,
                EPixelInternalFormat.R16f,
                EPixelFormat.Red,
                EPixelType.HalfFloat,
                EFrameBufferAttachment.ColorAttachment0);
            ssaoTex.Name = SSAOIntensityTextureName;
            ssaoTex.MinFilter = ETexMinFilter.Nearest;
            ssaoTex.MagFilter = ETexMagFilter.Nearest;
            Pipeline.SetTexture(ssaoTex);

            RenderingParameters renderParams = new()
            {
                DepthTest =
                {
                    Enabled = ERenderParamUsage.Unchanged,
                    UpdateDepth = false,
                    Function = EComparison.Always,
                }
            };

            var ssaoGenShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "SSAOGen.fs"), EShaderType.Fragment);
            var ssaoBlurShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "SSAOBlur.fs"), EShaderType.Fragment);

            XRTexture[] ssaoGenTexRefs =
            [
                normalTex,
                GetOrCreateNoiseTexture(),
                depthViewTex,
            ];
            XRTexture[] ssaoBlurTexRefs =
            [
                ssaoTex
            ];

            XRMaterial ssaoGenMat = new(ssaoGenTexRefs, ssaoGenShader) { RenderOptions = renderParams };
            XRMaterial ssaoBlurMat = new(ssaoBlurTexRefs, ssaoBlurShader) { RenderOptions = renderParams };

            XRQuadFrameBuffer ssaoGenFBO = new(ssaoGenMat, true, 
                (albedoTex, EFrameBufferAttachment.ColorAttachment0, 0, -1),
                (normalTex, EFrameBufferAttachment.ColorAttachment1, 0, -1),
                (rmsiTex, EFrameBufferAttachment.ColorAttachment2, 0, -1),
                (depthStencilTex, EFrameBufferAttachment.DepthStencilAttachment, 0, -1))
            {
                Name = SSAOFBOName
            };
            ssaoGenFBO.SettingUniforms += SSAOGen_SetUniforms;

            XRQuadFrameBuffer ssaoBlurFBO = new(ssaoBlurMat, true, (ssaoTex, EFrameBufferAttachment.ColorAttachment0, 0, -1))
            {
                Name = SSAOBlurFBOName
            };

            XRFrameBuffer gbufferFBO = new((ssaoTex, EFrameBufferAttachment.ColorAttachment0, 0, -1)) 
            {
                Name = GBufferFBOFBOName
            };

            Pipeline.SetFBO(ssaoGenFBO);
            Pipeline.SetFBO(ssaoBlurFBO);
            Pipeline.SetFBO(gbufferFBO);
        }

        private void SSAOGen_SetUniforms(XRRenderProgram program)
        {
            program.Uniform("NoiseScale", NoiseScale);
            program.Uniform("Samples", Kernel!);

            var rc = Pipeline.RenderState.SceneCamera;
            if (rc is null)
                return;
            
            rc.SetUniforms(program);
            rc.SetAmbientOcclusionUniforms(program);

            program.Uniform(EEngineUniform.ScreenWidth.ToString(), Pipeline.RenderState.CurrentRenderRegion.Width);
            program.Uniform(EEngineUniform.ScreenHeight.ToString(), Pipeline.RenderState.CurrentRenderRegion.Height);
            program.Uniform(EEngineUniform.ScreenOrigin.ToString(), 0.0f);
        }

        private XRTexture2D GetOrCreateNoiseTexture()
        {
            if (NoiseTexture != null)
                return NoiseTexture;

            XRTexture2D noiseTex = new()
            {
                Name = SSAONoiseTextureName,
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.Repeat,
                VWrap = ETexWrapMode.Repeat,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rg16f,
                Mipmaps =
                [
                    new()
                    {
                        Data = DataSource.FromArray(Noise!.SelectMany(v => new float[] { v.X, v.Y }).ToArray()),
                        PixelFormat = EPixelFormat.Rg,
                        PixelType = EPixelType.Float,
                        InternalFormat = EPixelInternalFormat.RG,
                        Width = NoiseWidth,
                        Height = NoiseHeight
                    }
                ]
            };

            Pipeline.SetTexture(noiseTex);
            noiseTex.PushData();
            return NoiseTexture = noiseTex;
        }
    }
}
