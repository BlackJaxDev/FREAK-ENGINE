﻿using System.Collections;
using System.Numerics;
using XREngine.Data.Core;
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
        private readonly Vector3[] _corners = new Vector3[8];
        public IReadOnlyList<Vector3> Corners => _corners;

        private void ComputeCorners(Matrix4x4 mvp)
        {
            // Compute the inverse of the MVP matrix
            if (!Matrix4x4.Invert(mvp, out Matrix4x4 invMVP))
                throw new InvalidOperationException("Cannot invert the MVP matrix.");
            
            // Define the 8 corners of the unit cube in clip space
            Vector4[] clipSpaceCorners =
            [
                new(-1, -1, -1, 1), // Near bottom left
                new(-1, 1, -1, 1),  // Near top left
                new(1, -1, -1, 1),  // Near bottom right
                new(1, 1, -1, 1),   // Near top right
                new(-1, -1, 1, 1),  // Far bottom left
                new(-1, 1, 1, 1),   // Far top left
                new(1, -1, 1, 1),   // Far bottom right
                new(1, 1, 1, 1),    // Far top right
            ];

            // Transform the corners to world space
            for (int i = 0; i < 8; i++)
            {
                Vector4 corner = Vector4.Transform(clipSpaceCorners[i], invMVP);
                // Perform perspective divide
                corner /= corner.W;
                _corners[i] = new Vector3(corner.X, corner.Y, corner.Z);
            }
        }

        public Vector3 LeftBottomNear
        {
            get => _corners[0];
            private set => _corners[0] = value;
        }
        public Vector3 LeftTopNear
        {
            get => _corners[1];
            set => _corners[1] = value;
        }
        public Vector3 RightBottomNear
        {
            get => _corners[2];
            set => _corners[2] = value;
        }
        public Vector3 RightTopNear
        {
            get => _corners[3];
            set => _corners[3] = value;
        }
        public Vector3 LeftBottomFar
        {
            get => _corners[4];
            set => _corners[4] = value;
        }
        public Vector3 LeftTopFar
        {
            get => _corners[5];
            set => _corners[5] = value;
        }
        public Vector3 RightBottomFar
        {
            get => _corners[6];
            set => _corners[6] = value;
        }
        public Vector3 RightTopFar
        {
            get => _corners[7];
            set => _corners[7] = value;
        }

        private readonly Plane[] _planes = new Plane[6];
        public IReadOnlyList<Plane> Planes => _planes;

        private void ExtractPlanes(Matrix4x4 mvp)
        {
            // Left plane
            Left = new Plane(
                mvp.M14 + mvp.M11,
                mvp.M24 + mvp.M21,
                mvp.M34 + mvp.M31,
                mvp.M44 + mvp.M41);

            // Right plane
            Right = new Plane(
                mvp.M14 - mvp.M11,
                mvp.M24 - mvp.M21,
                mvp.M34 - mvp.M31,
                mvp.M44 - mvp.M41);

            // Bottom plane
            Bottom = new Plane(
                mvp.M14 + mvp.M12,
                mvp.M24 + mvp.M22,
                mvp.M34 + mvp.M32,
                mvp.M44 + mvp.M42);

            // Top plane
            Top = new Plane(
                mvp.M14 - mvp.M12,
                mvp.M24 - mvp.M22,
                mvp.M34 - mvp.M32,
                mvp.M44 - mvp.M42);

            // Near plane
            Near = new Plane(
                mvp.M13,
                mvp.M23,
                mvp.M33,
                mvp.M43);

            // Far plane
            Far = new Plane(
                mvp.M14 - mvp.M13,
                mvp.M24 - mvp.M23,
                mvp.M34 - mvp.M33,
                mvp.M44 - mvp.M43);

            // Normalize the planes
            for (int i = 0; i < 6; i++)
                _planes[i] = Plane.Normalize(_planes[i]);
        }

        public Plane Left
        {
            get => _planes[0];
            private set => _planes[0] = value;
        }

        public Plane Right
        {
            get => _planes[1];
            private set => _planes[1] = value;
        }

        public Plane Bottom
        {
            get => _planes[2];
            private set => _planes[2] = value;
        }

        public Plane Top
        {
            get => _planes[3];
            private set => _planes[3] = value;
        }

        public Plane Near
        {
            get => _planes[4];
            private set => _planes[4] = value;
        }

        public Plane Far
        {
            get => _planes[5];
            private set => _planes[5] = value;
        }

        private Frustum(Plane[] planes, Vector3[] corners)
        {
            _planes = planes;
            _corners = corners;
        }
        public Frustum() { }
        public Frustum(Matrix4x4 mvp) : this()
        {
            ExtractPlanes(mvp);
            ComputeCorners(mvp);
        }

        public Frustum(float width, float height, float nearPlane, float farPlane) : this()
        {
            float 
                w = width / 2.0f, 
                h = height / 2.0f;

            AABB.GetCorners(
                new Vector3(-w, -h, -farPlane),
                new Vector3(w, h, -nearPlane),
                out Vector3 ftl,
                out Vector3 ftr,
                out Vector3 ntl,
                out Vector3 ntr,
                out Vector3 fbl,
                out Vector3 fbr,
                out Vector3 nbl,
                out Vector3 nbr);

            UpdatePoints(fbl, fbr, ftl, ftr, nbl, nbr, ntl, ntr);

            //float halfWidth = width / 2.0f;
            //float halfHeight = height / 2.0f;

            //Vector3 nearTopLeft = new(-halfWidth, halfHeight, nearPlane);
            //Vector3 nearTopRight = new(halfWidth, halfHeight, nearPlane);
            //Vector3 nearBottomLeft = new(-halfWidth, -halfHeight, nearPlane);
            //Vector3 nearBottomRight = new(halfWidth, -halfHeight, nearPlane);

            //Vector3 farTopLeft = new(-halfWidth, halfHeight, farPlane);
            //Vector3 farTopRight = new(halfWidth, halfHeight, farPlane);
            //Vector3 farBottomLeft = new(-halfWidth, -halfHeight, farPlane);
            //Vector3 farBottomRight = new(halfWidth, -halfHeight, farPlane);

            //UpdatePoints(
            //    farBottomLeft, farBottomRight, farTopLeft, farTopRight,
            //    nearBottomLeft, nearBottomRight, nearTopLeft, nearTopRight);
        }

        public Frustum(
           float fovY,
           float aspect,
           float nearZ,
           float farZ,
           Vector3 forward,
           Vector3 up,
           Vector3 position)
           : this()
        {
            float
                tan = (float)Math.Tan(XRMath.DegToRad(fovY / 2.0f)),
                nearYDist = tan * nearZ,
                nearXDist = aspect * nearYDist,
                farYDist = tan * farZ,
                farXDist = aspect * farYDist;

            Vector3
                rightDir = Vector3.Cross(forward, up),
                nearPos = position + forward * nearZ,
                farPos = position + forward * farZ,
                nX = rightDir * nearXDist,
                fX = rightDir * farXDist,
                nY = up * nearYDist,
                fY = up * farYDist,
                ntl = nearPos + nY - nX,
                ntr = nearPos + nY + nX,
                nbl = nearPos - nY - nX,
                nbr = nearPos - nY + nX,
                ftl = farPos + fY - fX,
                ftr = farPos + fY + fX,
                fbl = farPos - fY - fX,
                fbr = farPos - fY + fX;

            UpdatePoints(fbl, fbr, ftl, ftr, nbl, nbr, ntl, ntr);
        }
        public Frustum(
            Vector3 farBottomLeft, Vector3 farBottomRight, Vector3 farTopLeft, Vector3 farTopRight,
            Vector3 nearBottomLeft, Vector3 nearBottomRight, Vector3 nearTopLeft, Vector3 nearTopRight) : this()
        {
            UpdatePoints(
                farBottomLeft, farBottomRight, farTopLeft, farTopRight,
                nearBottomLeft, nearBottomRight, nearTopLeft, nearTopRight);
        }
        private Frustum(
            Vector3 farBottomLeft, Vector3 farBottomRight, Vector3 farTopLeft, Vector3 farTopRight,
            Vector3 nearBottomLeft, Vector3 nearBottomRight, Vector3 nearTopLeft, Vector3 nearTopRight,
            Vector3 sphereCenter, float sphereRadius) : this()
            => UpdatePoints(
                farBottomLeft, farBottomRight, farTopLeft, farTopRight,
                nearBottomLeft, nearBottomRight, nearTopLeft, nearTopRight, sphereCenter, sphereRadius);

        public void UpdatePoints(
           Vector3 farBottomLeft, Vector3 farBottomRight, Vector3 farTopLeft, Vector3 farTopRight,
           Vector3 nearBottomLeft, Vector3 nearBottomRight, Vector3 nearTopLeft, Vector3 nearTopRight)
        {
            _corners[0] = farBottomLeft;
            _corners[1] = farBottomRight;
            _corners[2] = farTopLeft;
            _corners[3] = farTopRight;
            _corners[4] = nearBottomLeft;
            _corners[5] = nearBottomRight;
            _corners[6] = nearTopLeft;
            _corners[7] = nearTopRight;

            //near, far
            Near = Plane.CreateFromVertices(nearBottomRight, nearBottomLeft, nearTopRight);
            Far = Plane.CreateFromVertices(farBottomLeft, farBottomRight, farTopLeft);

            //left, right
            Left = Plane.CreateFromVertices(nearBottomLeft, farBottomLeft, nearTopLeft);
            Right = Plane.CreateFromVertices(farBottomRight, nearBottomRight, farTopRight);

            //top, bottom
            Top = Plane.CreateFromVertices(farTopLeft, farTopRight, nearTopLeft);
            Bottom = Plane.CreateFromVertices(nearBottomLeft, nearBottomRight, farBottomLeft);

            //CalculateBoundingSphere();
        }

        private void UpdatePoints(
            Vector3 farBottomLeft, Vector3 farBottomRight, Vector3 farTopLeft, Vector3 farTopRight,
            Vector3 nearBottomLeft, Vector3 nearBottomRight, Vector3 nearTopLeft, Vector3 nearTopRight,
            Vector3 sphereCenter, float sphereRadius)
        {
            _corners[0] = farBottomLeft;
            _corners[1] = farBottomRight;
            _corners[2] = farTopLeft;
            _corners[3] = farTopRight;
            _corners[4] = nearBottomLeft;
            _corners[5] = nearBottomRight;
            _corners[6] = nearTopLeft;
            _corners[7] = nearTopRight;

            //near, far
            Near = Plane.CreateFromVertices(nearBottomRight, nearBottomLeft, nearTopRight);
            Far = Plane.CreateFromVertices(farBottomLeft, farBottomRight, farTopLeft);

            //left, right
            Left = Plane.CreateFromVertices(nearBottomLeft, farBottomLeft, nearTopLeft);
            Right = Plane.CreateFromVertices(farBottomRight, nearBottomRight, farTopRight);

            //top, bottom
            Top = Plane.CreateFromVertices(farTopLeft, farTopRight, nearTopLeft);
            Bottom = Plane.CreateFromVertices(nearBottomLeft, nearBottomRight, farBottomLeft);

            //UpdateBoundingSphere(sphereCenter, sphereRadius);
        }
        //public Plane this[int index]
        //{
        //    get => _planes[index];
        //    private set => _planes[index] = value;
        //}

        public Frustum Clone()
            => new(_planes, _corners);

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

        /// <summary>
        /// Retrieves a slice of the frustum between two depths
        /// </summary>
        /// <param name="startDepth"></param>
        /// <param name="endDepth"></param>
        /// <returns></returns>
        public Frustum GetFrustumSlice(float startDepth, float endDepth)
        {
            Frustum f = Clone();
            f.Near = new Plane(_planes[4].Normal, _planes[4].D - startDepth);
            f.Far = new Plane(_planes[5].Normal, _planes[5].D + endDepth);
            return f;
        }

        public Plane GetBetweenNearAndFar(bool normalFacesNear)
            => GetBetween(normalFacesNear, Near, Far);
        public Plane GetBetweenLeftAndRight(bool normalFacesLeft)
            => GetBetween(normalFacesLeft, Left, Right);
        public Plane GetBetweenTopAndBottom(bool normalFacesTop)
            => GetBetween(normalFacesTop, Top, Bottom);
        public static Plane GetBetween(bool normalFacesFirst, Plane first, Plane second)
        {
            Vector3 topPoint = XRMath.GetPlanePoint(first);
            Vector3 bottomPoint = XRMath.GetPlanePoint(second);
            Vector3 normal = Vector3.Normalize(normalFacesFirst 
                ? second.Normal - first.Normal 
                : first.Normal - second.Normal);
            Vector3 midPoint = (topPoint + bottomPoint) / 2.0f;
            return XRMath.CreatePlaneFromPointAndNormal(midPoint, normal);
        }

        /// <summary>
        /// Divides the frustum into four frustum quadrants
        /// </summary>
        /// <returns></returns>
        public void DivideIntoFourths(
            out Frustum topLeft,
            out Frustum topRight,
            out Frustum bottomLeft,
            out Frustum bottomRight)
        {
            topLeft = Clone();
            //Fix bottom and right planes
            topLeft.Bottom = GetBetweenTopAndBottom(true);
            topLeft.Right = GetBetweenLeftAndRight(true);

            topRight = Clone();
            //Fix bottom and left planes
            topRight.Bottom = GetBetweenTopAndBottom(true);
            topRight.Left = GetBetweenLeftAndRight(false);

            bottomLeft = Clone();
            //Fix top and right planes
            bottomLeft.Top = GetBetweenTopAndBottom(false);
            bottomLeft.Right = GetBetweenLeftAndRight(true);

            bottomRight = Clone();
            //Fix top and left planes
            bottomRight.Top = GetBetweenTopAndBottom(false);
            bottomRight.Left = GetBetweenLeftAndRight(false);
        }

        public IEnumerator<Plane> GetEnumerator() => ((IEnumerable<Plane>)_planes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _planes.GetEnumerator();

        public EContainment Contains(Box box)
            => GeoUtil.FrustumContainsBox1(this, box.LocalExtents, box.Transform);

        public EContainment Contains(AABB box)
            => GeoUtil.FrustumContainsAABB(this, box.Min, box.Max);

        public EContainment Contains(Sphere sphere)
            => GeoUtil.FrustumContainsSphere(this, sphere.Center, sphere.Radius);

        public EContainment Contains(IVolume shape)
            => shape switch
            {
                AABB box => Contains(box),
                Sphere sphere => Contains(sphere),
                Cone cone => Contains(cone),
                Capsule capsule => Contains(capsule),
                _ => throw new NotImplementedException(),
            };

        public EContainment Contains(Cone cone)
            => GeoUtil.FrustumContainsCone(this, cone.Center, cone.Up, cone.Height, cone.Radius);

        public bool Contains(Vector3 point)
        {
            for (int i = 0; i < 6; i++)
                if (DistanceFromPointToPlane(point, _planes[i]) < 0)
                    return false;
            
            return true;
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
            var corners = _corners;
            Vector3 min = new(float.MaxValue);
            Vector3 max = new(float.MinValue);
            for (int i = 0; i < corners.Length; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }
            return new AABB(min, max);
        }

        public Frustum TransformedBy(Matrix4x4 worldMatrix)
        {
            Frustum f = new();
            for (int i = 0; i < 8; i++)
                f._corners[i] = Vector3.Transform(_corners[i], worldMatrix);
            for (int i = 0; i < 6; i++)
                f._planes[i] = Plane.Transform(_planes[i], worldMatrix);
            return f;
        }

        public override string ToString()
            => $"Frustum (Near: {Near}, Far: {Far}, Left: {Left}, Right: {Right}, Top: {Top}, Bottom: {Bottom})";
    }
}
