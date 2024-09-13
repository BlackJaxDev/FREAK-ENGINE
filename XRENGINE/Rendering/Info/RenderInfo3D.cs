using XREngine.Data;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Trees;
using XREngine.Rendering.Commands;

namespace XREngine.Rendering.Info
{
    public class RenderInfo3D : RenderInfo, IOctreeItem
    {
        public override ITreeNode? TreeNode => OctreeNode;

        public static Func<IRenderable, RenderCommand[], RenderInfo3D>? ConstructorOverride { get; set; } = null;

        public static RenderInfo3D New(IRenderable owner, params RenderCommand[] renderCommands)
            => ConstructorOverride?.Invoke(owner, renderCommands) ?? new RenderInfo3D(owner, renderCommands);

        protected RenderInfo3D(IRenderable owner, params RenderCommand[] renderCommands)
            : base(owner, renderCommands) { }

        private IVolume? _cullingVolume;
        private OctreeNodeBase? _octreeNode;
        private bool _receivesShadows = true;
        private bool _castsShadows = true;
        private bool _visibleInLightingProbes = true;
        private bool _hiddenFromOwner = false;
        private bool _visibleToOwnerOnly = false;

        public bool HiddenFromOwner
        {
            get => _hiddenFromOwner;
            set => SetField(ref _hiddenFromOwner, value);
        }

        public bool VisibleToOwnerOnly
        {
            get => _visibleToOwnerOnly;
            set => SetField(ref _visibleToOwnerOnly, value);
        }

        /// <summary>
        /// The shape the rendering octree will use to determine occlusion and offscreen culling (visibility).
        /// </summary>
        public IVolume? CullingVolume
        {
            get => _cullingVolume;
            set => SetField(ref _cullingVolume, value);
        }

        /// <summary>
        /// The octree bounding box this object is currently located in.
        /// </summary>   
        public OctreeNodeBase? OctreeNode
        {
            get => _octreeNode;
            set => SetField(ref _octreeNode, value);
        }

        /// <summary>
        /// Used to render objects in the same pass in a certain order.
        /// Smaller value means rendered sooner, zero (exactly) means it doesn't matter.
        /// </summary>
        //[Browsable(false)]
        //public float RenderOrder => RenderOrderFunc is null ? 0.0f : RenderOrderFunc();

        public bool ReceivesShadows
        {
            get => _receivesShadows;
            set => SetField(ref _receivesShadows, value);
        }

        public bool CastsShadows
        {    
            get => _castsShadows;
            set => SetField(ref _castsShadows, value);
        }

        public bool VisibleInLightingProbes
        {
            get => _visibleInLightingProbes;
            set => SetField(ref _visibleInLightingProbes, value);
        }

        public virtual bool AllowRender(
            IVolume? cullingVolume,
            RenderCommandCollection passes,
            XRCamera camera) => IsVisible && (!passes.IsShadowPass || CastsShadows) && (CullingVolume is null || (cullingVolume?.Contains(CullingVolume) ?? EContainment.Contains) != EContainment.Disjoint);

        public bool Intersects(IVolume cullingVolume, bool containsOnly)
        {
            if (CullingVolume is null)
                return true;

            var containment = cullingVolume.Contains(CullingVolume);
            return containsOnly ? containment == EContainment.Contains : containment != EContainment.Disjoint;
        }
    }
}