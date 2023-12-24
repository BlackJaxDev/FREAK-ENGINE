using Silk.NET.Vulkan;

namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{
    struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }
}
