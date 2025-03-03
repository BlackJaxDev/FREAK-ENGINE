using Microsoft.VisualBasic;
using OpenVR.NET;
using OpenVR.NET.Devices;
using OpenVR.NET.Manifest;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Valve.VR;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.OpenGL;
using ETextureType = Valve.VR.ETextureType;

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

            private static readonly Dictionary<string, Dictionary<string, OpenVR.NET.Input.Action>> _actions = [];
            public static Dictionary<string, Dictionary<string, OpenVR.NET.Input.Action>> Actions => _actions;

            public static event Action<Dictionary<string, Dictionary<string, OpenVR.NET.Input.Action>>>? ActionsChanged;

            public static OpenVR.NET.Input.Action? GetAction<TCategory, TName>(TCategory category, TName name)
                where TCategory : struct, Enum
                where TName : struct, Enum
            {
                if (_actions.TryGetValue(category.ToString(), out var nameDic))
                    if (nameDic.TryGetValue(name.ToString(), out var action))
                        return action;
                return null;
            }

            public static bool TryGetAction<TCategory, TName>(TCategory category, TName name, [NotNullWhen(true)] out OpenVR.NET.Input.Action? action)
                where TCategory : struct, Enum
                where TName : struct, Enum
            {
                action = GetAction(category, name);
                return action is not null;
            }

            private static void CreateActions(IActionManifest actionManifest, VR vr)
            {
                _actions.Clear();
                foreach (var actionSet in actionManifest.ActionSets)
                {
                    var actions = actionManifest.ActionsForSet(actionSet);
                    foreach (var action in actions)
                    {
                        var a = action.CreateAction(vr, null);
                        if (a is null)
                            continue;

                        string categoryName = actionSet.Name.ToString();
                        if (!_actions.TryGetValue(categoryName, out var nameDic))
                            _actions.Add(categoryName, nameDic = []);

                        nameDic.Add(action.Name.ToString(), a);
                    }
                }
                ActionsChanged?.Invoke(_actions);
            }

            public static XRTexture2DArray? VRStereoViewTextureArray { get; private set; } = null;
            public static XRMaterialFrameBuffer? VRStereoRenderTarget { get; private set; } = null;
            
            public static XRTexture2D? VRLeftEyeViewTexture { get; private set; } = null;
            public static XRMaterialFrameBuffer? VRLeftEyeRenderTarget { get; private set; } = null;

            public static XRMaterialFrameBuffer? VRRightEyeRenderTarget { get; private set; } = null;
            public static XRTexture2D? VRRightEyeViewTexture { get; private set; } = null;

            public static AbstractRenderer? Renderer { get; set; } = null;

            private static bool InitSteamVR(IActionManifest actionManifest, VrManifest vrManifest)
            {
                var vr = Api;
                vr.DeviceDetected += OnDeviceDetected;
                if (!vr.TryStart(EVRApplicationType.VRApplication_Scene))
                {
                    Debug.LogWarning("Failed to initialize SteamVR.");
                    return false;
                }
                InstallApp(vrManifest);
                vr.SetActionManifest(actionManifest);
                CreateActions(actionManifest, vr);
                Time.Timer.UpdateFrame += Update;
                return true;
            }

            /// <summary>
            /// This method initializes the VR system in local mode.
            /// All VR input and rendering will be handled by this process.
            /// </summary>
            /// <param name="actionManifest"></param>
            /// <param name="vrManifest"></param>
            /// <param name="getEyeTextureHandleFunc"></param>
            /// <returns></returns>
            public static bool InitializeLocal(
                IActionManifest actionManifest,
                VrManifest vrManifest,
                XRWindow window)
            {
                if (!InitSteamVR(actionManifest, vrManifest))
                    return false;
                InitRender(window);
                return true;
            }

            private static void InitRender(XRWindow window)
            {
                window.RenderViewportsCallback += Render;
                Renderer = window.Renderer;

                uint rW = 0u, rH = 0u;
                Api.CVR.GetRecommendedRenderTargetSize(ref rW, ref rH);

                ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
                float hz = Api.CVR.GetFloatTrackedDeviceProperty(0, ETrackedDeviceProperty.Prop_DisplayFrequency_Float, ref error);
                if (error == ETrackedPropertyError.TrackedProp_Success && hz > 0.0f)
                {
                    Time.Timer.TargetRenderFrequency = hz;
                    Time.Timer.TargetUpdateFrequency = hz;
                    Time.Timer.FixedUpdateFrequency = hz / 3;
                }

                //Renderer.ResizeViewports(new Silk.NET.Maths.Vector2D<int>((int)rW * 2, (int)rH));
                //VRStereoRenderTarget = new XRMaterialFrameBuffer(new XRMaterial(
                //new[]
                //{
                //    VRStereoViewTexture = XRTexture2D.CreateFrameBufferTexture(
                //        rW * 2u,
                //        rH,
                //        EPixelInternalFormat.Rgba,
                //        EPixelFormat.Bgra,
                //        EPixelType.UnsignedByte,
                //        EFrameBufferAttachment.ColorAttachment0),
                //},
                //ShaderHelper.UnlitTextureFragForward()!));

                VRLeftEyeRenderTarget = new XRMaterialFrameBuffer(new XRMaterial(
                [
                    VRLeftEyeViewTexture = XRTexture2D.CreateFrameBufferTexture(
                        rW, rH,
                        EPixelInternalFormat.Rgba8,
                        EPixelFormat.Rgba,
                        EPixelType.UnsignedByte,
                        EFrameBufferAttachment.ColorAttachment0),
                ], ShaderHelper.UnlitTextureFragForward()!));

                VRRightEyeRenderTarget = new XRMaterialFrameBuffer(new XRMaterial(
                [
                    VRRightEyeViewTexture = XRTexture2D.CreateFrameBufferTexture(
                        rW, rH,
                        EPixelInternalFormat.Rgba8,
                        EPixelFormat.Rgba,
                        EPixelType.UnsignedByte,
                        EFrameBufferAttachment.ColorAttachment0),
                ], ShaderHelper.UnlitTextureFragForward()!));

                VRLeftEyeViewTexture.Resizable = false;
                VRLeftEyeViewTexture.SizedInternalFormat = ESizedInternalFormat.Rgba8;

                VRRightEyeViewTexture.Resizable = false;
                VRRightEyeViewTexture.SizedInternalFormat = ESizedInternalFormat.Rgba8;

                var leftVP = LeftEyeViewport = new XRViewport(window) { Index = 0 };
                var rightVP = RightEyeViewport = new XRViewport(window) { Index = 1 };
                leftVP.AllowUIRender = false;
                rightVP.AllowUIRender = false;
                leftVP.SetFullScreen();
                rightVP.SetFullScreen();
                leftVP.SetInternalResolution((int)rW, (int)rH, false);
                rightVP.SetInternalResolution((int)rW, (int)rH, false);
                leftVP.Resize(rW, rH, false);
                rightVP.Resize(rW, rH, false);
            }

            /// <summary>
            /// This method initializes the VR system in client mode.
            /// All VR input will be send to and handled by the server process and rendered frames will be sent to this process.
            /// </summary>
            /// <returns></returns>
            public static bool IninitializeClient(
                IActionManifest actionManifest,
                VrManifest vrManifest)
            {
                return InitSteamVR(actionManifest, vrManifest);
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

            /// <summary>
            /// VR-related transforms must subscribe to this event to recalculate their matrices directly before drawing.
            /// </summary>
            public static event Action? RecalcMatrixOnDraw;

            public static uint LastFrameSampleIndex { get; private set; } = 0;

            private static void Render()
            {
                //Begin drawing to the headset
                var drawContext = Api.UpdateDraw(Origin);

                //Update VR-related transforms
                RecalcMatrixOnDraw?.Invoke();

                //Render the scene to left and right eyes
                //TODO: stereoscopic rendering, OR parallel buffer rendering (I believe that can be done in Vulkan only?)
                LeftEyeViewport?.Render(VRLeftEyeRenderTarget);
                RightEyeViewport?.Render(VRRightEyeRenderTarget);

                //Submit the rendered frames to the headset
                nint? leftHandle = VRLeftEyeViewTexture?.APIWrappers?.FirstOrDefault()?.GetHandle();
                nint? rightHandle = VRRightEyeViewTexture?.APIWrappers?.FirstOrDefault()?.GetHandle();
                if (leftHandle is not null && rightHandle is not null)
                    SubmitRenders(leftHandle.Value, rightHandle.Value);

                if (Rendering.Settings.LogVRFrameTimes)
                    ReadStats();
            }

            private static void ReadStats()
            {
                Compositor_FrameTiming currentFrame = new();
                Compositor_FrameTiming previousFrame = new();
                currentFrame.m_nSize = (uint)Marshal.SizeOf<Compositor_FrameTiming>();
                previousFrame.m_nSize = (uint)Marshal.SizeOf<Compositor_FrameTiming>();
                Valve.VR.OpenVR.Compositor.GetFrameTiming(ref currentFrame, 0);
                Valve.VR.OpenVR.Compositor.GetFrameTiming(ref previousFrame, 1);

                uint currentFrameIndex = currentFrame.m_nFrameIndex;
                uint amountOfFramesSinceLast = currentFrameIndex - LastFrameSampleIndex;

                double gpuFrametimeMs = 0;
                double cpuFrametimeMs = 0;
                double totalFrametimeMs = 0;

                for (uint i = 0; i < amountOfFramesSinceLast; i++)
                {
                    Valve.VR.OpenVR.Compositor.GetFrameTiming(ref currentFrame, i);
                    Valve.VR.OpenVR.Compositor.GetFrameTiming(ref previousFrame, i + 1);

                    gpuFrametimeMs += currentFrame.m_flTotalRenderGpuMs;
                    cpuFrametimeMs += currentFrame.m_flNewFrameReadyMs - currentFrame.m_flNewPosesReadyMs + currentFrame.m_flCompositorRenderCpuMs;
                    totalFrametimeMs += (currentFrame.m_flSystemTimeInSeconds - previousFrame.m_flSystemTimeInSeconds) * 1000f;
                }

                gpuFrametimeMs /= amountOfFramesSinceLast;
                cpuFrametimeMs /= amountOfFramesSinceLast;
                totalFrametimeMs /= amountOfFramesSinceLast;

                LastFrameSampleIndex = currentFrameIndex;

                GpuFrametime = (float)gpuFrametimeMs;
                CpuFrametime = (float)cpuFrametimeMs;
                TotalFrametime = (float)totalFrametimeMs;
                Framerate = (int)(1.0f / totalFrametimeMs * 1000.0f);

                Debug.Out($"VR: {Framerate}fps / GPU: {MathF.Round(GpuFrametime, 2, MidpointRounding.AwayFromZero)}ms / CPU: {MathF.Round(CpuFrametime, 2, MidpointRounding.AwayFromZero)}ms");
            }

            public static float GpuFrametime { get; private set; } = 0;
            public static float CpuFrametime { get; private set; } = 0;
            public static float TotalFrametime { get; private set; } = 0;
            public static float Framerate { get; private set; } = 0;
            public static float MaxFrametime { get; private set; } = 0;

            public static XRViewport? LeftEyeViewport { get; private set; }
            public static XRViewport? RightEyeViewport { get; private set; }

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

            public static NamedPipeServerStream? PipeServer { get; private set; }
            public static NamedPipeClientStream? PipeClient { get; private set; }
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
                    PipeServer = new("VRInputPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
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

            public static void ExportAndSendHandles(OpenGLRenderer renderer, Process targetProcess)
            {
                uint memoryObject = renderer.CreateMemoryObject();
                uint semaphore = renderer.CreateSemaphore();

                IntPtr memoryHandle = renderer.GetMemoryObjectHandle(memoryObject);
                IntPtr semaphoreHandle = renderer.GetSemaphoreHandle(semaphore);

                IntPtr duplicatedMemoryHandle = DuplicateHandleForIPC(memoryHandle, targetProcess);
                IntPtr duplicatedSemaphoreHandle = DuplicateHandleForIPC(semaphoreHandle, targetProcess);

                SendHandlesViaNamedPipe(duplicatedMemoryHandle, duplicatedSemaphoreHandle);
            }

            private static IntPtr DuplicateHandleForIPC(IntPtr handle, Process targetProcess)
            {
                IntPtr currentProcessHandle = Process.GetCurrentProcess().Handle;
                IntPtr targetProcessHandle = targetProcess.Handle;

                bool success = DuplicateHandle(
                    currentProcessHandle,
                    handle,
                    targetProcessHandle,
                    out nint duplicatedHandle,
                    0,
                    false,
                    DUPLICATE_SAME_ACCESS
                );

                if (!success)
                {
                    throw new Exception("Failed to duplicate handle.");
                }

                return duplicatedHandle;
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool DuplicateHandle(
                IntPtr hSourceProcessHandle,
                IntPtr hSourceHandle,
                IntPtr hTargetProcessHandle,
                out IntPtr lpTargetHandle,
                uint dwDesiredAccess,
                bool bInheritHandle,
                uint dwOptions);

            public const uint DUPLICATE_SAME_ACCESS = 0x00000002;

            private static void SendHandlesViaNamedPipe(IntPtr memoryHandle, IntPtr semaphoreHandle, string pipeName = "HandlePipe")
            {
                using NamedPipeServerStream pipeServer = new(pipeName, PipeDirection.Out);
                Console.WriteLine("Waiting for connection...");
                pipeServer.WaitForConnection();

                using BinaryWriter writer = new(pipeServer);
                writer.Write(memoryHandle.ToInt64());
                writer.Write(semaphoreHandle.ToInt64());
            }

            private static void ReceiveHandlesViaNamedPipe(out IntPtr memoryHandle, out IntPtr semaphoreHandle, string pipeName = "HandlePipe")
            {
                using NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.In);
                Console.WriteLine("Connecting to server...");
                pipeClient.Connect();

                using BinaryReader reader = new(pipeClient);
                long memoryHandleValue = reader.ReadInt64();
                long semaphoreHandleValue = reader.ReadInt64();

                memoryHandle = new IntPtr(memoryHandleValue);
                semaphoreHandle = new IntPtr(semaphoreHandleValue);
            }

            public static unsafe void ReceiveAndImportHandles(OpenGLRenderer renderer)
            {
                if (renderer.EXTMemoryObject is null || renderer. EXTSemaphore is null)
                    return;

                ReceiveHandlesViaNamedPipe(out nint memoryHandle, out nint semaphoreHandle);

                uint sem = renderer.EXTSemaphore.GenSemaphore();
                uint mem = renderer.EXTMemoryObject.CreateMemoryObject();

                renderer.SetMemoryObjectHandle(mem, (void*)memoryHandle);
                renderer.SetSemaphoreHandle(sem, (void*)semaphoreHandle);
            }
        }
    }
}