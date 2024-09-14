using Extensions;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        public enum ESizedInternalFormat
        {
            Rgba8 = 32856,
            Rgba16 = 32859,
            R8 = 33321,
            R16 = 33322,
            Rg8 = 33323,
            Rg16 = 33324,
            R16f = 33325,
            R32f = 33326,
            Rg16f = 33327,
            Rg32f = 33328,
            R8i = 33329,
            R8ui = 33330,
            R16i = 33331,
            R16ui = 33332,
            R32i = 33333,
            R32ui = 33334,
            Rg8i = 33335,
            Rg8ui = 33336,
            Rg16i = 33337,
            Rg16ui = 33338,
            Rg32i = 33339,
            Rg32ui = 33340,
            Rgba32f = 34836,
            Rgba16f = 34842,
            Rgba32ui = 36208,
            Rgba16ui = 36214,
            Rgba8ui = 36220,
            Rgba32i = 36226,
            Rgba16i = 36232,
            Rgba8i = 36238
        }

        public class GLMaterial(OpenGLRenderer renderer, XRMaterial material) : GLObject<XRMaterial>(renderer, material)
        {
            public override GLObjectType Type => GLObjectType.Material;

            public GLRenderProgram? Program { get; private set; }

            protected override void UnlinkData()
            {
                base.UnlinkData();

                Program?.Destroy();
                Program = null;

                foreach (IGLTexture? tex in Textures)
                    tex?.Destroy();
                Textures = [];
            }

            protected override void LinkData()
            {
                base.LinkData();

                Program = Data.ShaderPipelineProgram is not null ? new GLRenderProgram(Renderer, Data.ShaderPipelineProgram) : null;
                Textures = Data.Textures?.Select(t => CreateGLTexture(Renderer, t))?.ToArray() ?? [];
            }

            public static IGLTexture? CreateGLTexture(OpenGLRenderer renderer, XRTexture texture)
                => texture switch
                {
                    XRTexture2D tex2D => new GLTexture2D(renderer, tex2D),
                    _ => null,
                };

            public IGLTexture?[] Textures { get; private set; } = [];
            
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

            public float SecondsLive { get; set; } = 0.0f;

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
                
                IGLTexture? texture = Textures[textureIndex];
                if (texture is null)
                    return;

                Program?.Sampler(texture.ResolveSamplerName(textureIndex, samplerNameOverride), texture, textureIndex);
            }
        }
    }
}