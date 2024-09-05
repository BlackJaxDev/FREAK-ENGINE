using XREngine.Data.Core;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRRenderBuffer : GenericRenderObject, IFrameBufferAttachement
    {
        public ERenderBufferStorage Storage { get; set; } = ERenderBufferStorage.Rgba32f;
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
        public int MultisampleCount { get; set; } = 1;
        public XRFrameBuffer? AttachedFBO { get; set; } = null;

        public XREvent<XRRenderBuffer> BindRequested;
        public XREvent<XRRenderBuffer> UnbindRequested;
        public XREvent<XRRenderBuffer> AllocateRequested;

        public void Bind()
            => BindRequested.Invoke(this);
        public void Unbind()
            => UnbindRequested.Invoke(this);

        /// <summary>
        /// Allocates memory for the render buffer using the current width, height, storage format, and multisample count.
        /// </summary>
        public void Allocate()
            => AllocateRequested.Invoke(this);

        public void AttachToFBO(XRFrameBuffer fbo, EFrameBufferAttachment attachment)
        {

        }
        public void DetachFromFBO(XRFrameBuffer fbo, EFrameBufferAttachment attachment)
        {

        }

        public void AttachToFBO(XRFrameBuffer target, int mipLevel = 0)
        {

        }

        public void DetachFromFBO(XRFrameBuffer target, int mipLevel = 0)
        {

        }

        public void AttachToFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel = 0)
        {

        }

        public void DetachFromFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel = 0)
        {

        }
    }
}