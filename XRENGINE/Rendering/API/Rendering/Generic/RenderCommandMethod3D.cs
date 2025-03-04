﻿using XREngine.Data.Rendering;

namespace XREngine.Rendering.Commands
{
    public class RenderCommandMethod3D : RenderCommand3D
    {
        public RenderCommandMethod3D(int renderPass, DelRender render)
            : base(renderPass) => Rendered += render;
        public RenderCommandMethod3D(DelRender rendered)
            : base((int)EDefaultRenderPass.OpaqueForward) => Rendered += rendered;
        public RenderCommandMethod3D(int renderPass)
            : base(renderPass) { }
        public RenderCommandMethod3D()
            : base((int)EDefaultRenderPass.OpaqueForward) { }

        public delegate void DelRender();

        public event DelRender? Rendered;

        public override void Render()
            => Rendered?.Invoke();
    }
}
