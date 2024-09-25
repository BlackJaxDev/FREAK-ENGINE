using XREngine.Rendering.Info;

namespace XREngine.Components.Scene.Mesh
{
    public abstract class RenderableComponent : XRComponent, IRenderable
    {
        public RenderableComponent()
        {
            Meshes.CollectionChanged += Meshes_CollectionChanged;
        }

        private void Meshes_CollectionChanged(object sender, Core.TCollectionChangedEventArgs<RenderableMesh> e)
        {
            RenderedObjects = Meshes.Select(x => x.RenderInfo).ToArray();
        }

        public EventList<RenderableMesh> Meshes { get; private set; } = [];

        public RenderInfo[] RenderedObjects { get; private set; } = [];
    }
}
