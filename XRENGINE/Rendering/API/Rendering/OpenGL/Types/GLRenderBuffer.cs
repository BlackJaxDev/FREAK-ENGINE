using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using XREngine.Data.Rendering;
using static XREngine.Rendering.OpenGL.OpenGLRenderer;

namespace XREngine.Rendering.OpenGL
{
    public class GLRenderBuffer(OpenGLRenderer renderer, XRRenderBuffer data) : GLObject<XRRenderBuffer>(renderer, data)
    {
        public override GLObjectType Type => GLObjectType.Renderbuffer;

        protected override void LinkData()
        {
            base.LinkData();
            Data.AllocateRequested += Allocate;
            Data.BindRequested += Bind;
            Data.UnbindRequested += Unbind;
            Data.AttachToFBORequested += AttachToFBO;
            Data.DetachFromFBORequested += DetachFromFBO;
        }

        protected override void UnlinkData()
        {
            base.UnlinkData();
            Data.AllocateRequested -= Allocate;
            Data.BindRequested -= Bind;
            Data.UnbindRequested -= Unbind;
            Data.AttachToFBORequested -= AttachToFBO;
            Data.DetachFromFBORequested -= DetachFromFBO;
        }

        public void Bind()
            => Api.BindRenderbuffer(GLEnum.Renderbuffer, BindingId);
        public void Unbind()
            => Api.BindRenderbuffer(GLEnum.Renderbuffer, 0);

        private void Allocate()
            => Api.NamedRenderbufferStorage(BindingId, ToGLEnum(Data.Type), Data.Width, Data.Height);

        public void AttachToFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel)
            => Api.NamedFramebufferRenderbuffer(Renderer.GenericToAPI<GLFrameBuffer>(target)!.BindingId, ToGLEnum(attachment), GLEnum.Renderbuffer, BindingId);
        public void DetachFromFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel)
            => Api.NamedFramebufferRenderbuffer(Renderer.GenericToAPI<GLFrameBuffer>(target)!.BindingId, ToGLEnum(attachment), GLEnum.Renderbuffer, 0);

        private static GLEnum ToGLEnum2(EFramebufferTarget fboType) => fboType switch
        {
            EFramebufferTarget.Framebuffer => GLEnum.Framebuffer,
            EFramebufferTarget.ReadFramebuffer => GLEnum.ReadFramebuffer,
            EFramebufferTarget.DrawFramebuffer => GLEnum.DrawFramebuffer,
            _ => GLEnum.Framebuffer
        };

        private static GLEnum ToGLEnum(ERenderBufferStorage type) => type switch
        {
            ERenderBufferStorage.DepthComponent => GLEnum.DepthComponent,
            ERenderBufferStorage.R3G3B2 => GLEnum.R3G3B2,
            ERenderBufferStorage.Rgb4 => GLEnum.Rgb4,
            ERenderBufferStorage.Rgb5 => GLEnum.Rgb5,
            ERenderBufferStorage.Rgb8 => GLEnum.Rgb8,
            ERenderBufferStorage.Rgb10 => GLEnum.Rgb10,
            ERenderBufferStorage.Rgb12 => GLEnum.Rgb12,
            ERenderBufferStorage.Rgb16 => GLEnum.Rgb16,
            ERenderBufferStorage.Rgba2 => GLEnum.Rgba2,
            ERenderBufferStorage.Rgba4 => GLEnum.Rgba4,
            ERenderBufferStorage.Rgba8 => GLEnum.Rgba8,
            ERenderBufferStorage.Rgb10A2 => GLEnum.Rgb10A2,
            ERenderBufferStorage.Rgba12 => GLEnum.Rgba12,
            ERenderBufferStorage.Rgba16 => GLEnum.Rgba16,
            ERenderBufferStorage.DepthComponent16 => GLEnum.DepthComponent16,
            ERenderBufferStorage.DepthComponent24 => GLEnum.DepthComponent24,
            ERenderBufferStorage.DepthComponent32 => GLEnum.DepthComponent32,
            ERenderBufferStorage.R8 => GLEnum.R8,
            ERenderBufferStorage.R16 => GLEnum.R16,
            ERenderBufferStorage.R16f => GLEnum.R16f,
            ERenderBufferStorage.R32f => GLEnum.R32f,
            ERenderBufferStorage.R8i => GLEnum.R8i,
            ERenderBufferStorage.R8ui => GLEnum.R8ui,
            ERenderBufferStorage.R16i => GLEnum.R16i,
            ERenderBufferStorage.R16ui => GLEnum.R16ui,
            ERenderBufferStorage.R32i => GLEnum.R32i,
            ERenderBufferStorage.R32ui => GLEnum.R32ui,
            ERenderBufferStorage.DepthStencil => GLEnum.DepthStencil,
            ERenderBufferStorage.Rgba32f => GLEnum.Rgba32f,
            ERenderBufferStorage.Rgb32f => GLEnum.Rgb32f,
            ERenderBufferStorage.Rgba16f => GLEnum.Rgba16f,
            ERenderBufferStorage.Rgb16f => GLEnum.Rgb16f,
            ERenderBufferStorage.Depth24Stencil8 => GLEnum.Depth24Stencil8,
            ERenderBufferStorage.R11fG11fB10f => GLEnum.R11fG11fB10f,
            ERenderBufferStorage.Rgb9E5 => GLEnum.Rgb9E5,
            ERenderBufferStorage.Srgb8 => GLEnum.Srgb8,
            ERenderBufferStorage.Srgb8Alpha8 => GLEnum.Srgb8Alpha8,
            ERenderBufferStorage.DepthComponent32f => GLEnum.DepthComponent32f,
            ERenderBufferStorage.Depth32fStencil8 => GLEnum.Depth32fStencil8,
            ERenderBufferStorage.StencilIndex1 => GLEnum.StencilIndex1,
            ERenderBufferStorage.StencilIndex4 => GLEnum.StencilIndex4,
            ERenderBufferStorage.StencilIndex8 => GLEnum.StencilIndex8,
            ERenderBufferStorage.StencilIndex16 => GLEnum.StencilIndex16,
            ERenderBufferStorage.Rgba32ui => GLEnum.Rgba32ui,
            ERenderBufferStorage.Rgb32ui => GLEnum.Rgb32ui,
            ERenderBufferStorage.Rgba16ui => GLEnum.Rgba16ui,
            ERenderBufferStorage.Rgb16ui => GLEnum.Rgb16ui,
            ERenderBufferStorage.Rgba8ui => GLEnum.Rgba8ui,
            ERenderBufferStorage.Rgb8ui => GLEnum.Rgb8ui,
            ERenderBufferStorage.Rgba32i => GLEnum.Rgba32i,
            ERenderBufferStorage.Rgb32i => GLEnum.Rgb32i,
            ERenderBufferStorage.Rgba16i => GLEnum.Rgba16i,
            ERenderBufferStorage.Rgb16i => GLEnum.Rgb16i,
            ERenderBufferStorage.Rgba8i => GLEnum.Rgba8i,
            ERenderBufferStorage.Rgb8i => GLEnum.Rgb8i,
            ERenderBufferStorage.Rgb10A2ui => GLEnum.Rgb10A2ui,
            _ => GLEnum.Rgba8
        };

        private static GLEnum ToGLEnum(EFrameBufferAttachment attachment) => attachment switch
        {
            EFrameBufferAttachment.FrontLeft => GLEnum.FrontLeft,
            EFrameBufferAttachment.FrontRight => GLEnum.FrontRight,
            EFrameBufferAttachment.BackLeft => GLEnum.BackLeft,
            EFrameBufferAttachment.BackRight => GLEnum.BackRight,
            EFrameBufferAttachment.Color => GLEnum.Color,
            EFrameBufferAttachment.Depth => GLEnum.Depth,
            EFrameBufferAttachment.Stencil => GLEnum.Stencil,
            EFrameBufferAttachment.DepthStencilAttachment => GLEnum.DepthStencilAttachment,
            EFrameBufferAttachment.ColorAttachment0 => GLEnum.ColorAttachment0,
            EFrameBufferAttachment.ColorAttachment1 => GLEnum.ColorAttachment1,
            EFrameBufferAttachment.ColorAttachment2 => GLEnum.ColorAttachment2,
            EFrameBufferAttachment.ColorAttachment3 => GLEnum.ColorAttachment3,
            EFrameBufferAttachment.ColorAttachment4 => GLEnum.ColorAttachment4,
            EFrameBufferAttachment.ColorAttachment5 => GLEnum.ColorAttachment5,
            EFrameBufferAttachment.ColorAttachment6 => GLEnum.ColorAttachment6,
            EFrameBufferAttachment.ColorAttachment7 => GLEnum.ColorAttachment7,
            EFrameBufferAttachment.ColorAttachment8 => GLEnum.ColorAttachment8,
            EFrameBufferAttachment.ColorAttachment9 => GLEnum.ColorAttachment9,
            EFrameBufferAttachment.ColorAttachment10 => GLEnum.ColorAttachment10,
            EFrameBufferAttachment.ColorAttachment11 => GLEnum.ColorAttachment11,
            EFrameBufferAttachment.ColorAttachment12 => GLEnum.ColorAttachment12,
            EFrameBufferAttachment.ColorAttachment13 => GLEnum.ColorAttachment13,
            EFrameBufferAttachment.ColorAttachment14 => GLEnum.ColorAttachment14,
            EFrameBufferAttachment.ColorAttachment15 => GLEnum.ColorAttachment15,
            EFrameBufferAttachment.DepthAttachment => GLEnum.DepthAttachment,
            EFrameBufferAttachment.StencilAttachment => GLEnum.StencilAttachment,
            _ => GLEnum.ColorAttachment0
        };
    }
}