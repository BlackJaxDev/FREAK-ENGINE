using System.Drawing.Imaging;
using System.Drawing;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Data.Core;
using System.Numerics;
using XREngine.Data;

namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Performs screen-space ambient occlusion on the scene.
    /// </summary>
    /// <param name="pipeline"></param>
    public class VPRC_SSAO(XRRenderPipeline pipeline) : ViewportRenderCommand(pipeline)
    {
        public XRQuadFrameBuffer? SSAOFBO { get; private set; }
        public XRQuadFrameBuffer? SSAOBlurFBO { get; private set; }
        public XRFrameBuffer? GBufferFBO { get; private set; }

        public string SSAONoiseTextureName { get; set; } = "SSAONoiseTexture";
        public string SSAOFBOTextureName { get; set; } = "SSAOFBOTexture";

        private readonly SSAOGenerator _ssaoInfo = new();
        
        private Vector2 _noiseScale;
        private Vector3[] _noiseSamples;

        public override void DestroyFBOs()
        {
            SSAOBlurFBO?.Destroy();
            SSAOBlurFBO = null;

            SSAOFBO?.Destroy();
            SSAOFBO = null;

            GBufferFBO?.Destroy();
            GBufferFBO = null;
        }
        public override unsafe void GenerateFBOs()
        {
            XRTexture2D? normalTex = Pipeline.GetTexture<XRTexture2D>("Normal");
            XRTextureView2D? depthViewTex = Pipeline.GetTexture<XRTextureView2D>("DepthView");
            XRTexture2D? albedoTex = Pipeline.GetTexture<XRTexture2D>("AlbedoOpacity");
            XRTexture2D? rmsiTex = Pipeline.GetTexture<XRTexture2D>("RMSI");
            XRTexture2D? depthStencilTex = Pipeline.GetTexture<XRTexture2D>("DepthStencil");
            if (normalTex is null || 
                depthViewTex is null ||
                albedoTex is null || 
                rmsiTex is null || 
                depthStencilTex is null)
                return;

            var vp = Pipeline.RenderStatus.Viewport;
            if (vp is null)
                return;

            int width = vp.InternalWidth;
            int height = vp.InternalHeight;

            _ssaoInfo.Generate();

            _noiseScale = vp.InternalResolutionRegion.Extents / 4.0f;
            _noiseSamples = _ssaoInfo.Kernel!;

            RenderingParameters renderParams = new()
            {
                DepthTest =
                {
                    Enabled = ERenderParamUsage.Unchanged,
                    UpdateDepth = false,
                    Function = EComparison.Always,
                }
            };

            XRTexture2D noiseTex = new(
                (uint)_ssaoInfo.NoiseWidth,
                (uint)_ssaoInfo.NoiseHeight,
                EPixelInternalFormat.Rg32f,
                EPixelFormat.Rg,
                EPixelType.Float)
            {
                Name = SSAONoiseTextureName,
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.Repeat,
                VWrap = ETexWrapMode.Repeat,
                Resizable = false,
            };

            noiseTex.Mipmaps[0].GetPixels().SetPixels(_ssaoInfo.Noise!.SelectMany(v => new float[] { v.X, v.Y }).ToArray());

            Pipeline.SetTexture(noiseTex);

            XRTexture2D ssaoTex = XRTexture2D.CreateFrameBufferTexture(
                (uint)width,
                (uint)height,
                EPixelInternalFormat.R16f,
                EPixelFormat.Red,
                EPixelType.HalfFloat,
                EFrameBufferAttachment.ColorAttachment0);

            ssaoTex.Name = SSAOFBOTextureName;
            ssaoTex.MinFilter = ETexMinFilter.Nearest;
            ssaoTex.MagFilter = ETexMagFilter.Nearest;

            Pipeline.SetTexture(ssaoTex);

            XRShader ssaoShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "SSAOGen.fs"), EShaderType.Fragment);
            XRShader ssaoBlurShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "SSAOBlur.fs"), EShaderType.Fragment);

            XRTexture2D[] ssaoRefs =
            [
                normalTex,
                noiseTex,
                depthViewTex,
            ];
            XRTexture2D[] ssaoBlurRefs =
            [
                ssaoTex
            ];

            XRMaterial ssaoMat = new(ssaoRefs, ssaoShader) { RenderOptions = renderParams };
            XRMaterial ssaoBlurMat = new(ssaoBlurRefs, ssaoBlurShader) { RenderOptions = renderParams };

            SSAOFBO = new XRQuadFrameBuffer(ssaoMat);
            SSAOFBO.SettingUniforms += SSAO_SetUniforms;
            SSAOFBO.SetRenderTargets(
                (albedoTex, EFrameBufferAttachment.ColorAttachment0, 0, -1),
                (normalTex, EFrameBufferAttachment.ColorAttachment1, 0, -1),
                (rmsiTex, EFrameBufferAttachment.ColorAttachment2, 0, -1),
                (depthStencilTex, EFrameBufferAttachment.DepthStencilAttachment, 0, -1));
            Pipeline.SetFBO("SSAO", SSAOFBO);

            SSAOBlurFBO = new XRQuadFrameBuffer(ssaoBlurMat);
            Pipeline.SetFBO("SSAOBlur", SSAOBlurFBO);

            GBufferFBO = new XRFrameBuffer();
            GBufferFBO.SetRenderTargets((ssaoTex, EFrameBufferAttachment.ColorAttachment0, 0, -1));
            Pipeline.SetFBO("GBuffer", GBufferFBO);
        }

        private void SSAO_SetUniforms(XRRenderProgram program)
        {
            program.Uniform("NoiseScale", _noiseScale);
            program.Uniform("Samples", _noiseSamples);

            var rc = Pipeline.RenderStatus.Camera;
            if (rc != null)
            {
                rc.SetUniforms(program);
                rc.SetAmbientOcclusionUniforms(program);
            }
        }
        protected override void Execute()
        {

        }
    }

    public class SSAOGenerator : XRBase
    {
        public const int DefaultSamples = 64;
        const int DefaultNoiseWidth = 4, DefaultNoiseHeight = 4;
        const float DefaultMinSampleDist = 0.1f, DefaultMaxSampleDist = 1.0f;

        public Vector2[]? Noise { get; private set; }
        public Vector3[]? Kernel { get; private set; }
        public int Samples { get; private set; }
        public int NoiseWidth { get; private set; }
        public int NoiseHeight { get; private set; }
        public float MinSampleDist { get; private set; }
        public float MaxSampleDist { get; private set; }

        public void Generate(
            //int width, int height,
            int samples = DefaultSamples,
            int noiseWidth = DefaultNoiseWidth,
            int noiseHeight = DefaultNoiseHeight,
            float minSampleDist = DefaultMinSampleDist,
            float maxSampleDist = DefaultMaxSampleDist)
        {
            Samples = samples;
            NoiseWidth = noiseWidth;
            NoiseHeight = noiseHeight;
            MinSampleDist = minSampleDist;
            MaxSampleDist = maxSampleDist;

            Random r = new();

            Kernel = new Vector3[samples];
            Noise = new Vector2[noiseWidth * noiseHeight];

            float scale;
            Vector3 sample;

            for (int i = 0; i < samples; ++i)
            {
                sample = Vector3.Normalize(new Vector3(
                    (float)r.NextDouble() * 2.0f - 1.0f,
                    (float)r.NextDouble() * 2.0f - 1.0f,
                    (float)r.NextDouble() + 0.1f));
                scale = i / (float)samples;
                sample *= Interp.Lerp(minSampleDist, maxSampleDist, scale * scale);
                Kernel[i] = sample;
            }

            for (int i = 0; i < Noise.Length; ++i)
                Noise[i] = Vector2.Normalize(new Vector2((float)r.NextDouble(), (float)r.NextDouble()));
        }
    }
}
