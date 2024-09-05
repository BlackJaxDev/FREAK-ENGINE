using Silk.NET.OpenGL;
using XREngine.Data.Rendering;
using static XREngine.Rendering.OpenGL.OpenGLRenderer;

namespace XREngine.Rendering.OpenGL
{
    public class GLFrameBuffer(OpenGLRenderer renderer, XRFrameBuffer data) : GLObject<XRFrameBuffer>(renderer, data)
    {
        public override GLObjectType Type => GLObjectType.Framebuffer;

        protected override void UnlinkData()
        {
            Data.SetDrawBuffersRequested -= SetDrawBuffers;
            Data.PreSetRenderTarget -= Value_PreSetRenderTarget;
            Data.PostSetRenderTarget -= Value_PostSetRenderTarget;
            Data.PreSetRenderTargets -= Value_PreSetRenderTargets;
            Data.PostSetRenderTargets -= Value_PostSetRenderTargets;
            Data.BindForReadRequested -= BindForReading;
            Data.BindForWriteRequested -= BindForWriting;
            Data.BindRequested -= Bind;
            Data.UnbindFromReadRequested -= UnbindFromReading;
            Data.UnbindFromWriteRequested -= UnbindFromWriting;
            Data.UnbindRequested -= Unbind;
            base.UnlinkData();
        }

        protected override void LinkData()
        {
            Data.SetDrawBuffersRequested += SetDrawBuffers;
            Data.PreSetRenderTarget += Value_PreSetRenderTarget;
            Data.PostSetRenderTarget += Value_PostSetRenderTarget;
            Data.PreSetRenderTargets += Value_PreSetRenderTargets;
            Data.PostSetRenderTargets += Value_PostSetRenderTargets;
            Data.BindForReadRequested += BindForReading;
            Data.BindForWriteRequested += BindForWriting;
            Data.BindRequested += Bind;
            Data.UnbindFromReadRequested += UnbindFromReading;
            Data.UnbindFromWriteRequested += UnbindFromWriting;
            Data.UnbindRequested += Unbind;
            base.LinkData();
        }

        private void Value_PreSetRenderTargets()
        {
            if (IsGenerated)
                Data.DetachAll();
        }
        private void Value_PostSetRenderTargets()
        {
            if (IsGenerated)
                Data.AttachAll();
        }

        private void Value_PreSetRenderTarget(int i)
        {
            Bind();

            if (IsGenerated)
                Data.Detach(i);
        }
        private void Value_PostSetRenderTarget(int i)
        {
            if (IsGenerated)
                Data.Attach(i);

            Unbind();
        }

        public void BindForReading()
        {
            Api.BindFramebuffer(GLEnum.ReadFramebuffer, BindingId);
        }
        public void UnbindFromReading()
        {
            //CheckErrors();
            Api.BindFramebuffer(GLEnum.ReadFramebuffer, 0);
        }

        public void BindForWriting()
        {
            Api.BindFramebuffer(GLEnum.DrawFramebuffer, BindingId);
        }
        public void UnbindFromWriting()
        {
            //CheckErrors();
            Api.BindFramebuffer(GLEnum.DrawFramebuffer, 0);
        }

        //Same as BindForWriting, technically
        public void Bind()
        {
            Api.BindFramebuffer(GLEnum.Framebuffer, BindingId);
        }
        //Same as UnbindFromWriting, technically
        public void Unbind()
        {
            //CheckErrors();
            Api.BindFramebuffer(GLEnum.Framebuffer, 0);
        }

        public unsafe void SetDrawBuffers()
        {
            var casted = Data.DrawBuffers?.Select(x => ToGLEnum(x))?.ToArray();
            if (casted != null)
            {
                fixed (GLEnum* drawBuffers = casted)
                {
                    Api.NamedFramebufferDrawBuffers(BindingId, (uint)(Data.DrawBuffers?.Length ?? 0), drawBuffers);
                }
            }
            Api.NamedFramebufferReadBuffer(BindingId, ColorBuffer.None);
            CheckErrors();
        }

        private static GLEnum ToGLEnum(EDrawBuffersAttachment x)
            => x switch
            {
                EDrawBuffersAttachment.ColorAttachment0 => GLEnum.ColorAttachment0,
                EDrawBuffersAttachment.ColorAttachment1 => GLEnum.ColorAttachment1,
                EDrawBuffersAttachment.ColorAttachment2 => GLEnum.ColorAttachment2,
                EDrawBuffersAttachment.ColorAttachment3 => GLEnum.ColorAttachment3,
                EDrawBuffersAttachment.ColorAttachment4 => GLEnum.ColorAttachment4,
                EDrawBuffersAttachment.ColorAttachment5 => GLEnum.ColorAttachment5,
                EDrawBuffersAttachment.ColorAttachment6 => GLEnum.ColorAttachment6,
                EDrawBuffersAttachment.ColorAttachment7 => GLEnum.ColorAttachment7,
                EDrawBuffersAttachment.ColorAttachment8 => GLEnum.ColorAttachment8,
                EDrawBuffersAttachment.ColorAttachment9 => GLEnum.ColorAttachment9,
                EDrawBuffersAttachment.ColorAttachment10 => GLEnum.ColorAttachment10,
                EDrawBuffersAttachment.ColorAttachment11 => GLEnum.ColorAttachment11,
                EDrawBuffersAttachment.ColorAttachment12 => GLEnum.ColorAttachment12,
                EDrawBuffersAttachment.ColorAttachment13 => GLEnum.ColorAttachment13,
                EDrawBuffersAttachment.ColorAttachment14 => GLEnum.ColorAttachment14,
                EDrawBuffersAttachment.ColorAttachment15 => GLEnum.ColorAttachment15,
                EDrawBuffersAttachment.None => GLEnum.None,
                _ => GLEnum.None,
            };

        public void CheckErrors()
            => Renderer.CheckFrameBufferErrors(this);

        protected internal override void PostGenerated()
            => Data.AttachAll();
    }
}
