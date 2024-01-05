using Silk.NET.Vulkan;

public unsafe partial class VulkanAPI : BaseAPI
{
    private CommandPool commandPool;

    private void DestroyCommandPool()
        => vk!.DestroyCommandPool(device, commandPool, null);

    private void CreateCommandPool()
    {
        var queueFamiliyIndicies = FindQueueFamilies(physicalDevice);

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
        };

        if (vk!.CreateCommandPool(device, poolInfo, null, out commandPool) != Result.Success)
            throw new Exception("failed to create command pool!");
    }
}