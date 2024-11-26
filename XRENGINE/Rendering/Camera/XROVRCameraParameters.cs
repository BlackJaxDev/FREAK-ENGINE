﻿using Extensions;
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

        protected override Matrix4x4 CalculateProjectionMatrix()
            => Engine.VRState.Api.CVR.GetProjectionMatrix(LeftEye ? EVREye.Eye_Left : EVREye.Eye_Right, NearZ, FarZ).ToNumerics().Transposed();

        protected override Frustum CalculateUntransformedFrustum()
            => new(GetProjectionMatrix().Inverted());
    }
}
