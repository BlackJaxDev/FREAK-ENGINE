using XREngine.Components;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Data.Components
{
    public abstract class Shape3DComponent : XRComponent, IRenderable
    {
        private RenderInfo3D _renderInfo;

        public Shape3DComponent()
        {
            RenderedObjects = [_renderInfo = RenderInfo3D.New(this, GetRenderCommand())];
        }

        public virtual RenderInfo3D RenderInfo
        {
            get => _renderInfo;
            set
            {
                _renderInfo = value;
                RenderedObjects = [value];
            }
        }

        public RenderInfo[] RenderedObjects { get; private set; }

        protected abstract RenderCommand3D GetRenderCommand();
    }
}
