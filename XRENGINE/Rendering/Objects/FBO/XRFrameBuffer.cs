using Extensions;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRFrameBuffer : GenericRenderObject
    {
        public XRFrameBuffer() { }
        public XRFrameBuffer(params (IFrameBufferAttachement Target, EFrameBufferAttachment Attachment, int MipLevel, int LayerIndex)[]? targets)
            => SetRenderTargets(targets);

        private EDrawBuffersAttachment[]? _drawBuffers;
        private EFrameBufferTextureTypeFlags _textureTypes = EFrameBufferTextureTypeFlags.None;
        private (IFrameBufferAttachement Target, EFrameBufferAttachment Attachment, int MipLevel, int LayerIndex)[]? _targets;

        public static XRFrameBuffer? CurrentlyBound { get; set; }

        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public EFrameBufferTextureTypeFlags TextureTypes
        {
            get => _textureTypes;
            set => SetField(ref _textureTypes, value);
        }
        public (IFrameBufferAttachement Target, EFrameBufferAttachment Attachment, int MipLevel, int LayerIndex)[]? Targets
        {
            get => _targets;
            private set => SetField(ref _targets, value);
        }
        public EDrawBuffersAttachment[]? DrawBuffers
        {
            get => _drawBuffers;
            private set => SetField(ref _drawBuffers, value);
        }

        public event Action? Resized;

        /// <summary>
        /// Resizes the textures attached to this frame buffer.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Resize(uint width, uint height)
        {
            Width = width;
            Height = height;

            if (Targets is null)
                return;

            foreach (var (texture, _, _, _) in Targets)
                if (texture is XRTexture2D texture2D)
                    texture2D.Resize(width, height);

            Resized?.Invoke();
        }

        public event Action? PreSetRenderTargets;
        public event Action? PostSetRenderTargets;

        /// <summary>
        /// Uses the textures in the material to set the render targets.
        /// </summary>
        /// <param name="material"></param>
        public void SetRenderTargets(XRMaterial? material)
            => SetRenderTargets(material?.Textures?.
                Where(x => x?.FrameBufferAttachment != null).
                Select(x => ((IFrameBufferAttachement)x, x.FrameBufferAttachment!.Value, 0, -1)).
                ToArray());

        /// <summary>
        /// Informs the framebuffer where it is writing shader output data to;
        /// any combination of textures and renderbuffers.
        /// </summary>
        /// <param name="targets">The array of targets to render to.
        /// <list type="bullet">
        /// <item><description><see cref="IFrameBufferAttachement"/> Target: the <see cref="XRTexture"/> or <see cref="XRRenderBuffer"/> to render to.</description></item>
        /// <item><description><see cref="EFrameBufferAttachment"/> Attachment: which shader output to capture.</description></item>
        /// <item><description><see cref="int"/> MipLevel: the level of detail to write to.</description></item>
        /// <item><description><see cref="int"/> LayerIndex: the layer to write to. For example, a cubemap has 6 layers, one for each face.</description></item>
        /// </list>
        /// </param>
        public void SetRenderTargets(params (IFrameBufferAttachement Target, EFrameBufferAttachment Attachment, int MipLevel, int LayerIndex)[]? targets)
        {
            PreSetRenderTargets?.Invoke();

            Targets = targets;
            TextureTypes = EFrameBufferTextureTypeFlags.None;

            List<EDrawBuffersAttachment> fboAttachments = [];
            if (Targets is not null)
            {
                foreach (var (_, Attachment, _, _) in Targets)
                {
                    switch (Attachment)
                    {
                        case EFrameBufferAttachment.Color:
                            TextureTypes |= EFrameBufferTextureTypeFlags.Color;
                            continue;
                        case EFrameBufferAttachment.Depth:
                        case EFrameBufferAttachment.DepthAttachment:
                            TextureTypes |= EFrameBufferTextureTypeFlags.Depth;
                            continue;
                        case EFrameBufferAttachment.DepthStencilAttachment:
                            TextureTypes |= EFrameBufferTextureTypeFlags.Depth | EFrameBufferTextureTypeFlags.Stencil;
                            continue;
                        case EFrameBufferAttachment.Stencil:
                        case EFrameBufferAttachment.StencilAttachment:
                            TextureTypes |= EFrameBufferTextureTypeFlags.Stencil;
                            continue;
                    }
                    fboAttachments.Add((EDrawBuffersAttachment)(int)Attachment);
                    TextureTypes |= EFrameBufferTextureTypeFlags.Color;
                }
            }

            DrawBuffers = [.. fboAttachments];

            PostSetRenderTargets?.Invoke();
        }

        public event Action? BindForReadRequested;
        public event Action? BindForWriteRequested;
        public event Action? BindRequested;

        public event Action? UnbindFromReadRequested;
        public event Action? UnbindFromWriteRequested;
        public event Action? UnbindRequested;

        public StateObject BindForReading()
        {
            CurrentlyBound?.UnbindFromReading();

            BindForReadRequested?.Invoke();
            CurrentlyBound = this;

            return new StateObject(UnbindFromReading);
        }
        public void UnbindFromReading()
        {
            if (CurrentlyBound != this)
                return;

            UnbindFromReadRequested?.Invoke();
            CurrentlyBound = null;
        }

        public StateObject BindForWriting()
        {
            CurrentlyBound?.UnbindFromWriting();

            BindForWriteRequested?.Invoke();
            CurrentlyBound = this;

            return new StateObject(UnbindFromWriting);
        }
        public void UnbindFromWriting()
        {
            if (CurrentlyBound != this)
                return;

            UnbindFromWriteRequested?.Invoke();
            CurrentlyBound = null;
        }

        public StateObject Bind()
        {
            CurrentlyBound?.Unbind();

            BindRequested?.Invoke();
            CurrentlyBound = this;

            return new StateObject(Unbind);
        }

        public void Unbind()
        {
            if (CurrentlyBound != this)
                return;

            UnbindRequested?.Invoke();
            CurrentlyBound = null;
        }
        
        public event Action<int>? PreSetRenderTarget;
        public event Action<int>? PostSetRenderTarget;

        public void SetRenderTarget(int i, (IFrameBufferAttachement Target, EFrameBufferAttachment Attachment, int MipLevel, int LayerIndex) target)
        {
            if (Targets is null || !Targets.IndexInRangeArrayT(i))
            {
                Debug.Out($"Index {i} is out of range for the number of targets in the framebuffer.");
                return;
            }

            PreSetRenderTarget?.Invoke(i);
            Targets[i] = target;
            PostSetRenderTarget?.Invoke(i);
        }

        public unsafe void AttachAll()
        {
            Bind();
            if (Targets != null)
                for (int i = 0; i < Targets.Length; ++i)
                    Attach(i);
            SetDrawBuffers();
            Unbind();
        }

        public event Action? SetDrawBuffersRequested;

        private unsafe void SetDrawBuffers()
            => SetDrawBuffersRequested?.Invoke();

        public void DetachAll()
        {
            Bind();
            if (Targets != null)
                for (int i = 0; i < Targets.Length; ++i)
                    Detach(i);
            Unbind();
        }
        public void Attach(int i)
        {
            var targets = Targets;
            if (targets is null)
                return;

            var (Target, Attachment, MipLevel, LayerIndex) = targets[i];
            switch (Target)
            {
                case XRTexture texture:
                    {
                        texture.Bind();
                        texture.PushData();

                        if (texture is XRTextureCube cuberef && LayerIndex >= 0 && LayerIndex < 6)
                            cuberef.AttachFaceToFBO(this, Attachment, ECubemapFace.PosX + LayerIndex, MipLevel);
                        else
                            texture.AttachToFBO(this, Attachment, MipLevel);
                        break;
                    }

                case XRRenderBuffer buf:
                    buf.Bind();
                    buf.AttachToFBO(this, Attachment);
                    break;
            }
        }
        public void Detach(int i)
        {
            if (Targets is null)
                return;

            var (Target, Attachment, MipLevel, LayerIndex) = Targets[i];
            switch (Target)
            {
                case XRTexture texture:
                    {
                        if (texture is XRTextureCube cuberef && LayerIndex >= 0 && LayerIndex < 6)
                            cuberef.DetachFaceFromFBO(this, Attachment, ECubemapFace.PosX + LayerIndex, MipLevel);
                        else
                            texture.DetachFromFBO(this, Attachment, MipLevel);
                        break;
                    }
                case XRRenderBuffer buf:
                    buf.Bind();
                    buf.DetachFromFBO(this, Attachment);
                    break;
            }
        }
    }
}
