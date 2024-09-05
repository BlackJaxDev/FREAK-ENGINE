using XREngine.Data.Rendering;

namespace XREngine.Rendering.Models
{
    public class StaticSoftSubMesh : BaseSubMesh
    {
        public StaticSoftSubMesh() : base() { }

        public StaticSoftSubMesh(
            IVolume cullingVolume,
            XRMesh primitives,
            XRMaterial material) : base(cullingVolume, primitives, material) { }

        public StaticSoftSubMesh(
            IVolume cullingVolume,
            EventList<LOD> lods) : base(cullingVolume, lods) { }

        public StaticSoftSubMesh(
            IVolume cullingVolume,
            params LOD[] lods) : base(cullingVolume, lods) { }
    }
}
