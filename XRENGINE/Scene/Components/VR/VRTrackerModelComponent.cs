﻿using OpenVR.NET.Devices;

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

        protected override DeviceModel? GetRenderModel(VrDevice? device)
        {
            if (DeviceIndex is null || device is null || device.DeviceIndex != DeviceIndex.Value)
                return null;

            return Engine.VRState.Api.CVR.GetTrackedDeviceClass(device.DeviceIndex) == Valve.VR.ETrackedDeviceClass.GenericTracker
                ? device.Model
                : null;
        }
    }
}
