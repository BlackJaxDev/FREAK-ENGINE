using System.Collections;
using System.Numerics;
using XREngine.Data.Rendering;

namespace XREngine.Data.Geometry
{
    public readonly struct Frustum : IVolume, IEnumerable<Plane>
    {
        /// <summary>
        /// Returns frustum corners in the following order:
        /// left bottom near, 
        /// left top near, 
        /// right bottom near, 
        /// right top near, 
        /// left bottom far, 
        /// left top far, 
        /// right bottom far, 
        /// right top far
        /// </summary>
        public Vector3[] Corners =>
        [
            GeoUtil.Intersection(Left, Bottom, Near),
            GeoUtil.Intersection(Left, Top, Near),
            GeoUtil.Intersection(Right, Bottom, Near),
            GeoUtil.Intersection(Right, Top, Near),
            GeoUtil.Intersection(Left, Bottom, Far),
            GeoUtil.Intersection(Left, Top, Far),
            GeoUtil.Intersection(Right, Bottom, Far),
            GeoUtil.Intersection(Right, Top, Far)
        ];

        private readonly Plane[] _planes;

        public Plane Left => _planes[0];
        public Plane Right => _planes[1];
        public Plane Bottom => _planes[2];
        public Plane Top => _planes[3];
        public Plane Near => _planes[4];
        public Plane Far => _planes[5];

        private Frustum(Plane[] planes) => _planes = planes;
        public Frustum() => _planes = new Plane[6];
        public Frustum(Matrix4x4 mvp) : this()
        {
            // Indices for plane calculation
            int[,] indices = new int[,]
            {
                { +1, +0, +0, +1 },  // Left
                { -1, +0, +0, +1 },  // Right
                { +0, +1, +0, +1 },  // Bottom
                { +0, -1, +0, +1 },  // Top
                { +0, +0, +1, +1 },  // Near
                { +0, +0, -1, +1 }   // Far
            };

            for (int i = 0; i < 6; i++)
                _planes[i] = Plane.Normalize(new Plane(
                    mvp.M14 + indices[i, 0] * mvp.M11 + indices[i, 1] * mvp.M12 + indices[i, 2] * mvp.M13,
                    mvp.M24 + indices[i, 0] * mvp.M21 + indices[i, 1] * mvp.M22 + indices[i, 2] * mvp.M23,
                    mvp.M34 + indices[i, 0] * mvp.M31 + indices[i, 1] * mvp.M32 + indices[i, 2] * mvp.M33,
                    mvp.M44 + indices[i, 0] * mvp.M41 + indices[i, 1] * mvp.M42 + indices[i, 2] * mvp.M43));
        }

        public Plane this[int index]
        {
            get => _planes[index];
            set => _planes[index] = value;
        }

        public Frustum Clone() => new(_planes);

        public bool Intersects(AABB boundingBox)
        {
            for (int i = 0; i < 6; i++)
            {
                Plane plane = _planes[i];
                Vector3 point = new(
                    plane.Normal.X > 0 ? boundingBox.Min.X : boundingBox.Max.X,
                    plane.Normal.Y > 0 ? boundingBox.Min.Y : boundingBox.Max.Y,
                    plane.Normal.Z > 0 ? boundingBox.Min.Z : boundingBox.Max.Z);
                if (DistanceFromPointToPlane(point, plane) < 0)
                    return false;
            }

            return true;
        }

        public static float DistanceFromPointToPlane(Vector3 point, Plane plane)
        {
            Vector3 normal = new(plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
            return Math.Abs(Vector3.Dot(normal, point) + plane.D) / normal.Length();
        }

        public Frustum GetFrustumSlice(float startDepth, float endDepth)
        {
            Frustum f = Clone();
            f[4] = new Plane(_planes[4].Normal, _planes[4].D - startDepth);
            f[5] = new Plane(_planes[5].Normal, _planes[5].D + endDepth);
            return f;
        }

        public IEnumerator<Plane> GetEnumerator() => ((IEnumerable<Plane>)_planes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _planes.GetEnumerator();

        public EContainment Contains(Box box)
        {
            throw new NotImplementedException();
        }

        public EContainment Contains(AABB box)
        {
            throw new NotImplementedException();
        }

        public EContainment Contains(Sphere sphere)
        {
            throw new NotImplementedException();
        }
        
        public EContainment Contains(IVolume shape)
        {
            throw new NotImplementedException();
        }

        public EContainment Contains(Cone cone)
        {
            throw new NotImplementedException();
        }

        public bool Contains(Vector3 point)
        {
            throw new NotImplementedException();
        }

        public bool ContainedWithin(AABB boundingBox)
        {
            throw new NotImplementedException();
        }

        public EContainment Contains(Capsule shape)
        {
            throw new NotImplementedException();
        }

        public Vector3 ClosestPoint(Vector3 point, bool clampToEdge)
        {
            throw new NotImplementedException();
        }

        public AABB GetAABB()
        {
            var corners = Corners;
            Vector3 min = new(float.MaxValue);
            Vector3 max = new(float.MinValue);
            for (int i = 0; i < corners.Length; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }
            return new AABB(min, max);
        }
    }
}
