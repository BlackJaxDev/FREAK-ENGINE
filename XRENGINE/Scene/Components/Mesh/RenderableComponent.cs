using Extensions;
using XREngine.Rendering.Info;

namespace XREngine.Components.Scene.Mesh
{
    [Serializable]
    public abstract class RenderableComponent : XRComponent, IRenderable
    {
        public RenderableComponent()
        {
            Meshes.PostAnythingAdded += Meshes_PostAnythingAdded;
            Meshes.PostAnythingRemoved += Meshes_PostAnythingRemoved;
        }

        protected virtual void Meshes_PostAnythingRemoved(RenderableMesh item)
        {
            var ri = item.RenderInfo;
            int i = RenderedObjects.IndexOf(ri);
            if (i < 0)
                return;
            
            RenderedObjects = [.. RenderedObjects.Where((_, x) => x != i)];

            if (IsActive && ri.WorldInstance == World)
                ri.WorldInstance = null;
        }

        protected virtual void Meshes_PostAnythingAdded(RenderableMesh item)
        {
            var ri = item.RenderInfo;
            int i = RenderedObjects.IndexOf(ri);
            if (i >= 0)
                return;

            RenderedObjects = [.. RenderedObjects, ri];

            if (IsActive)
                ri.WorldInstance = World;
        }

        public EventList<RenderableMesh> Meshes { get; private set; } = [];
        public RenderInfo[] RenderedObjects { get; private set; } = [];
    }
}
