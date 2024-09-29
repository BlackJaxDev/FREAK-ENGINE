using Extensions;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        public enum ESizedInternalFormat
        {
            //Red
            R8,
            R8Snorm,
            R16,
            R16Snorm,

            //Red Green
            Rg8,
            Rg8Snorm,
            Rg16,
            Rg16Snorm,

            //Red Green Blue
            R3G3B2,
            Rgb4,
            Rgb5,
            Rgb8,
            Rgb8Snorm,
            Rgb10,
            Rgb12,
            Rgb16Snorm,
            Rgba2,
            Rgba4,

            //Red Green Blue Alpha
            Rgb5A1,
            Rgba8,
            Rgba8Snorm,
            Rgb10A2,
            Rgba12,
            Rgba16,

            //Red Green Blue
            Srgb8,

            //Red Green Blue Alpha
            Srgb8Alpha8,

            //Red
            R16f,

            //Red Green
            Rg16f,

            //Red Green Blue
            Rgb16f,

            //Red Green Blue Alpha
            Rgba16f,

            //Red
            R32f,

            //Red Green
            Rg32f,

            //Red Green Blue
            Rgb32f,

            //Red Green Blue Alpha
            Rgba32f,

            //Red Green Blue
            R11fG11fB10f,
            Rgb9E5,

            //Red
            R8i,
            R8ui,
            R16i,
            R16ui,
            R32i,
            R32ui,

            //Red Green
            Rg8i,
            Rg8ui,
            Rg16i,
            Rg16ui,
            Rg32i,
            Rg32ui,

            //Red Green Blue
            Rgb8i,
            Rgb8ui,
            Rgb16i,
            Rgb16ui,
            Rgb32i,
            Rgb32ui,

            //Red Green Blue Alpha
            Rgba8i,
            Rgba8ui,
            Rgba16i,
            Rgba16ui,
            Rgba32i,
            Rgba32ui
        }

        public class GLMaterial(OpenGLRenderer renderer, XRMaterial material) : GLObject<XRMaterial>(renderer, material)
        {
            private float _secondsLive = 0.0f;

            public override GLObjectType Type => GLObjectType.Material;

            public GLRenderProgram? Program => Renderer.GenericToAPI<GLRenderProgram>(Data.ShaderPipelineProgram);

            protected override void LinkData()
            {
                foreach (var tex in Data.Textures)
                    if (Renderer.TryGetAPIRenderObject(tex, out var apiObj))
                        apiObj?.Generate();
            }
            protected override void UnlinkData()
            {
                foreach (var tex in Data.Textures)
                    if (Renderer.TryGetAPIRenderObject(tex, out var apiObj))
                        apiObj?.Destroy();
            }

            public void SetUniforms()
            {
                //Apply special rendering parameters
                if (Data.RenderOptions != null)
                    Renderer.ApplyRenderParameters(Data.RenderOptions);

                var program = Data.ShaderPipelineProgram;
                if (program is null)
                    return;

                foreach (ShaderVar param in Data.Parameters)
                    param.SetUniform(program);

                SetTextureUniforms();
                SetEngineUniforms();
                Data.SettingUniforms.Invoke(Data);
            }

            public float SecondsLive
            {
                get => _secondsLive;
                set => SetField(ref _secondsLive, value);
            }

            private void SetEngineUniforms()
            {
                //TODO: keep track of time
                //SecondsLive += Engine.Time.Timer.UpdateDelta;

                var reqs = Data.RenderOptions.RequiredEngineUniforms;

                //Set engine uniforms
                //if (reqs.HasFlag(EUniformRequirements.Camera))
                //    Renderer.CurrentCamera.SetUniforms(program);

                //if (Requirements.HasFlag(EUniformRequirements.Lights))
                //    AbstractRenderer.Current3DScene.Lights.SetUniforms(program);

                if (reqs.HasFlag(EUniformRequirements.RenderTime))
                {
                    Program?.Uniform(nameof(EUniformRequirements.RenderTime), SecondsLive);
                }
                if (reqs.HasFlag(EUniformRequirements.ViewportDimensions))
                {
                    //Program?.Uniform(nameof(EUniformRequirements.ViewportDimensions), viewportDimensions);
                }
                if (reqs.HasFlag(EUniformRequirements.MousePosition))
                {
                    //Program?.Uniform(nameof(EUniformRequirements.MousePosition), mousePosition);
                }
            }

            public EDrawBuffersAttachment[] CollectFBOAttachments()
            {
                if (Data.Textures is null || Data.Textures.Count <= 0)
                    return [];

                List<EDrawBuffersAttachment> fboAttachments = [];
                foreach (XRTexture tref in Data.Textures)
                {
                    if (!tref.FrameBufferAttachment.HasValue)
                        continue;
                    switch (tref.FrameBufferAttachment.Value)
                    {
                        case EFrameBufferAttachment.Color:
                        case EFrameBufferAttachment.Depth:
                        case EFrameBufferAttachment.DepthAttachment:
                        case EFrameBufferAttachment.DepthStencilAttachment:
                        case EFrameBufferAttachment.Stencil:
                        case EFrameBufferAttachment.StencilAttachment:
                            continue;
                    }
                    fboAttachments.Add((EDrawBuffersAttachment)(int)tref.FrameBufferAttachment.Value);
                }

                return [.. fboAttachments];
            }

            public void SetTextureUniforms()
            {
                for (int i = 0; i < Data.Textures.Count; ++i)
                    SetTextureUniform(i);
            }
            public void SetTextureUniform(int textureIndex, string? samplerNameOverride = null)
            {
                if (!Data.Textures.IndexInRange(textureIndex))
                    return;
                
                var tex = Data.Textures[textureIndex];
                if (tex is null || Renderer.GetOrCreateAPIRenderObject(tex) is not IGLTexture texture)
                    return;

                Program?.Sampler(texture.ResolveSamplerName(textureIndex, samplerNameOverride), texture, textureIndex);
            }
        }
    }
}