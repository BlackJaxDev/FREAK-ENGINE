using XREngine.Data.Rendering;

namespace XREngine.Rendering.Models
{
    public class SkeletalSoftSubMesh : BaseSubMesh
    {
        public SkeletalSoftSubMesh() : base() { }

        public SkeletalSoftSubMesh(
            IVolume cullingVolume,
            XRMesh primitives,
            XRMaterial material) : base(cullingVolume, primitives, material) { }

        public SkeletalSoftSubMesh(
            IVolume cullingVolume,
            IEnumerable<LOD> lods) : base(cullingVolume, lods) { }

        public SkeletalSoftSubMesh(
            IVolume cullingVolume,
            params LOD[] lods) : base(cullingVolume, lods) { }
    }
}
