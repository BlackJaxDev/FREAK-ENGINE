﻿using XREngine.Data.Rendering;
using static XREngine.Rendering.XRTexture;

namespace XREngine.Rendering
{
    public class XRRenderBuffer : GenericRenderObject, IFrameBufferAttachement
    {
        private EFrameBufferAttachment? _frameBufferAttachment;
        private ERenderBufferStorage _type = ERenderBufferStorage.Rgba32f;
        private uint _width = 1u;
        private uint _height = 1u;
        private int _multisampleCount = 1;
        private XRFrameBuffer? _attachedFBO = null;

        public ERenderBufferStorage Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }
        public uint Width
        {
            get => _width;
            set => SetField(ref _width, value);
        }
        public uint Height
        {
            get => _height;
            set => SetField(ref _height, value);
        }
        public int MultisampleCount
        {
            get => _multisampleCount;
            set => SetField(ref _multisampleCount, value);
        }
        public XRFrameBuffer? AttachedFBO
        {
            get => _attachedFBO;
            set => SetField(ref _attachedFBO, value);
        }
        public EFrameBufferAttachment? FrameBufferAttachment
        {
            get => _frameBufferAttachment;
            set => SetField(ref _frameBufferAttachment, value);
        }

        public event DelAttachToFBO? AttachToFBORequested;
        public event DelDetachFromFBO? DetachFromFBORequested;
        public event Action? BindRequested;
        public event Action? UnbindRequested;
        public event Action? AllocateRequested;

        public void Bind()
            => BindRequested?.Invoke();

        public void Unbind()
            => UnbindRequested?.Invoke();

        /// <summary>
        /// Allocates memory for the render buffer using the current width, height, storage format, and multisample count.
        /// </summary>
        public void Allocate()
            => AllocateRequested?.Invoke();

        public void AttachToFBO(XRFrameBuffer target, int mipLevel = 0)
        {
            if (FrameBufferAttachment.HasValue)
                AttachToFBO(target, FrameBufferAttachment.Value, mipLevel);
        }

        public void DetachFromFBO(XRFrameBuffer target, int mipLevel = 0)
        {
            if (FrameBufferAttachment.HasValue)
                DetachFromFBO(target, FrameBufferAttachment.Value, mipLevel);
        }

        public void AttachToFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel = 0)
            => AttachToFBORequested?.Invoke(target, attachment, mipLevel);
        public void DetachFromFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel = 0)
            => DetachFromFBORequested?.Invoke(target, attachment, mipLevel);

    }
}