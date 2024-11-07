using XREngine.Core.Files;

namespace XREngine.Rendering.Models
{
    public class Model : XRAsset
    {
        public Model() { }
        public Model(params SubMesh[] meshes)
            => _meshes.AddRange(meshes);
        public Model(IEnumerable<SubMesh> meshes)
            => _meshes.AddRange(meshes);

        protected EventList<SubMesh> _meshes = [];
        public EventList<SubMesh> Meshes => _meshes;
    }
}
