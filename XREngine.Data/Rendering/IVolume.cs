using System.Numerics;
using XREngine.Data.Geometry;

namespace XREngine.Data.Rendering
{
    public interface IVolume
    {
        //bool ContainedWithin(IVolume volume)
        //    => volume.Contains(this) == EContainment.Contains;

        //EContainment ContainsGeneric(IVolume shape)
        //    => shape switch
        //    {
        //        AABB box => ContainsAABB(box),
        //        Sphere sphere => ContainsSphere(sphere),
        //        Cone cone => ContainsCone(cone),
        //        ConeX coneX => ContainsCone(coneX),
        //        ConeY coneY => ContainsCone(coneY),
        //        ConeZ coneZ => ContainsCone(coneZ),
        //        Capsule capsule => ContainsCapsule(capsule),
        //        CapsuleX capsuleX => ContainsCapsule(capsuleX),
        //        CapsuleY capsuleY => ContainsCapsule(capsuleY),
        //        CapsuleZ capsuleZ => ContainsCapsule(capsuleZ),
        //        Box box => ContainsBox(box),
        //        _ => EContainment.Disjoint
        //    };

        EContainment ContainsAABB(AABB box, float tolerance = float.Epsilon);
        EContainment ContainsBox(Box box);
        EContainment ContainsSphere(Sphere sphere);
        EContainment ContainsCone(Cone cone);
        EContainment ContainsCapsule(Capsule shape);

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
        bool ContainsPoint(Vector3 point, float tolerance = float.Epsilon);
        AABB GetAABB();
        bool IntersectsSegment(Segment segment, out Vector3[] points);
        bool IntersectsSegment(Segment segment);
    }
}