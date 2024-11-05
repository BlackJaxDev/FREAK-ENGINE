using System.Numerics;
using XREngine.Data.Geometry;

namespace SimpleScene.Util.ssBVH
{
    public class SphereBVHNodeAdaptor : ISSBVHNodeAdaptor<Sphere>
    {
        protected BVH<Sphere>? _bvh;
        protected Dictionary<Sphere, BVHNode<Sphere>> _sphereToLeafMap = [];
        public BVH<Sphere>? BVH => _bvh;

        public void SetBVH(BVH<Sphere> bvh)
            => _bvh = bvh;

        public Vector3 ObjectPos(Sphere sphere)
            => sphere.Center;

        public float Radius(Sphere sphere)
            => sphere.Radius;

        public void CheckMap(Sphere sphere)
        {
            if (!_sphereToLeafMap.ContainsKey(sphere))
                throw new Exception("missing map for a shuffled child");
        }

        public void UnmapObject(Sphere sphere)
            => _sphereToLeafMap.Remove(sphere);

        public void MapObjectToBVHLeaf(Sphere sphere, BVHNode<Sphere> leaf)
            => _sphereToLeafMap[sphere] = leaf;

        public BVHNode<Sphere> GetLeaf(Sphere sphere)
            => _sphereToLeafMap[sphere];
    }
}

