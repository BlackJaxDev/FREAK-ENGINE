﻿using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;

namespace XREngine.Rendering
{
    public class XRPerspectiveCameraParameters(float verticalFieldOfView, float? aspectRatio, float nearPlane, float farPlane) 
        : XRCameraParameters(nearPlane, farPlane)
    {
        private float _aspectRatio = aspectRatio ?? 1.0f;
        private bool _inheritAspectRatio = aspectRatio is null;

        /// <summary>
        /// Field of view on the Y axis in degrees.
        /// </summary>
        public float VerticalFieldOfView
        {
            get => verticalFieldOfView;
            set => SetField(ref verticalFieldOfView, value);
        }

        /// <summary>
        /// Field of view on the X axis in degrees.
        /// </summary>
        public float HorizontalFieldOfView
        {
            get => VerticalFieldOfView * AspectRatio;
            set => VerticalFieldOfView = value / AspectRatio;
        }

        /// <summary>
        /// The aspect ratio of the camera, calculated as width / height.
        /// </summary>
        public float AspectRatio
        {
            get => _aspectRatio;
            set => SetField(ref _aspectRatio, value);
        }

        /// <summary>
        /// If true, the aspect ratio will be inherited from the aspect ratio of the viewport.
        /// </summary>
        public bool InheritAspectRatio
        {
            get => _inheritAspectRatio;
            set => SetField(ref _inheritAspectRatio, value);
        }

        /// <summary>
        /// Easy way to set the aspect ratio by providing the width and height of the camera.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetAspectRatio(float width, float height)
            => AspectRatio = width / height;

        protected override Matrix4x4 CalculateProjectionMatrix()
            => Matrix4x4.CreatePerspectiveFieldOfView(XRMath.DegToRad(VerticalFieldOfView), AspectRatio, NearZ, FarZ);

        protected override Frustum CalculateUntransformedFrustum()
            => new(VerticalFieldOfView, AspectRatio, NearZ, FarZ, Globals.Forward, Globals.Up, Vector3.Zero);

        public override void SetUniforms(XRRenderProgram program)
        {
            base.SetUniforms(program);
            program.Uniform(EEngineUniform.CameraFovY.ToString(), VerticalFieldOfView);
            program.Uniform(EEngineUniform.CameraFovX.ToString(), HorizontalFieldOfView);
            program.Uniform(EEngineUniform.CameraAspect.ToString(), AspectRatio);
        }
    }
}
