using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace XREngine.Rendering.Vulkan;
public unsafe partial class VulkanRenderer
{
    private Device device;
    private Queue graphicsQueue;
    private Queue presentQueue;

    private void DestroyLogicalDevice()
        => Api!.DestroyDevice(device, null);

    private void CreateLogicalDevice()
    {
        var indices = FindQueueFamilies(physicalDevice);

        var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
            queueCreateInfos[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        
        PhysicalDeviceFeatures deviceFeatures = new();

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = (uint)deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions)
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
        }
        else
            createInfo.EnabledLayerCount = 0;
        
        if (Api!.CreateDevice(physicalDevice, in createInfo, null, out device) != Result.Success)
            throw new Exception("Failed to create logical device.");

        Api!.GetDeviceQueue(device, indices.GraphicsFamily!.Value, 0, out graphicsQueue);
        Api!.GetDeviceQueue(device, indices.PresentFamily!.Value, 0, out presentQueue);

        if (EnableValidationLayers)
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }
}