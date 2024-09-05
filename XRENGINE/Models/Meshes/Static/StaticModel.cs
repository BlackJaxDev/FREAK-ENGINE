using XREngine.Core.Files;
using XREngine.Data.Geometry;
using XREngine.Physics;

namespace XREngine.Rendering.Models
{
    public class StaticModel : XRAsset
    {
        public StaticModel() { }
        public StaticModel(params StaticRigidSubMesh[] rigidMeshes)
            => _rigidChildren.AddRange(rigidMeshes);
        public StaticModel(params StaticSoftSubMesh[] softMeshes)
            => _softChildren.AddRange(softMeshes);
        public StaticModel(params BaseSubMesh[] meshes)
        {
            foreach (var m in meshes)
            {
                if (m is StaticRigidSubMesh rigid)
                    _rigidChildren.Add(rigid);
                else if (m is StaticSoftSubMesh soft)
                    _softChildren.Add(soft);
            }
        }
        public StaticModel(XRCollisionShape collisionShape, params StaticRigidSubMesh[] rigidMeshes) : this(rigidMeshes)
            => CollisionShape = collisionShape;
        public StaticModel(XRCollisionShape collisionShape, params StaticSoftSubMesh[] softMeshes) : this(softMeshes)
            => CollisionShape = collisionShape;
        public StaticModel(XRCollisionShape collisionShape, params BaseSubMesh[] meshes) : this(meshes)
            => CollisionShape = collisionShape;

        public EventList<StaticRigidSubMesh> RigidChildren => _rigidChildren;
        public EventList<StaticSoftSubMesh> SoftChildren => _softChildren;
        public XRCollisionShape? CollisionShape { get; set; }

        protected EventList<StaticRigidSubMesh> _rigidChildren = [];
        protected EventList<StaticSoftSubMesh> _softChildren = [];
        
        /// <summary>
        /// Calculates the fully-encompassing aabb for this model based on each child mesh's aabb.
        /// </summary>
        public AABB CalculateCullingAABB()
        {
            AABB aabb = new();
            foreach (var s in RigidChildren)
                if (s.RenderInfo?.CullingVolume != null)
                    aabb.ExpandToInclude(s.RenderInfo.CullingVolume.GetAABB());
            foreach (var s in SoftChildren)
                if (s.RenderInfo?.CullingVolume != null)
                    aabb.ExpandToInclude(s.RenderInfo.CullingVolume.GetAABB());
            return aabb;
        }
    }
}
