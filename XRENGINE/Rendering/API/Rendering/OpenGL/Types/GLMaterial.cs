using Extensions;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        public class GLMaterial(OpenGLRenderer renderer, XRMaterial material) : GLObject<XRMaterial>(renderer, material)
        {
            private float _secondsLive = 0.0f;

            public override GLObjectType Type => GLObjectType.Material;

            public GLRenderProgram? Program => Renderer.GenericToAPI<GLRenderProgram>(Data.ShaderPipelineProgram);

            protected override void LinkData()
            {
                //foreach (var tex in Data.Textures)
                //    if (Renderer.TryGetAPIRenderObject(tex, out var apiObj))
                //        apiObj?.Generate();

                Data.Textures.PostAnythingAdded += TextureAdded;
                Data.Textures.PostAnythingRemoved += TextureRemoved;
            }

            protected override void UnlinkData()
            {
                foreach (var tex in Data.Textures)
                    if (Renderer.TryGetAPIRenderObject(tex, out var apiObj))
                        apiObj?.Destroy();

                Data.Textures.PostAnythingAdded -= TextureAdded;
                Data.Textures.PostAnythingRemoved -= TextureRemoved;
            }

            private void TextureRemoved(XRTexture tex)
            {
                if (Renderer.TryGetAPIRenderObject(tex, out var apiObj))
                    apiObj?.Destroy();
            }
            
            private void TextureAdded(XRTexture tex)
            {

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
                SecondsLive += Engine.Time.Timer.Update.Delta;

                var reqs = Data.RenderOptions.RequiredEngineUniforms;

                //Set engine uniforms
                if (reqs.HasFlag(EUniformRequirements.Camera) && Program is not null)
                    Engine.Rendering.State.PipelineState?.RenderingCamera?.SetUniforms(Program.Data);

                if (reqs.HasFlag(EUniformRequirements.Lights))
                {
                    //AbstractRenderer.Current3DScene.Lights.SetUniforms(program);
                }

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