using System.Numerics;
using XREngine.Data.Geometry;

namespace XREngine.Data.Rendering
{
    public interface IVolume
    {
        bool ContainedWithin(IVolume volume)
            => volume.Contains(this) == EContainment.Contains;

        EContainment Contains(IVolume shape)
            => shape switch
            {
                AABB box => Contains(box),
                Sphere sphere => Contains(sphere),
                Cone cone => Contains(cone),
                Capsule capsule => Contains(capsule),
                _ => EContainment.Disjoint
            };

        EContainment Contains(AABB box);
        EContainment Contains(Sphere sphere);
        EContainment Contains(Cone cone);
        EContainment Contains(Capsule shape);

        /// <summary>
        /// Returns a point on the edge of the shape that is closest to the point in space given.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="clampToEdge">If true, and the point is inside the shape, will find the nearest point on the edge of the shape. If false, returns the same point given.</param>
        /// <returns></returns>
        Vector3 ClosestPoint(Vector3 point, bool clampToEdge);
        /// <summary>
        /// Returns true if the given point lies within the shape.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        bool Contains(Vector3 point);
        AABB GetAABB();
        bool Intersects(Segment segment, out Vector3[] points);
        bool Intersects(Segment segment);
    }
}