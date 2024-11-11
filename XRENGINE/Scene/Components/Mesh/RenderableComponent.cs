using XREngine.Rendering.Info;

namespace XREngine.Components.Scene.Mesh
{
    public abstract class RenderableComponent : XRComponent, IRenderable
    {
        public RenderableComponent()
        {
            Meshes.PostAnythingAdded += Meshes_PostAnythingAdded;
            Meshes.PostAnythingRemoved += Meshes_PostAnythingRemoved;
        }

        protected void Meshes_PostAnythingRemoved(RenderableMesh item)
        {
            if (!RenderedObjects.Contains(item.RenderInfo))
                return;
            
            if (item.RenderInfo.WorldInstance == World)
                item.RenderInfo.WorldInstance = null;
            RenderedObjects = RenderedObjects.Where(x => x != item.RenderInfo).ToArray();
        }

        protected virtual void Meshes_PostAnythingAdded(RenderableMesh item)
        {
            if (RenderedObjects.Contains(item.RenderInfo))
                return;

            RenderedObjects = [.. RenderedObjects, item.RenderInfo];
            item.RenderInfo.WorldInstance = World;
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(World):
                    foreach (var ro in RenderedObjects)
                        ro.WorldInstance = World;
                    break;
            }
        }

        public EventList<RenderableMesh> Meshes { get; private set; } = [];

        public RenderInfo[] RenderedObjects { get; private set; } = [];
    }
}
