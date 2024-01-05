using Silk.NET.Core.Native;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;

public unsafe partial class VulkanAPI
{
    private readonly string[] deviceExtensions = new[]
    {
        KhrSwapchain.ExtensionName
    };

    private string[] GetRequiredExtensions()
    {
        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        if (EnableValidationLayers)
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();

        return extensions;
    }
}