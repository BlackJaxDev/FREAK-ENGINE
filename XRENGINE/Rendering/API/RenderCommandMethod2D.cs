﻿using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;

namespace XREngine.Rendering
{
    public class RenderCommandMethod2D : RenderCommand2D
    {
        public RenderCommandMethod2D(int renderPass, Action render)
            : base(renderPass) => Rendered += render;
        public RenderCommandMethod2D(Action render)
            : base((int)EDefaultRenderPass.OpaqueForward) => Rendered += render;
        public RenderCommandMethod2D(int renderPass)
            : base(renderPass) { }
        public RenderCommandMethod2D()
            : base((int)EDefaultRenderPass.OpaqueForward) { }

        public event Action? Rendered;

        public override void Render()
            => Rendered?.Invoke();
    }
}
