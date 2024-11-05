using XREngine.Data.Geometry;

namespace SimpleScene.Util.ssBVH
{

    public class SphereBVH(int maxSpheresPerLeaf = 1)
        : BVH<Sphere>(new SphereBVHNodeAdaptor(), [], maxSpheresPerLeaf) { }
}

