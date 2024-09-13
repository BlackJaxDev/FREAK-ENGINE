using OpenVR.NET.Devices;
using Valve.VR;
using XREngine.Components;

namespace XREngine.Data.Components.Scene
{
    public class VRControllerComponent : XRComponent
    {
        public VRControllerComponent()
            => Engine.VRState.Api.DeviceDetected += OnDeviceDetected;

        private bool _leftHand = true;
        private VrDevice? _device;

        public bool LeftHand 
        {
            get => _leftHand;
            set => SetField(ref _leftHand, value);
        }

        public VrDevice? Device
        {
            get => _device;
            private set => SetField(ref _device, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Device):
                        if (Device is not null)
                        {
                            UnregisterTick(ETickGroup.Normal, ETickOrder.Input, PollDevice);
                        }
                        break;
                }
            }
            return change;
        }

        private void PollDevice()
        {

        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(LeftHand):
                    ReverifyTrackedDevices();
                    break;
                case nameof(Device):
                    if (Device is not null)
                    {
                        Engine.VRState.Api.DeviceDetected -= OnDeviceDetected;
                        RegisterTick(ETickGroup.Normal, ETickOrder.Input, PollDevice);
                    }
                    else
                        Engine.VRState.Api.DeviceDetected += OnDeviceDetected;
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
                if (c != ETrackedDeviceClass.Controller)
                    continue;
                
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
