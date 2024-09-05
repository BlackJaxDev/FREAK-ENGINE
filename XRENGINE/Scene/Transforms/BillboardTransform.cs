using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Rendering;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Represents a transform that always faces the camera.
    /// Calculates the rotation on the CPU.
    /// Use shaders directly on meshes for better performance.
    /// </summary>
    /// <param name="parent"></param>
    public class BillboardTransform(TransformBase? parent) : TransformBase(parent)
    {
        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        private bool _perspective = false;
        public bool Perspective
        {
            get => _perspective;
            set => SetField(ref _perspective, value);
        }

        private Vector3? _constrainDir = null;
        public Vector3? ConstrainDir
        {
            get => _constrainDir;
            set => SetField(ref _constrainDir, value);
        }

        private bool _scaleByDistance = false;
        public bool ScaleByDistance
        {
            get => _scaleByDistance;
            set => SetField(ref _scaleByDistance, value);
        }

        private float _distanceScalar = 1.0f;
        public float DistanceScalar
        {
            get => _distanceScalar;
            set => SetField(ref _distanceScalar, value);
        }

        protected override Matrix4x4 CreateLocalMatrix()
        {
            if (!Enabled || XRCamera.CurrentRenderTarget is null)
                return Matrix4x4.Identity;

            //TODO: transform camera position to local scene node space
            //Create billboard matrix in local space using that position

            Vector3 toCamVec;
            if (Perspective)
            {
                //Billboard is calculated from the parent transform position to the camera transform position.
                Vector3 camPos = XRCamera.CurrentRenderTarget.Transform.WorldTranslation;
                toCamVec = camPos - WorldTranslation;
            }
            else
            {
                //Billboard is calculated from the parent transform position perpendicularly to the camera near plane.
                Vector3 camPlanePoint = GeoUtil.ClosestPointPlanePoint(XRCamera.CurrentRenderTarget.NearPlane(), WorldTranslation);
                toCamVec = camPlanePoint - WorldTranslation;
            }

            if (ConstrainDir.HasValue)
            {
                //Constrain dir dictates the only direction in which the billboard can rotate around.
                Vector3 constrained = Vector3.Cross(ConstrainDir.Value, toCamVec);
                toCamVec = Vector3.Cross(constrained, ConstrainDir.Value);
            }

            Matrix4x4 worldLookAt = Matrix4x4.CreateLookTo(XRCamera.CurrentRenderTarget.Transform.WorldTranslation, -toCamVec, Globals.Up);

            if (ScaleByDistance)
            {
                //Scale the billboard by the distance from the camera.
                float distance = toCamVec.Length();
                worldLookAt *= Matrix4x4.CreateScale(distance * DistanceScalar);
            }

            //Convert the world matrix to a local matrix
            return worldLookAt * ParentInverseWorldMatrix;
        }
    }
}