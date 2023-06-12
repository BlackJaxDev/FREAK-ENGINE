using System.Numerics;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Geometry
{
    public struct Frustum
    {
        private Plane[] _planes;

        public unsafe Frustum(Matrix modelViewProjectionMatrix)
        {
            _planes = new Plane[6];

            // Extract planes from the column-major model-view-projection matrix.
            for (int i = 0; i < 6; i++)
            {
                int col = i % 2;
                int sign = i < 2 ? 1 : -1;
                int row = 3 - i / 2;

                _planes[i] = new Plane(
                    modelViewProjectionMatrix[row] + sign * modelViewProjectionMatrix[4 * col],
                    modelViewProjectionMatrix[row + 4] + sign * modelViewProjectionMatrix[4 * col + 1],
                    modelViewProjectionMatrix[row + 8] + sign * modelViewProjectionMatrix[4 * col + 2],
                    modelViewProjectionMatrix[row + 12] + sign * modelViewProjectionMatrix[4 * col + 3]
                );
            }
        }

        public bool Intersects(BoundingBox boundingBox)
        {
            for (int i = 0; i < 6; i++)
            {
                Plane plane = _planes[i];
                Vec3 xyz = new(
                    plane.Normal.x > 0 ? boundingBox.MinX : boundingBox.MaxX,
                    plane.Normal.y > 0 ? boundingBox.MinY : boundingBox.MaxY,
                    plane.Normal.z > 0 ? boundingBox.MinZ : boundingBox.MaxZ);
                if (plane.DistanceTo(xyz) < 0)
                    return false;
            }

            return true;
        }

        public Frustum GetFrustumSlice(float startDepth, float endDepth)
        {
            Plane[] newPlanes = new Plane[6];
            Array.Copy(_planes, newPlanes, 6);

            // Calculate the direction of the near and far planes.
            Vector3 nearPlaneDirection = _planes[4].Normal;
            Vector3 farPlaneDirection = _planes[5].Normal;

            // Calculate the new near and far plane offsets.
            float newNearPlaneOffset = _planes[4].Distance - startDepth;
            float newFarPlaneOffset = _planes[5].Distance + endDepth;

            // Update the near and far plane equations.
            newPlanes[4] = new Plane(nearPlaneDirection, newNearPlaneOffset);
            newPlanes[5] = new Plane(farPlaneDirection, newFarPlaneOffset);

            return new Frustum(newPlanes);
        }

        private Frustum(Plane[] planes)
        {
            _planes = planes;
        }
    }
}
