using XREngine.Data.Rendering;

namespace XREngine.Rendering.Models
{
    public class StaticRigidSubMesh : BaseSubMesh
    {
        public StaticRigidSubMesh() : base() { }

        public StaticRigidSubMesh(
            IVolume cullingVolume,
            XRMesh mesh,
            XRMaterial material) : base(cullingVolume, mesh, material) { }

        public StaticRigidSubMesh(
            IVolume cullingVolume,
            EventList<LOD> lods) : base(cullingVolume, lods) { }

        public StaticRigidSubMesh(
            IVolume cullingVolume,
            params LOD[] lods) : base(cullingVolume, lods) { }
    }
}
