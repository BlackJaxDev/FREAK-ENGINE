namespace XREngine.Components.Scene.Mesh
{
    public class RenderableComponent : XRComponent
    {
        public EventList<RenderableMesh> Meshes { get; private set; } = [];
    }
}
