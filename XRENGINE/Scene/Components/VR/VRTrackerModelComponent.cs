using OpenVR.NET.Devices;

namespace XREngine.Scene.Components.VR
{
    public class VRTrackerModelComponent : VRDeviceModelComponent
    {
        private uint? _deviceIndex;
        public uint? DeviceIndex
        {
            get => _deviceIndex;
            set => SetField(ref _deviceIndex, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(DeviceIndex):
                    Model?.Destroy();
                    Model = null;
                    break;
            }
        }

        protected override DeviceModel? GetRenderModel()
            => DeviceIndex is null ? null : Engine.VRState.Api.TrackedDevices.FirstOrDefault(d => d.DeviceIndex == DeviceIndex && Engine.VRState.Api.CVR.GetTrackedDeviceClass(d.DeviceIndex) == Valve.VR.ETrackedDeviceClass.GenericTracker)?.Model;
    }
}
