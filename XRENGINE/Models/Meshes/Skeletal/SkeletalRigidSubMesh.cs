using XREngine.Data.Rendering;

namespace XREngine.Rendering.Models
{
    public class SkeletalRigidSubMesh : BaseSubMesh
    {
        public SkeletalRigidSubMesh() : base() { }

        public SkeletalRigidSubMesh(
            IVolume cullingVolume,
            XRMesh primitives,
            XRMaterial material) : base(cullingVolume, primitives, material) { }

        public SkeletalRigidSubMesh(
            IVolume cullingVolume,
            IEnumerable<LOD> lods) : base(cullingVolume, lods) { }

        public SkeletalRigidSubMesh(
            IVolume cullingVolume,
            params LOD[] lods) : base(cullingVolume, lods) { }
    }
}
