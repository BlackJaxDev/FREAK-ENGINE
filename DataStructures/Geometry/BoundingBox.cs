using System.Numerics;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Geometry
{
    /// <summary>
    /// Axis-aligned bounding box
    /// </summary>
    public struct BoundingBox
    {
        private Vector3 min;
        private Vector3 max;

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Vector3 Size
        {
            get => Max - Min;
            set
            {
                Vector3 center = Center;
                Vector3 extents = value / 2.0f;
                Min = center - extents;
                Max = center + extents;
            }
        }

        public Vector3 Extents
        {
            get => (Max - Min) / 2.0f;
            set
            {
                Vector3 center = Center;
                Vector3 extents = value;
                Min = center - extents;
                Max = center + extents;
            }
        }

        public Vector3 Center
        {
            get => (Max + Min) / 2.0f;
            set
            {

                Vector3 center = value;
                Vector3 extents = Extents;
                Min = center - extents;
                Max = center + extents;
            }
        }

        public float Volume => Size.X * Size.Y * Size.Z;

        public Vector3 Min { get => min; set => min = value; }
        public Vector3 Max { get => max; set => max = value; }

        public bool Contains(Vector3 point)
            => Vector3.Min(Min, point) == Min && Vector3.Max(Max, point) == Max;

        public bool Intersects(BoundingBox other) => 
            other.Min.X <= Max.X && other.Max.X >= Min.X &&
            other.Min.Y <= Max.Y && other.Max.Y >= Min.Y &&
            other.Min.Z <= Max.Z && other.Max.Z >= Min.Z;

        public void ExpandToInclude(Vector3 point)
        {
            Min = Vector3.Min(Min, point);
            Max = Vector3.Max(Max, point);
        }

        public void ExpandToInclude(BoundingBox other)
        {
            Min = Vector3.Min(Min, other.Min);
            Max = Vector3.Max(Max, other.Max);
        }

        public BoundingBox Transformed(Matrix4x4 matrix)
        {
            Vector3[] corners = GetCorners();
            Vector3 newMin = corners[0];
            Vector3 newMax = corners[0];

            for (int i = 1; i < corners.Length; i++)
            {
                corners[i] = Vector3.Transform(corners[i], matrix);
                newMin = Vector3.Min(newMin, corners[i]);
                newMax = Vector3.Max(newMax, corners[i]);
            }

            return new BoundingBox(newMin, newMax);
        }

        public Vector3[] GetCorners()
        {
            return new Vector3[]
            {
                new Vector3(Min.X, Min.Y, Min.Z),
                new Vector3(Max.X, Min.Y, Min.Z),
                new Vector3(Min.X, Max.Y, Min.Z),
                new Vector3(Max.X, Max.Y, Min.Z),
                new Vector3(Min.X, Min.Y, Max.Z),
                new Vector3(Max.X, Min.Y, Max.Z),
                new Vector3(Min.X, Max.Y, Max.Z),
                new Vector3(Max.X, Max.Y, Max.Z)
            };
        }

        public static Vec3[] GetCorners(Vec3 halfExtents, Matrix transform)
        {
            Vec3[] corners = new Vec3[8];
            corners[0] = transform.TransformPoint(new Vec3(-halfExtents.X, -halfExtents.Y, -halfExtents.Z));
            corners[1] = transform.TransformPoint(new Vec3(halfExtents.X, -halfExtents.Y, -halfExtents.Z));
            corners[2] = transform.TransformPoint(new Vec3(-halfExtents.X, halfExtents.Y, -halfExtents.Z));
            corners[3] = transform.TransformPoint(new Vec3(halfExtents.X, halfExtents.Y, -halfExtents.Z));
            corners[4] = transform.TransformPoint(new Vec3(-halfExtents.X, -halfExtents.Y, halfExtents.Z));
            corners[5] = transform.TransformPoint(new Vec3(halfExtents.X, -halfExtents.Y, halfExtents.Z));
            corners[6] = transform.TransformPoint(new Vec3(-halfExtents.X, halfExtents.Y, halfExtents.Z));
            corners[7] = transform.TransformPoint(new Vec3(halfExtents.X, halfExtents.Y, halfExtents.Z));
            return corners;
        }

        public override string ToString()
        {
            return $"BoundingBox (Min: {Min}, Max: {Max})";
        }
    }
}
