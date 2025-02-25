using Extensions;
using System.Numerics;
using Valve.VR;
using XREngine.Data.Geometry;

namespace XREngine.Rendering
{
    /// <summary>
    /// Retrieves the view and projection matrices for a VR eye camera from OpenVR.
    /// </summary>
    /// <param name="leftEye"></param>
    /// <param name="nearPlane"></param>
    /// <param name="farPlane"></param>
    public class XROVRCameraParameters(bool leftEye, float nearPlane, float farPlane) 
        : XRCameraParameters(nearPlane, farPlane)
    {
        private bool _leftEye = leftEye;
        public bool LeftEye
        {
            get => _leftEye;
            set => SetField(ref _leftEye, value);
        }

        public override Vector2 GetFrustumSizeAtDistance(float drawDistance)
        {
            var invProj = GetProjectionMatrix().Inverted();
            float normDist = (drawDistance - NearZ) / (FarZ - NearZ);
            //unproject the the points on the clip space box at normalized distance
            Vector3 bottomLeft = Vector3.Transform(new Vector3(-1, -1, normDist), invProj);
            Vector3 bottomRight = Vector3.Transform(new Vector3(1, -1, normDist), invProj);
            Vector3 topLeft = Vector3.Transform(new Vector3(-1, 1, normDist), invProj);
            //calculate the size of the frustum at the given distance
            return new Vector2((bottomRight - bottomLeft).Length(), (topLeft - bottomLeft).Length());
        }

        protected override Matrix4x4 CalculateProjectionMatrix()
            => Engine.VRState.Api.IsHeadsetPresent && Engine.VRState.Api.CVR is not null
                ? Engine.VRState.Api.CVR.GetProjectionMatrix(LeftEye ? EVREye.Eye_Left : EVREye.Eye_Right, NearZ, FarZ).ToNumerics().Transposed()
                : Matrix4x4.Identity;

        protected override Frustum CalculateUntransformedFrustum()
            => new(GetProjectionMatrix().Inverted());
    }
}
