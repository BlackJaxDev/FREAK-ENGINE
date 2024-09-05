//using XREngine.Core.Files;
//using XREngine.Data.Geometry;

//namespace XREngine.Rendering.Models
//{
//    public class SkeletalModel : XRAsset
//    {
//        public SkeletalModel() : base() { }
//        public SkeletalModel(string name) : this() { _name = name; }

//        public GlobalFileRef<Skeleton> SkeletonRef => _skeleton;
//        public EventList<SkeletalRigidSubMesh> RigidChildren => _rigidChildren;
//        public EventList<SkeletalSoftSubMesh> SoftChildren => _softChildren;

//        public GlobalFileRef<Skeleton> _skeleton = new();
//        public EventList<SkeletalRigidSubMesh> _rigidChildren = [];
//        public EventList<SkeletalSoftSubMesh> _softChildren = [];

//        public AABB CalculateBindPoseCullingAABB()
//        {
//            AABB aabb = new();
//            foreach (var s in RigidChildren)
//                if (s.RenderInfo.CullingVolume != null)
//                    aabb.ExpandToInclude(s.RenderInfo.CullingVolume.GetAABB());
//            //foreach (var s in SoftChildren)
//            //    if (s.CullingVolume != null)
//            //        aabb.Expand(s.CullingVolume.GetAABB());
//            return aabb;
//        }

//        public BaseSubMesh[] CollectAllMeshes()
//        {
//            BaseSubMesh[] meshes = new BaseSubMesh[RigidChildren.Count + SoftChildren.Count];
//            for (int i = 0; i < RigidChildren.Count; ++i)
//                meshes[i] = RigidChildren[i];
//            for (int i = 0; i < SoftChildren.Count; ++i)
//                meshes[RigidChildren.Count + i] = SoftChildren[i];
//            return meshes;
//        }
//    }
//}
