using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct OrientedBoundingBox
    {
        public Vector3 Center;
        public Vector3 Extents;
        public Quaternion Orientation;

        public OrientedBoundingBox(Vector3 center, Vector3 extents, Quaternion orientation)
        {
            Center = center;
            Extents = extents;
            Orientation = orientation;
        }

        public OrientedBoundingBox(BoundingBox aabb, Quaternion orientation)
        {
            Center = aabb.Center;
            Extents = aabb.Size / 2;
            Orientation = orientation;
        }

        public BoundingBox GetAxisAlignedBoundingBox()
        {
            Vector3[] corners = GetCorners();

            Vector3 min = corners[0];
            Vector3 max = corners[0];

            for (int i = 1; i < corners.Length; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }

            return new BoundingBox(min, max);
        }

        public Vector3[] GetCorners()
        {
            Vector3[] corners = new Vector3[8];
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(Orientation);

            corners[0] = Vector3.Transform(new Vector3(-Extents.X, -Extents.Y, -Extents.Z), rotationMatrix) + Center;
            corners[1] = Vector3.Transform(new Vector3(Extents.X, -Extents.Y, -Extents.Z), rotationMatrix) + Center;
            corners[2] = Vector3.Transform(new Vector3(-Extents.X, Extents.Y, -Extents.Z), rotationMatrix) + Center;
            corners[3] = Vector3.Transform(new Vector3(Extents.X, Extents.Y, -Extents.Z), rotationMatrix) + Center;
            corners[4] = Vector3.Transform(new Vector3(-Extents.X, -Extents.Y, Extents.Z), rotationMatrix) + Center;
            corners[5] = Vector3.Transform(new Vector3(Extents.X, -Extents.Y, Extents.Z), rotationMatrix) + Center;
            corners[6] = Vector3.Transform(new Vector3(-Extents.X, Extents.Y, Extents.Z), rotationMatrix) + Center;
            corners[7] = Vector3.Transform(new Vector3(Extents.X, Extents.Y, Extents.Z), rotationMatrix) + Center;

            return corners;
        }
    }
}
