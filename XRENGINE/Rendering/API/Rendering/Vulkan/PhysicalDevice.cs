using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace XREngine.Rendering.Vulkan;
public unsafe partial class VulkanRenderer
{
    private PhysicalDevice physicalDevice;

    private void PickPhysicalDevice()
    {
        uint devicedCount = 0;
        Api!.EnumeratePhysicalDevices(instance, ref devicedCount, null);

        if (devicedCount == 0)
            throw new Exception("failed to find GPUs with Vulkan support!");
        
        var devices = new PhysicalDevice[devicedCount];
        fixed (PhysicalDevice* devicesPtr = devices)
        {
            Api!.EnumeratePhysicalDevices(instance, ref devicedCount, devicesPtr);
        }

        foreach (var device in devices)
            if (IsDeviceSuitable(device))
            {
                physicalDevice = device;
                break;
            }
        
        if (physicalDevice.Handle == 0)
            throw new Exception("failed to find a suitable GPU!");
    }

    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indices = FindQueueFamilies(device);

        bool extensionsSupported = CheckDeviceExtensionsSupport(device);

        bool swapChainAdequate = false;
        if (extensionsSupported)
        {
            var swapChainSupport = QuerySwapChainSupport(device);
            swapChainAdequate = 
                swapChainSupport.Formats.Length != 0 &&
                swapChainSupport.PresentModes.Length != 0;
        }

        return indices.IsComplete() && extensionsSupported && swapChainAdequate;
    }

    private bool CheckDeviceExtensionsSupport(PhysicalDevice device)
    {
        uint extentionsCount = 0;
        Api!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, null);

        var availableExtensions = new ExtensionProperties[extentionsCount];
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
        {
            Api!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
        }

        var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

        return deviceExtensions.All(availableExtensionNames.Contains);

    }

    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilityCount = 0;
        Api!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            Api!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
        }


        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                indices.GraphicsFamily = i;
            
            khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var presentSupport);

            if (presentSupport)
                indices.PresentFamily = i;
            
            if (indices.IsComplete())
                break;
            
            i++;
        }

        return indices;
    }

    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags memProps)
    {
        Api!.GetPhysicalDeviceMemoryProperties(physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & memProps) == memProps)
                return (uint)i;

        throw new Exception("Failed to find suitable memory type.");
    }
}