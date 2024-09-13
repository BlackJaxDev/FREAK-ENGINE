using XREngine.Core.Files;
using XREngine.Data.Geometry;

namespace XREngine.Rendering.Models
{
    public class Model : XRAsset
    {
        public Model() { }

        public Model(params SubMesh[] meshes)
            => _meshes.AddRange(meshes);

        public EventList<SubMesh> Meshes => _meshes;

        protected EventList<SubMesh> _meshes = [];
    }
}
