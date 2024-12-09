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
                    if (tex is not null && Renderer.TryGetAPIRenderObject(tex, out var apiObj))
                        apiObj?.Destroy();
                
                Data.Textures.PostAnythingAdded -= TextureAdded;
                Data.Textures.PostAnythingRemoved -= TextureRemoved;
            }

            private void TextureRemoved(XRTexture? tex)
            {
                if (tex is not null && Renderer.TryGetAPIRenderObject(tex, out var apiObj))
                    apiObj?.Destroy();
            }
            
            private void TextureAdded(XRTexture? tex)
            {
            }

            public void SetUniforms(GLRenderProgram? program)
            {
                //Apply special rendering parameters
                if (Data.RenderOptions != null)
                    Renderer.ApplyRenderParameters(Data.RenderOptions);

                program ??= Program;
                if (program is null)
                    return;

                foreach (ShaderVar param in Data.Parameters)
                    param.SetUniform(program.Data);

                SetTextureUniforms(program);
                SetEngineUniforms(program);
                Data.OnSettingUniforms(program.Data);
            }

            public float SecondsLive
            {
                get => _secondsLive;
                set => SetField(ref _secondsLive, value);
            }

            private void SetEngineUniforms(GLRenderProgram program)
            {
                SecondsLive += Engine.Time.Timer.Update.Delta;

                var reqs = Data.RenderOptions.RequiredEngineUniforms;

                //Set engine uniforms
                if (reqs.HasFlag(EUniformRequirements.Camera))
                    Engine.Rendering.State.RenderingPipelineState?.RenderingCamera?.SetUniforms(program.Data);

                if (reqs.HasFlag(EUniformRequirements.Lights))
                {
                    //AbstractRenderer.Current3DScene.Lights.SetUniforms(program);
                }

                if (reqs.HasFlag(EUniformRequirements.RenderTime))
                {
                    program?.Uniform(nameof(EUniformRequirements.RenderTime), SecondsLive);
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
                foreach (XRTexture? tref in Data.Textures)
                {
                    if (tref is null || !tref.FrameBufferAttachment.HasValue)
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

            public void SetTextureUniforms(GLRenderProgram program)
            {
                for (int i = 0; i < Data.Textures.Count; ++i)
                    SetTextureUniform(program, i);
            }
            public void SetTextureUniform(GLRenderProgram program, int textureIndex, string? samplerNameOverride = null)
            {
                if (!Data.Textures.IndexInRange(textureIndex))
                    return;
                
                var tex = Data.Textures[textureIndex];
                if (tex is null || Renderer.GetOrCreateAPIRenderObject(tex) is not IGLTexture texture)
                    return;

                program?.Sampler(texture.ResolveSamplerName(textureIndex, samplerNameOverride), texture, textureIndex);
            }
        }
    }
}