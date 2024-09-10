using OpenVR.NET.Devices;
using Valve.VR;
using XREngine.Components;

namespace XREngine.Data.Components.Scene
{
    public class VRControllerComponent : XRComponent
    {
        private bool _leftHand = true;
        public bool LeftHand 
        {
            get => _leftHand;
            set => SetField(ref _leftHand, value);
        }

        public VrDevice? Device { get; private set; }

        public VRControllerComponent()
            => Engine.VRState.Api.DeviceDetected += OnDeviceDetected;

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(LeftHand):
                    ReverifyTrackedDevices();
                    break;
            }
        }

        private void OnDeviceDetected(VrDevice device)
            => ReverifyTrackedDevices();

        private void ReverifyTrackedDevices()
        {
            var devices = Engine.VRState.Api.TrackedDevices;
            foreach (var dev in devices)
            {
                if (!dev.IsEnabled)
                    continue;

                var c = Engine.VRState.Api.CVR.GetTrackedDeviceClass(dev.DeviceIndex);
                if (c == ETrackedDeviceClass.Controller)
                {
                    ETrackedControllerRole role = Engine.VRState.Api.CVR.GetControllerRoleForTrackedDeviceIndex(dev.DeviceIndex);
                    if (role == ETrackedControllerRole.LeftHand && LeftHand)
                    {
                        Device = dev;
                        break;
                    }
                    else if (role == ETrackedControllerRole.RightHand && !LeftHand)
                    {
                        Device = dev;
                        break;
                    }
                }
            }
        }
    }
}
