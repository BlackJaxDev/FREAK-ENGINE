using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.HTC;
using Silk.NET.OpenXR.Extensions.HTCX;
using System.IO;
using System.Runtime.InteropServices;
using XREngine.Data.Lists.Unsafe;
using XREngine.Rendering.Graphics.Renderers;

namespace XREngine.Rendering
{
    public unsafe partial class OpenXR : AbstractRenderer<XR>
    {
        /// <summary>
        /// https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_KHR_vulkan_enable
        /// </summary>
        public const string XR_KHR_vulkan_enable = "XR_KHR_vulkan_enable";

        /// <summary>
        /// https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_KHR_vulkan_enable2
        /// </summary>
        public const string XR_KHR_vulkan_enable2 = "XR_KHR_vulkan_enable2";

        /// <summary>
        /// https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_HTCX_vive_tracker_interaction
        /// </summary>
        public const string XR_HTCX_vive_tracker_interaction = "XR_HTCX_vive_tracker_interaction";

        public static string[] EnabledExtensions = new string[]
        {
            XR_KHR_vulkan_enable,
            XR_KHR_vulkan_enable2,
            XR_HTCX_vive_tracker_interaction,
        };
        protected override XR GenerateAPI() => XR.GetApi();

        public static ExtensionProperties[]? Extensions { get; set; }

        private static void EnsureResult(Result result)
        {
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"OpenXR operation failed with error: {result}");
            }
        }
        private bool IsExtensionSupported(ExtensionProperties[] availableExtensions, string extensionName)
        {
            foreach (var extension in availableExtensions)
                if (extensionName == Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName))
                    return true;
            
            return false;
        }
        public void Initialize()
        {
            var xrInstance = new Instance();
            var xrSystemId = (uint)xrInstance.SelectActivePath(XRPath.Root, XRPath.Vulkan);
            var xrSystem = xrInstance.CreateSystem(new SystemCreateInfo { SystemId = xrSystemId });

            using var enabledExtensions = new UTF8ArrayPtr(EnableExtensions(GetExtensions()));

            var createInfo = new InstanceCreateInfo
            {
                Type = StructureType.InstanceCreateInfo,
                Next = (void*)IntPtr.Zero,
                CreateFlags = 0,
                EnabledApiLayerCount = 0,
                EnabledApiLayerNames = (byte**)IntPtr.Zero,
                ApplicationInfo = new ApplicationInfo
                {
                    //ApplicationName = "VulkanOpenXR",
                    ApplicationVersion = 1,
                    //EngineName = "VulkanEngine",
                    EngineVersion = 1,
                    //ApiVersion = XR.CurrentVersion
                },
                EnabledExtensionCount = (uint)enabledExtensions.Strings.Count,
                EnabledExtensionNames = enabledExtensions.Ptr
            };

            // Create an OpenXR instance
            Instance instance;
            EnsureResult(API.CreateInstance(&createInfo, &instance));

            // Create a system
            ulong systemId;
            var systemGetInfo = new SystemGetInfo { FormFactor = FormFactor.HeadMountedDisplay };
            EnsureResult(API.GetSystem(instance, systemGetInfo, &systemId));

            // Initialize the OpenXR session
            var sessionCreateInfo = new SessionCreateInfo
            {
                SystemId = systemId,
                Type = StructureType.SessionCreateInfo,
                //Next = &vkDevice
            };

            Session session;
            EnsureResult(API.CreateSession(instance, &sessionCreateInfo, &session));

            // Initialize HTC Vive Tracker extension
            var trackerSpaceCreateInfo = new ReferenceSpaceCreateInfo
            {
                Type = (StructureType)Extension.HtcX.TypeSpaceCreateInfoViveTracker,
                Next = new ReferenceSpaceCreateInfo
                {
                    TrackerRole = Extension.HtcX.ViveTrackerRole.GenericTracker,
                    DeviceIndex = 0
                }
            };

            Space space;
            EnsureResult(API.CreateReferenceSpace(session, trackerSpaceCreateInfo, &space));

            // Continue with the rest of your application logic
        }

        private List<string> EnableExtensions(ExtensionProperties[] availableExtensions)
            => EnabledExtensions.Where(x => IsExtensionSupported(availableExtensions, x)).ToList();

        private static ExtensionProperties[] GetExtensions()
        {
            ExtensionProperties[] extensions;
            uint extensionsCount;
            API.EnumerateInstanceExtensionProperties((byte*)null, 0u, &extensionsCount, null);
            extensions = new ExtensionProperties[extensionsCount];
            fixed (ExtensionProperties* extensionProperties = extensions)
                API.EnumerateInstanceExtensionProperties((byte*)null, extensionsCount, &extensionsCount, extensionProperties);
            return extensions;
        }

        public static void Cleanup()
        {

        }

        protected override bool LoadExt<T>(out T? output)
        {

        }

        protected override void InitAPI()
        {

        }

        protected override void CleanUp()
        {

        }

        protected override void DrawFrame(double delta)
        {

        }

        protected override bool LoadExt<T>(out T? output) where T : default
        {
            throw new NotImplementedException();
        }
    }
}
