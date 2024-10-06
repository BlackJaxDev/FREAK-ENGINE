using System.Numerics;
using XREngine.Components;
using XREngine.Core.Attributes;
using XREngine.Rendering;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Components.Scene
{
    [RequiresTransform(typeof(VRHeadsetTransform))]
    public class VRHeadsetComponent : XRComponent
    {
        protected VRHeadsetComponent() : base()
        {
            _leftEyeTransform = new VREyeTransform(true, Transform);
            _rightEyeTransform = new VREyeTransform(false, Transform);

            _leftEyeCamera = new(() => new XRCamera(_leftEyeTransform, _leftEyeParams), true);
            _rightEyeCamera = new(() => new XRCamera(_rightEyeTransform, _rightEyeParams), true);
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
    }
}
