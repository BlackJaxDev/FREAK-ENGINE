using Silk.NET.Core.Native;
using Silk.NET.OpenXR.Extensions.KHR;
using Silk.NET.Vulkan.Extensions.EXT;

public unsafe partial class OpenXRAPI
{
    //private readonly string[] deviceExtensions =
    //[
    //    HtcxViveTrackerInteraction.ExtensionName,
    //    //HtcFacialTracking.ExtensionName,
    //    //HtcFoveation.ExtensionName,
    //    //HtcPassthrough.ExtensionName,
    //    KhrVulkanEnable.ExtensionName,
    //    KhrVulkanEnable2.ExtensionName,
    //    //KhrAndroidSurfaceSwapchain.ExtensionName,
    //];
    public enum Renderer
    {
        OpenGL,
        Vulkan,
    }
    private string[] GetRequiredExtensions(Renderer renderer, params string[] otherExtensions)
    {
        string[] extensions = [];
        if (EnableValidationLayers)
            extensions = [.. extensions, ExtDebugUtils.ExtensionName];
        switch (renderer)
        {
            case Renderer.Vulkan:
                {
                    extensions = [.. extensions, KhrVulkanEnable.ExtensionName, KhrVulkanEnable2.ExtensionName];
                }
                break;
            case Renderer.OpenGL:
                {
                    extensions = [.. extensions, KhrOpenglEnable.ExtensionName];
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(renderer), renderer, null);
        }

        if (otherExtensions.Length > 0)
            extensions = [.. extensions, .. otherExtensions];

        return extensions;
    }
}