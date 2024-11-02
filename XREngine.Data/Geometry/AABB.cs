using System.Numerics;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace XREngine.Data.Geometry
{
    /// <summary>
    /// Axis-aligned bounding box
    /// </summary>
    public struct AABB : IShape
    {
        private Vector3 _min;
        private Vector3 _max;

        public AABB(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public static AABB FromCenterSize(Vector3 center, Vector3 size)
        {
            Vector3 extents = size * 0.5f;
            return new AABB(center - extents, center + extents);
        }

        public static AABB FromSize(Vector3 size)
            => FromCenterSize(Vector3.Zero, size);

        [JsonIgnore]
        [YamlIgnore]
        public Vector3 Size
        {
            readonly get => Max - Min;
            set
            {
                Vector3 center = Center;
                Vector3 extents = value / 2.0f;
                Min = center - extents;
                Max = center + extents;
            }
        }

        [JsonIgnore]
        [YamlIgnore]
        public Vector3 Extents
        {
            readonly get => (Max - Min) / 2.0f;
            set
            {
                Vector3 center = Center;
                Vector3 extents = value;
                Min = center - extents;
                Max = center + extents;
            }
        }

        [JsonIgnore]
        [YamlIgnore]
        public Vector3 Center
        {
            readonly get => (Max + Min) / 2.0f;
            set
            {

                Vector3 center = value;
                Vector3 extents = Extents;
                Min = center - extents;
                Max = center + extents;
            }
        }

        [JsonIgnore]
        [YamlIgnore]
        public readonly float Volume => Size.X * Size.Y * Size.Z;

        public Vector3 Min
        {
            readonly get => _min;
            set => _min = value;
        }
        public Vector3 Max
        {
            readonly get => _max;
            set => _max = value;
        }

        public readonly bool Intersects(AABB other) => 
            other.Min.X <= Max.X && other.Max.X >= Min.X &&
            other.Min.Y <= Max.Y && other.Max.Y >= Min.Y &&
            other.Min.Z <= Max.Z && other.Max.Z >= Min.Z;

        public void ExpandToInclude(Vector3 point)
        {
            Min = Vector3.Min(Min, point);
            Max = Vector3.Max(Max, point);
        }

        public void ExpandToInclude(AABB other)
        {
            Min = Vector3.Min(Min, other.Min);
            Max = Vector3.Max(Max, other.Max);
        }

        /// <summary>
        /// Transforms the bounding box using the given transformer method.
        /// Then, recalculates the min and max to re-align with axes.
        /// </summary>
        /// <param name="transformer"></param>
        /// <returns></returns>
        public readonly AABB Transformed(Func<Vector3, Vector3>? transformer = null)
        {
            Vector3[] corners = GetCorners(transformer);
            Vector3 newMin = corners[0];
            Vector3 newMax = corners[0];
            for (int i = 1; i < corners.Length; i++)
            {
                newMin = Vector3.Min(newMin, corners[i]);
                newMax = Vector3.Max(newMax, corners[i]);
            }
            return new AABB(newMin, newMax);
        }

        /// <summary>
        /// Returns the corners of this box at its current position.
        /// Naming system (back, front, etc) is relative to a camera looking in the -Z direction (forward).
        /// [T = top, B = bottom]
        /// [B = back, F = front]
        /// [L = left,  R = right]
        /// </summary>
        public readonly void GetCorners(
            out Vector3 TBL,
            out Vector3 TBR,
            out Vector3 TFL,
            out Vector3 TFR,
            out Vector3 BBL,
            out Vector3 BBR,
            out Vector3 BFL,
            out Vector3 BFR)
            => GetCorners(Min, Max, out TBL, out TBR, out TFL, out TFR, out BBL, out BBR, out BFL, out BFR);

        /// <summary>
        /// Returns the corners of a box with the given minimum and maximum corner coordinates.
        /// Naming system (back, front, etc) is relative to a camera looking in the -Z direction (forward).
        /// [T = top, B = bottom]
        /// [B = back, F = front]
        /// [L = left,  R = right]
        /// </summary>
        public static void GetCorners(
            Vector3 min,
            Vector3 max,
            out Vector3 TBL,
            out Vector3 TBR,
            out Vector3 TFL,
            out Vector3 TFR,
            out Vector3 BBL,
            out Vector3 BBR,
            out Vector3 BFL,
            out Vector3 BFR)
        {
            float Top = max.Y;
            float Bottom = min.Y;
            float Front = max.Z;
            float Back = min.Z;
            float Right = max.X;
            float Left = min.X;

            TBL = new Vector3(Left, Top, Back);
            TBR = new Vector3(Right, Top, Back);

            TFL = new Vector3(Left, Top, Front);
            TFR = new Vector3(Right, Top, Front);

            BBL = new Vector3(Left, Bottom, Back);
            BBR = new Vector3(Right, Bottom, Back);

            BFL = new Vector3(Left, Bottom, Front);
            BFR = new Vector3(Right, Bottom, Front);
        }
        /// <summary>
        /// Returns the corners of this box transformed by the given matrix.
        /// Naming system (back, front, etc) is relative to a camera looking in the -Z direction (forward), before the matrix is applied.
        /// [T = top, B = bottom] 
        /// [B = back, F = front] 
        /// [L = left,  R = right]
        /// </summary>
        public readonly void GetCorners(
            Matrix4x4 transform,
            out Vector3 TBL,
            out Vector3 TBR,
            out Vector3 TFL,
            out Vector3 TFR,
            out Vector3 BBL,
            out Vector3 BBR,
            out Vector3 BFL,
            out Vector3 BFR)
            => GetCorners(Extents, transform, out TBL, out TBR, out TFL, out TFR, out BBL, out BBR, out BFL, out BFR);

        /// <summary>
        /// Returns the corners of a box with the given half extents and transformed by the given matrix.
        /// Naming system (back, front, etc) is relative to a camera looking in the -Z direction (forward), before the matrix is applied.
        /// [T = top, B = bottom] 
        /// [B = back, F = front] 
        /// [L = left,  R = right]
        /// </summary>
        public static void GetCorners(
            Vector3 halfExtents,
            Matrix4x4 transform,
            out Vector3 TBL,
            out Vector3 TBR,
            out Vector3 TFL,
            out Vector3 TFR,
            out Vector3 BBL,
            out Vector3 BBR,
            out Vector3 BFL,
            out Vector3 BFR)
        {
            float Top = halfExtents.Y;
            float Bottom = -halfExtents.Y;
            float Front = halfExtents.Z;
            float Back = -halfExtents.Z;
            float Right = halfExtents.X;
            float Left = -halfExtents.X;

            TBL = Vector3.Transform(new Vector3(Left, Top, Back), transform);
            TBR = Vector3.Transform(new Vector3(Right, Top, Back), transform);

            TFL = Vector3.Transform(new Vector3(Left, Top, Front), transform);
            TFR = Vector3.Transform(new Vector3(Right, Top, Front), transform);

            BBL = Vector3.Transform(new Vector3(Left, Bottom, Back), transform);
            BBR = Vector3.Transform(new Vector3(Right, Bottom, Back), transform);

            BFL = Vector3.Transform(new Vector3(Left, Bottom, Front), transform);
            BFR = Vector3.Transform(new Vector3(Right, Bottom, Front), transform);
        }
        public readonly Vector3[] GetCorners()
        {
            GetCorners(out Vector3 TBL, out Vector3 TBR, out Vector3 TFL, out Vector3 TFR, out Vector3 BBL, out Vector3 BBR, out Vector3 BFL, out Vector3 BFR);
            return [TBL, TBR, TFL, TFR, BBL, BBR, BFL, BFR];
        }
        public readonly Vector3[] GetCorners(Matrix4x4 transform)
        {
            GetCorners(transform, out Vector3 TBL, out Vector3 TBR, out Vector3 TFL, out Vector3 TFR, out Vector3 BBL, out Vector3 BBR, out Vector3 BFL, out Vector3 BFR);
            return [TBL, TBR, TFL, TFR, BBL, BBR, BFL, BFR];
        }
        public static Vector3[] GetCorners(Vector3 halfExtents, Matrix4x4 transform)
        {
            GetCorners(halfExtents, transform, out Vector3 TBL, out Vector3 TBR, out Vector3 TFL, out Vector3 TFR, out Vector3 BBL, out Vector3 BBR, out Vector3 BFL, out Vector3 BFR);
            return [TBL, TBR, TFL, TFR, BBL, BBR, BFL, BFR];
        }
        public static Vector3[] GetCorners(Vector3 boxMin, Vector3 boxMax)
        {
            GetCorners(boxMin, boxMax, out Vector3 TBL, out Vector3 TBR, out Vector3 TFL, out Vector3 TFR, out Vector3 BBL, out Vector3 BBR, out Vector3 BFL, out Vector3 BFR);
            return [TBL, TBR, TFL, TFR, BBL, BBR, BFL, BFR];
        }

        public readonly Vector3[] GetCorners(Func<Vector3, Vector3>? transformer = null)
        {
            var array = new Vector3[]
            {
                new(Min.X, Min.Y, Min.Z),
                new(Max.X, Min.Y, Min.Z),
                new(Min.X, Max.Y, Min.Z),
                new(Max.X, Max.Y, Min.Z),
                new(Min.X, Min.Y, Max.Z),
                new(Max.X, Min.Y, Max.Z),
                new(Min.X, Max.Y, Max.Z),
                new(Max.X, Max.Y, Max.Z)
            };

            if (transformer != null)
                for (int i = 0; i < array.Length; i++)
                    array[i] = transformer(array[i]);

            return array;
        }

        public static Vector3[] GetCorners(Vector3 halfExtents, Func<Vector3, Vector3>? transformer = null)
            => GetCornersFromMinMax(-halfExtents, halfExtents, transformer);

        public static Vector3[] GetCorners(Vector3 center, Vector3 halfExtents, Func<Vector3, Vector3>? transformer = null)
            => GetCornersFromMinMax(center - halfExtents, center + halfExtents, transformer);

        public static Vector3[] GetCornersFromMinMax(Vector3 min, Vector3 max, Func<Vector3, Vector3>? transformer = null)
        {
            Vector3[] corners =
            [
                new(min.X, min.Y, min.Z),
                new(max.X, min.Y, min.Z),
                new(min.X, max.Y, min.Z),
                new(max.X, max.Y, min.Z),
                new(min.X, min.Y, max.Z),
                new(max.X, min.Y, max.Z),
                new(min.X, max.Y, max.Z),
                new(max.X, max.Y, max.Z),
            ];

            if (transformer != null)
                for (int i = 0; i < corners.Length; i++)
                    corners[i] = transformer(corners[i]);

            return corners;
        }

        public override readonly string ToString()
            => $"BoundingBox (Min: {Min}, Max: {Max})";

        public static AABB FromCorners(Vector3 min, Vector3 max)
            => new()
            {
                Min = min,
                Max = max
            };

        public readonly bool Contains(Vector3 point)
            => Vector3.Min(Min, point) == Min && Vector3.Max(Max, point) == Max;

        public readonly EContainment Contains(AABB box)
        {
            if (box.Min.X < Min.X || box.Max.X > Max.X)
                return EContainment.Disjoint;
            if (box.Min.Y < Min.Y || box.Max.Y > Max.Y)
                return EContainment.Disjoint;
            if (box.Min.Z < Min.Z || box.Max.Z > Max.Z)
                return EContainment.Disjoint;
            return EContainment.Contains;
        }

        public readonly EContainment Contains(Sphere sphere)
        {
            static float DistSqr(float c, float min, float max)
            {
                if (c < min)
                    return (min - c) * (min - c);
                if (c > max)
                    return (c - max) * (c - max);
                return 0;
            }

            float sqrDist = 0.0f;
            for (int i = 0; i < 3; i++)
                sqrDist += DistSqr(sphere.Center[i], Min[i], Max[i]);

            if (sqrDist > sphere.Radius * sphere.Radius)
                return EContainment.Disjoint;

            for (int i = 0; i < 3; i++)
            {
                if (sphere.Center[i] - sphere.Radius < Min[i])
                    return EContainment.Intersects;
                if (sphere.Center[i] + sphere.Radius > Max[i])
                    return EContainment.Intersects;
            }

            return EContainment.Contains;
        }

        public EContainment Contains(Cone cone)
        {
            throw new NotImplementedException();
        }

        public EContainment Contains(Capsule shape)
        {
            throw new NotImplementedException();
        }

        public readonly Vector3 ClosestPoint(Vector3 point, bool clampToEdge)
        {
            Vector3 closest = Vector3.Clamp(point, Min, Max);
            if (clampToEdge)
            {
                for (int i = 0; i < 3; i++)
                {
                    float minDist = point[i] - Min[i];
                    float maxDist = Max[i] - point[i];

                    //Closer to min side?
                    if (minDist < maxDist)
                        closest[i] = Min[i]; //Clamp to min
                    else
                        closest[i] = Max[i]; //Otherwise clamp to max
                }
            }
            return closest;
        }

        readonly AABB Rendering.IVolume.GetAABB() => this;

        public static AABB Union(AABB bounds1, AABB bounds2)
        {
            Vector3 min = Vector3.Min(bounds1.Min, bounds2.Min);
            Vector3 max = Vector3.Max(bounds1.Max, bounds2.Max);
            return new AABB(min, max);
        }
        public static AABB Union(AABB bounds, Vector3 point)
        {
            Vector3 min = Vector3.Min(bounds.Min, point);
            Vector3 max = Vector3.Max(bounds.Max, point);
            return new AABB(min, max);
        }
        public static AABB Union(AABB bounds, Sphere sphere)
        {
            Vector3 min = Vector3.Min(bounds.Min, sphere.Center - new Vector3(sphere.Radius));
            Vector3 max = Vector3.Max(bounds.Max, sphere.Center + new Vector3(sphere.Radius));
            return new AABB(min, max);
        }
        public static AABB Intersection(AABB bounds1, AABB bounds2)
        {
            Vector3 min = Vector3.Max(bounds1.Min, bounds2.Min);
            Vector3 max = Vector3.Min(bounds1.Max, bounds2.Max);
            return new AABB(min, max);
        }
    }
}
