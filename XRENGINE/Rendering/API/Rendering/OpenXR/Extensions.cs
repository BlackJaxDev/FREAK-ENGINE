using Silk.NET.Core.Native;
using Silk.NET.OpenXR.Extensions.KHR;
using Silk.NET.Vulkan.Extensions.EXT;
using System.Numerics;
using XREngine.Rendering;

public unsafe partial class OpenXRAPI
{
    public override bool CalcDotLuminance(XRTexture2D texture, Vector3 luminance, out float dotLuminance, bool genMipmapsNow)
    {
        throw new NotImplementedException();
    }
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
        switch (renderer)
        {
            case Renderer.Vulkan:
                {
                    var glfwExtensions = Window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
                    extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);
                    if (EnableValidationLayers)
                        extensions = [.. extensions, ExtDebugUtils.ExtensionName];
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