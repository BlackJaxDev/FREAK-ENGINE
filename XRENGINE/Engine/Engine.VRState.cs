using MagicPhysX;
using OpenVR.NET;
using OpenVR.NET.Devices;
using OpenVR.NET.Manifest;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Valve.VR;
using XREngine.Data.Core;

namespace XREngine
{
    public static partial class Engine
    {
        public static class VRState
        {
            private static VR? _api = null;
            public static VR Api => _api ??= new VR();

            public enum VRMode
            {
                /// <summary>
                /// This mode indicates the VR system is awaiting inputs from a client and will send rendered frames to the client.
                /// </summary>
                Server,
                /// <summary>
                /// This mode indicates the VR system is sending inputs to a server and will receive rendered fr.
                /// </summary>
                Client,
                Local,
            }

            public static ETrackingUniverseOrigin Origin { get; set; } = ETrackingUniverseOrigin.TrackingUniverseStanding;

            /// <summary>
            /// This method initializes the VR system in local mode.
            /// All VR input and rendering will be handled by this process.
            /// </summary>
            /// <param name="actionManifest"></param>
            /// <param name="vrManifest"></param>
            /// <param name="getEyeTextureHandleFunc"></param>
            /// <returns></returns>
            [RequiresDynamicCode("")]
            [RequiresUnreferencedCode("")]
            public static bool InitializeLocal(
                IActionManifest actionManifest,
                VrManifest vrManifest,
                Func<nint> getEyeTextureHandleFunc)
            {
                GetEyeTextureHandle = getEyeTextureHandleFunc;
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
            /// <summary>
            /// This method initializes the VR system in client mode.
            /// All VR input will be send to and handled by the server process and rendered frames will be sent to this process.
            /// </summary>
            /// <returns></returns>
            public static bool IninitializeClient()
            {
                return false;
            }
            /// <summary>
            /// This method initializes the VR system in server mode.
            /// VR input is sent to this process and rendered frames are sent to the client process to submit to OpenVR.
            /// </summary>
            /// <returns></returns>
            public static bool InitializeServer()
            {
                return false;
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
                nint? handle = GetEyeTextureHandle?.Invoke();
                if (handle is not null)
                    SubmitRender(handle.Value);
            }

            public static Func<nint>? GetEyeTextureHandle { get; set; }

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

            public static NamedPipeServerStream? PipeServer { get; private set; } = new("VRInputPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            public static NamedPipeClientStream? PipeClient { get; private set; } = new(".", "VRInputPipe", PipeDirection.Out, PipeOptions.Asynchronous);
            public static void StartInputClient()
            {
                PipeClient = new(".", "VRInputPipe", PipeDirection.Out, PipeOptions.Asynchronous);
                PipeClient.Connect();
            }
            private static void ProcessInputData(VRInputData? inputData)
            {
                if (inputData is null)
                    return;

                // Update the latest input data
                _latestInputData = inputData;
            }
            public static void StopInputServer()
            {
                if (PipeServer is null)
                    return;

                if (PipeServer.IsConnected)
                    PipeServer.Disconnect();
                
                PipeServer.Close();
                PipeServer.Dispose();
            }
            
            [RequiresUnreferencedCode("")]
            [RequiresDynamicCode("")]
            public static async Task SendInputs()
            {
                if (PipeClient is null)
                    return;

                try
                {
                    CaptureVRInputData();
                    await PipeClient.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_data)));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, $"Error sending input data: {ex.Message}");
                }
            }

            private static VRInputData _data = new();

            private static void CaptureVRInputData()
            {

            }

            public struct VRInputData
            {
                public ETrackedDeviceClass DeviceClass;
                public ETrackingResult TrackingResult;
                public bool Connected;
                public bool PoseValid;
                public Quaternion Rotation;
                public Vector3 Position;
                public Vector3 Velocity;
                public Vector3 AngularVelocity;
                public Quaternion RenderRotation;
                public Vector3 RenderPosition;
                public uint unPacketNum;
                public ulong ulButtonPressed;
                public ulong ulButtonTouched;
                public VRControllerAxis_t rAxis0; //VRControllerAxis_t[5]
                public VRControllerAxis_t rAxis1;
                public VRControllerAxis_t rAxis2;
                public VRControllerAxis_t rAxis3;
                public VRControllerAxis_t rAxis4;
            }

            private static StreamReader? _reader = null;
            private static StreamWriter? _writer = null;
            private static bool _waitingForInput = false;

            [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
            [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
            private static async Task InputListenerAsync()
            {
                Debug.Out("Waiting for VR input connection...");
                try
                {
                    _waitingForInput = true;
                    await PipeServer!.WaitForConnectionAsync();
                    _waitingForInput = false;
                    Debug.Out("VR input connection established.");
                    _reader = new(PipeServer);
                }
                catch (Exception ex)
                {
                    _waitingForInput = false;
                    Debug.LogException(ex, $"Error accepting VR input connection: {ex.Message}");
                }
            }

            private static DateTime _lastInputRead = DateTime.MinValue;

            [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
            [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
            private static async Task ReadVRInput()
            {
                if (_reader is null)
                    return;

                // Read input data from the pipe asynchronously
                string? jsonData = await _reader.ReadLineAsync();
                if (jsonData is null)
                {
                    if ((DateTime.Now - _lastInputRead).Seconds > 1)
                    {
                        Debug.Out("VR input client disconnected.");
                        _reader.Dispose();
                        _reader = null;
                    }
                    return;
                }
                _lastInputRead = DateTime.Now;
                ProcessInputData(JsonSerializer.Deserialize<VRInputData?>(jsonData));
            }

            private static VRInputData? _latestInputData = null;
        }
    }
}