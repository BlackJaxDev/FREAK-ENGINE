using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Trees;
using XREngine.Rendering.Commands;

namespace XREngine.Rendering.Info
{
    public class RenderInfo2D(IRenderable owner) : RenderInfo(owner), IQuadtreeItem
    {
        private int _layerIndex = 0;
        private int _indexWithinLayer = 0;
        private BoundingRectangleF _cullingVolume;
        private QuadtreeNodeBase? _quadtreeNode;

        /// <summary>
        /// Used to render objects in the same pass in a certain order.
        /// Smaller value means rendered sooner, zero (exactly) means it doesn't matter.
        /// </summary>
        public int LayerIndex 
        {
            get => _layerIndex;
            set => SetField(ref _layerIndex, value);
        }

        public int IndexWithinLayer
        {
            get => _indexWithinLayer;
            set => SetField(ref _indexWithinLayer, value);
        }

        /// <summary>
        /// The axis-aligned bounding box for this UI component.
        /// </summary>
        public BoundingRectangleF CullingVolume
        {
            get => _cullingVolume;
            set => SetField(ref _cullingVolume, value);
        }

        public QuadtreeNodeBase? QuadtreeNode
        {
            get => _quadtreeNode;
            set => SetField(ref _quadtreeNode, value);
        }

        public bool ShouldRender { get; } = true;

        public bool DeeperThan(RenderInfo2D other)
        {
            if (other is null)
                return true;

            if (LayerIndex > other.LayerIndex)
                return true;
            else if (LayerIndex == other.LayerIndex && IndexWithinLayer > other.IndexWithinLayer)
                return true;

            return false;
        }

        public virtual bool AllowRender(BoundingRectangleF? cullingVolume, RenderCommandCollection passes, XRCamera camera)
            => IsVisible && (cullingVolume is null || cullingVolume.Value.Intersects(CullingVolume));

        public bool Intersects(BoundingRectangleF cullingVolume, bool containsOnly)
            => containsOnly
                ? cullingVolume.Contains(CullingVolume)
                : cullingVolume.Intersects(CullingVolume);

        public bool Contains(Vector2 point)
            => CullingVolume.Contains(point);

        public Vector2 ClosestPoint(Vector2 point)
            => CullingVolume.Contains(point)
                ? point 
                : CullingVolume.ClosestPoint(point);

        public bool DeeperThan(IQuadtreeItem other)
            => other is RenderInfo2D other2D && DeeperThan(other2D);
    }
}