﻿using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Components.Scene
{
    public class VRHeadsetComponent : XRComponent, IRenderable
    {
        protected VRHeadsetComponent() : base()
        {
            _leftEyeTransform = new VREyeTransform(true, Transform);
            _rightEyeTransform = new VREyeTransform(false, Transform);

            _leftEyeCamera = new(() => new XRCamera(_leftEyeTransform, _leftEyeParams), true);
            _rightEyeCamera = new(() => new XRCamera(_rightEyeTransform, _rightEyeParams), true);

            RenderInfo = RenderInfo3D.New(this, new RenderCommandMethod3D((int)EDefaultRenderPass.OpaqueForward, Render));
            RenderedObjects = [RenderInfo];
        }

        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();
            _leftEyeTransform.Parent = Transform;
            _rightEyeTransform.Parent = Transform;
        }

        private void Render(bool shadowPass)
        {
            if (shadowPass)
                return;

            var leftFrustum = _leftEyeCamera.Value.WorldFrustum();
            var rightFrustum = _rightEyeCamera.Value.WorldFrustum();

            Engine.Rendering.Debug.RenderFrustum(leftFrustum, ColorF4.Red);
            Engine.Rendering.Debug.RenderFrustum(rightFrustum, ColorF4.Red);

            //Engine.Rendering.Debug.RenderSphere(Transform.WorldTranslation, 0.1f, true, ColorF4.Black);
            //Engine.Rendering.Debug.RenderSphere(_leftEyeTransform.WorldTranslation, 0.1f, true, ColorF4.Cyan);
            //Engine.Rendering.Debug.RenderSphere(_rightEyeTransform.WorldTranslation, 0.1f, true, ColorF4.LightRed);
        }

        private readonly Lazy<XRCamera> _leftEyeCamera;
        private readonly Lazy<XRCamera> _rightEyeCamera;
        private readonly XROVRCameraParameters _leftEyeParams = new(true, 0.01f, 10000.0f);
        private readonly XROVRCameraParameters _rightEyeParams = new(false, 0.01f, 10000.0f);
        private readonly VREyeTransform _leftEyeTransform;
        private readonly VREyeTransform _rightEyeTransform;
        private float _near = 0.01f;
        private float _far = 10000.0f;

        public XRCamera LeftEyeCamera => _leftEyeCamera.Value;
        public XRCamera RightEyeCamera => _rightEyeCamera.Value;

        public float Near
        {
            get => _near;
            set => SetField(ref _near, value);
        }
        public float Far
        {
            get => _far;
            set => SetField(ref _far, value);
        }

        public RenderInfo3D RenderInfo { get; }
        public RenderInfo[] RenderedObjects { get; }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Near):
                    _leftEyeParams.NearZ = _rightEyeParams.NearZ = Near;
                    break;
                case nameof(Far):
                    _leftEyeParams.FarZ = _rightEyeParams.FarZ = Far;
                    break;
            }
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            XRViewport? leftEye = Engine.VRState.LeftEyeViewport;
            XRViewport? rightEye = Engine.VRState.RightEyeViewport;

            if (leftEye is null || rightEye is null)
            {
                Debug.LogWarning("VRHeadsetComponent requires the game to initailize with VR.");
                return;
            }

            leftEye.Camera = LeftEyeCamera;
            leftEye.WorldInstanceOverride = World;
            rightEye.Camera = RightEyeCamera;
            rightEye.WorldInstanceOverride = World;
        }
    }
}
