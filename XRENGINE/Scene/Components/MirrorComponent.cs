using System.Numerics;
using XREngine.Components;
using XREngine.Components.Scene.Mesh;
using XREngine.Data.Geometry;
using XREngine.Rendering.Info;

namespace XREngine.Data.Components
{
    //[RequireComponents(typeof(ModelComponent))]
    public class MirrorComponent : XRComponent, IRenderable
    {
        public ModelComponent Model => GetSiblingComponent<ModelComponent>(true)!;

        private float _mirrorHeight = 0.0f;
        public float MirrorHeight
        {
            get => _mirrorHeight;
            set => _mirrorHeight = value;
        }

        private float _mirrorWidth = 0.0f;
        public float MirrorWidth
        {
            get => _mirrorWidth;
            set => _mirrorWidth = value;
        }

        private readonly RenderInfo3D _renderInfo;

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(MirrorHeight):
                case nameof(MirrorWidth):
                    _renderInfo.LocalCullingVolume = new AABB(
                        new Vector3(0, 0, 0),
                        new Vector3(MirrorWidth, MirrorHeight, 0.001f));
                    break;
            }
        }

        //private XRRenderBuffer _renderBuffer = new()
        //{
        //    FrameBufferAttachment = Rendering.EFrameBufferAttachment.ColorAttachment0,
        //};

        public MirrorComponent()
        {
            //_renderInfo = RenderInfo3D.New(this);
            RenderedObjects = [];
        }
        public RenderInfo[] RenderedObjects { get; }
    }
}
