﻿using System.Numerics;
using XREngine.Data;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Generates the necessary textures and framebuffers for SSAO in the render pipeline depending on the current render area.
    /// </summary>
    /// <param name="pipeline"></param>
    public class VPRC_SSAO(ViewportRenderCommandContainer pipeline) : ViewportRenderCommand(pipeline)
    {
        public string SSAONoiseTextureName { get; set; } = "SSAONoiseTexture";
        public string SSAOFBOTextureName { get; set; } = "SSAOFBOTexture";
        public string SSAOFBOName { get; set; } = "SSAOFBO";
        public string SSAOBlurFBOName { get; set; } = "SSAOBlurFBO";
        public string GBufferFBOFBOName { get; set; } = "GBufferFBO";

        const int DefaultSamples = 64;
        const int DefaultNoiseWidth = 4, DefaultNoiseHeight = 4;
        const float DefaultMinSampleDist = 0.1f, DefaultMaxSampleDist = 1.0f;

        public Vector2[]? Noise { get; private set; }
        public Vector3[]? Kernel { get; private set; }

        public int Samples { get; private set; } = DefaultSamples;
        public int NoiseWidth { get; private set; } = DefaultNoiseWidth;
        public int NoiseHeight { get; private set; } = DefaultNoiseHeight;
        public float MinSampleDist { get; private set; } = DefaultMinSampleDist;
        public float MaxSampleDist { get; private set; } = DefaultMaxSampleDist;

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

        private void SSAO_SetUniforms(XRRenderProgram program)
        {
            program.Uniform("NoiseScale", NoiseScale);
            program.Uniform("Samples", Kernel!);

            var rc = Pipeline.RenderStatus.Camera;
            if (rc != null)
            {
                rc.SetUniforms(program);
                rc.SetAmbientOcclusionUniforms(program);
            }
        }

        private int _lastWidth = 0;
        private int _lastHeight = 0;

        public string NormalTextureName { get; set; } = "Normal";
        public string DepthViewTextureName { get; set; } = "DepthView";
        public string AlbedoTextureName { get; set; } = "AlbedoOpacity";
        public string RMSITextureName { get; set; } = "RMSI";
        public string DepthStencilTextureName { get; set; } = "DepthStencil";

        public void SetOptions(int samples, int noiseWidth, int noiseHeight, float minSampleDist, float maxSampleDist)
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
        public void SetOutputNames(string noise, string ssao, string ssaoFBO, string ssaoBlurFBO, string gBufferFBO)
        {
            SSAONoiseTextureName = noise;
            SSAOFBOTextureName = ssao;
            SSAOFBOName = ssaoFBO;
            SSAOBlurFBOName = ssaoBlurFBO;
            GBufferFBOFBOName = gBufferFBO;
        }

        protected override void Execute()
        {
            XRTexture2D? normalTex = Pipeline.GetTexture<XRTexture2D>(NormalTextureName);
            XRTextureView2D? depthViewTex = Pipeline.GetTexture<XRTextureView2D>(DepthViewTextureName);
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
            if (width == _lastWidth && height == _lastHeight)
                return;
            
            _lastWidth = width;
            _lastHeight = height;

            GenerateNoiseKernel();

            NoiseScale = new Vector2(
                (float)width / NoiseWidth,
                (float)height / NoiseHeight);

            XRTexture2D noiseTex = new(
                (uint)NoiseWidth,
                (uint)NoiseHeight,
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
            noiseTex.Mipmaps[0].GetPixels().SetPixels(Noise!.SelectMany(v => new float[] { v.X, v.Y }).ToArray());
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

            RenderingParameters renderParams = new()
            {
                DepthTest =
                {
                    Enabled = ERenderParamUsage.Unchanged,
                    UpdateDepth = false,
                    Function = EComparison.Always,
                }
            };

            var ssaoFbo = new XRQuadFrameBuffer(
                new([normalTex, noiseTex, depthViewTex],
                    XRShader.EngineShader(Path.Combine(SceneShaderPath, "SSAOGen.fs"), EShaderType.Fragment)
                    ) { RenderOptions = renderParams },
                false,
                (albedoTex, EFrameBufferAttachment.ColorAttachment0, 0, -1),
                (normalTex, EFrameBufferAttachment.ColorAttachment1, 0, -1),
                (rmsiTex, EFrameBufferAttachment.ColorAttachment2, 0, -1),
                (depthStencilTex, EFrameBufferAttachment.DepthStencilAttachment, 0, -1)) { Name = SSAOFBOName };

            ssaoFbo.SettingUniforms += SSAO_SetUniforms;

            Pipeline.SetFBO(ssaoFbo);
            Pipeline.SetFBO(new XRQuadFrameBuffer(new([ssaoTex], XRShader.EngineShader(Path.Combine(SceneShaderPath, "SSAOBlur.fs"), EShaderType.Fragment)) { RenderOptions = renderParams }) { Name = SSAOBlurFBOName });
            Pipeline.SetFBO(new XRFrameBuffer((ssaoTex, EFrameBufferAttachment.ColorAttachment0, 0, -1)) { Name = GBufferFBOFBOName });
        }
    }
}
