using OpenVR.NET;
using OpenVR.NET.Devices;
using OpenVR.NET.Manifest;
using System.IO;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Text.Json;
using Valve.VR;
using XREngine.Data.Core;
using System.Diagnostics.CodeAnalysis;

namespace XREngine
{
    public static partial class Engine
    {
        public static class VRState
        {
            private static VR? _api = null;
            public static VR Api => _api ??= new VR();

            public static ETrackingUniverseOrigin Origin { get; set; } = ETrackingUniverseOrigin.TrackingUniverseStanding;

            [RequiresDynamicCode("")]
            [RequiresUnreferencedCode("")]
            public static bool Initialize(IActionManifest actionManifest, VrManifest vrManifest)
            {
                var vr = Api;
                vr.DeviceDetected += OnDeviceDetected;
                if (!vr.TryStart(EVRApplicationType.VRApplication_Scene))
                {
                    Debug.LogWarning("Failed to start VR application");
                    return false;
                }
                InstallApp(vrManifest);
                vr.SetActionManifest(actionManifest);
                foreach (var actionSet in actionManifest.ActionSets)
                {
                    var actions = actionManifest.ActionsForSet(actionSet);
                    foreach (var action in actions)
                        action.CreateAction(vr, null);
                }
                Time.Timer.UpdateFrame += Update;
                Time.Timer.RenderFrame += Render;
                return true;
            }

            [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
            [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
            private static void InstallApp(VrManifest vrManifest)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), ".vrmanifest");
                File.WriteAllText(path, JsonSerializer.Serialize(new
                {
                    source = "builtin",
                    applications = new VrManifest[] { vrManifest }
                }, JSonOpts));

                //Valve.VR.OpenVR.Applications.RemoveApplicationManifest( path );
                var error = Valve.VR.OpenVR.Applications?.AddApplicationManifest(path, false);
                if (error != EVRApplicationError.None)
                    Debug.LogWarning($"Error installing app manifest: {error}");
            }

            private static readonly JsonSerializerOptions JSonOpts = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                IncludeFields = true
            };

            private static void Update()
            {
                Api.UpdateInput();
                Api.Update();
            }
            private static void Render()
            {
                var drawContext = Api.UpdateDraw(Origin);
                //nint handle = GetEyeTexHandle();
                //SubmitRender(handle);
            }

            private static void OnDeviceDetected(VrDevice device)
            {
                Debug.Out($"Device detected: {device}");
            }

            private static VRTextureBounds_t _singleTexBounds = new()
            {
                uMin = 0.0f,
                uMax = 1.0f,
                vMin = 0.0f,
                vMax = 1.0f,
            };
            private static VRTextureBounds_t _leftEyeTexBounds = new()
            {
                uMin = 0.0f,
                uMax = 0.5f,
                vMin = 0.0f,
                vMax = 1.0f,
            };

            private static VRTextureBounds_t _rightEyeTexBounds = new()
            {
                uMin = 0.5f,
                uMax = 1.0f,
                vMin = 0.0f,
                vMax = 1.0f,
            };

            private static Texture_t _eyeTex = new()
            {
                eColorSpace = EColorSpace.Auto,
            };

            public static void SubmitRenders(
                IntPtr leftEyeHandle,
                IntPtr rightEyeHandle,
                ETextureType apiType = ETextureType.OpenGL,
                EColorSpace colorSpace = EColorSpace.Auto,
                EVRSubmitFlags flags = EVRSubmitFlags.Submit_Default)
            {
                _eyeTex.eColorSpace = colorSpace;
                _eyeTex.eType = apiType;

                var comp = Valve.VR.OpenVR.Compositor;

                _eyeTex.handle = leftEyeHandle;
                CheckError(comp.Submit(EVREye.Eye_Left, ref _eyeTex, ref _singleTexBounds, flags));

                _eyeTex.handle = rightEyeHandle;
                CheckError(comp.Submit(EVREye.Eye_Right, ref _eyeTex, ref _singleTexBounds, flags));

                comp.PostPresentHandoff();
            }

            public static void SubmitRender(
                IntPtr eyesHandle,
                ETextureType apiType = ETextureType.OpenGL,
                EColorSpace colorSpace = EColorSpace.Auto,
                EVRSubmitFlags flags = EVRSubmitFlags.Submit_Default)
            {
                _eyeTex.eColorSpace = colorSpace;
                _eyeTex.handle = eyesHandle;
                _eyeTex.eType = apiType;

                var comp = Valve.VR.OpenVR.Compositor;
                CheckError(comp.Submit(EVREye.Eye_Left, ref _eyeTex, ref _leftEyeTexBounds, flags));
                CheckError(comp.Submit(EVREye.Eye_Right, ref _eyeTex, ref _rightEyeTexBounds, flags));

                comp.PostPresentHandoff();
            }

            //enum EVRSubmitFlags
            //{
            //    // Simple render path. App submits rendered left and right eye images with no lens distortion correction applied.
            //    Submit_Default = 0x00,

            //    // App submits final left and right eye images with lens distortion already applied (lens distortion makes the images appear
            //    // barrel distorted with chromatic aberration correction applied). The app would have used the data returned by
            //    // vr::IVRSystem::ComputeDistortion() to apply the correct distortion to the rendered images before calling Submit().
            //    Submit_LensDistortionAlreadyApplied = 0x01,

            //    // If the texture pointer passed in is actually a renderbuffer (e.g. for MSAA in OpenGL) then set this flag.
            //    Submit_GlRenderBuffer = 0x02,

            //    // Do not use
            //    Submit_Reserved = 0x04,

            //    // Set to indicate that pTexture is a pointer to a VRTextureWithPose_t.
            //    // This flag can be combined with Submit_TextureWithDepth to pass a VRTextureWithPoseAndDepth_t.
            //    Submit_TextureWithPose = 0x08,

            //    // Set to indicate that pTexture is a pointer to a VRTextureWithDepth_t.
            //    // This flag can be combined with Submit_TextureWithPose to pass a VRTextureWithPoseAndDepth_t.
            //    Submit_TextureWithDepth = 0x10,

            //    // Set to indicate a discontinuity between this and the last frame.
            //    // This will prevent motion smoothing from attempting to extrapolate using the pair.
            //    Submit_FrameDiscontinuty = 0x20,

            //    // Set to indicate that pTexture->handle is a contains VRVulkanTextureArrayData_t
            //    Submit_VulkanTextureWithArrayData = 0x40,

            //    // If the texture pointer passed in is an OpenGL Array texture, set this flag
            //    Submit_GlArrayTexture = 0x80,

            //    // If the texture is an EGL texture and not an glX/wGL texture (Linux only, currently)
            //    Submit_IsEgl = 0x100,

            //    // Do not use
            //    Submit_Reserved2 = 0x08000,
            //    Submit_Reserved3 = 0x10000,
            //};

            public static bool CheckError(EVRCompositorError error)
            {
                bool hasError = error != EVRCompositorError.None;
                if (hasError)
                    Debug.LogWarning($"OpenVR compositor error: {error}");
                return hasError;
            }

            internal static void Initialize(object vRActionManifest, object vRManifest)
            {
                throw new NotImplementedException();
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