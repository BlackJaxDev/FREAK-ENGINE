﻿using OpenVR.NET;
using OpenVR.NET.Devices;
using System.Numerics;
using Valve.VR;
using XREngine.Data.Core;

namespace XREngine
{
    public static partial class Engine
    {
        public static class VRState
        {
            private static VR? _api = null;
            public static VR Api => _api ??= Initialize();

            public static ETrackingUniverseOrigin Origin { get; set; } = ETrackingUniverseOrigin.TrackingUniverseStanding;

            public static VR Initialize()
            {
                var vr = new VR();
                vr.DeviceDetected += OnDeviceDetected;
                vr.TryStart();
                //vr.InstallApp(new VrManifest());
                //vr.SetActionManifest(new ActionManifest<GameAction, ActionCategory>());
                Time.Timer.UpdateFrame += Update;
                Time.Timer.RenderFrame += Render;
                return vr;
            }

            private static void Update()
            {
                Api.UpdateInput();
                Api.Update();
            }
            private static void Render()
            {
                Api.UpdateDraw(Origin);
            }

            private static void OnDeviceDetected(VrDevice device)
            {
                Debug.Out($"Device detected: {device}");
            }

            public class DevicePoseInfo : XRBase
            {
                public event Action<DevicePoseInfo>? ValidPoseChanged;
                public event Action<DevicePoseInfo>? IsConnectedChanged;
                public event Action<DevicePoseInfo>? Updated;

                public ETrackingResult State { get; private set; }
                public Matrix4x4 DeviceToWorldMatrix { get; private set; }

                public Vector3 Velocity { get; private set; }
                public Vector3 LastVelocity { get; private set; }

                public Vector3 AngularVelocity { get; private set; }
                public Vector3 LastAngularVelocity { get; private set; }

                public bool ValidPose { get; private set; }
                public bool IsConnected { get; private set; }

                public void Update(TrackedDevicePose_t pose)
                {
                    State = pose.eTrackingResult;
                    DeviceToWorldMatrix = pose.mDeviceToAbsoluteTracking.ToNumerics();

                    LastVelocity = Velocity;
                    Velocity = pose.vVelocity.ToNumerics();

                    LastAngularVelocity = AngularVelocity;
                    AngularVelocity = pose.vAngularVelocity.ToNumerics();

                    bool validChanged = ValidPose != pose.bPoseIsValid;
                    ValidPose = pose.bPoseIsValid;

                    bool isConnectedChanged = IsConnected != pose.bDeviceIsConnected;
                    IsConnected = pose.bDeviceIsConnected;

                    if (validChanged)
                        ValidPoseChanged?.Invoke(this);

                    if (isConnectedChanged)
                        IsConnectedChanged?.Invoke(this);

                    Updated?.Invoke(this);
                }
            }
        }
        //public enum GameAction
        //{

        //}
        //public enum ActionCategory
        //{

        //}
    }
}