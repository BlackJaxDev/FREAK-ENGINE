using OpenVR.NET.Devices;

namespace XREngine.Scene.Components.VR
{
    public class VRControllerModelComponent : VRDeviceModelComponent
    {
        private bool _leftHand = false;
        public bool LeftHand
        {
            get => _leftHand;
            set => SetField(ref _leftHand, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(LeftHand):
                    Model?.Destroy();
                    Model = null;
                    break;
            }
        }

        protected override DeviceModel? GetRenderModel()
            => LeftHand
                ? Engine.VRState.Api.LeftController?.Model
                : Engine.VRState.Api.RightController?.Model;
    }
}
