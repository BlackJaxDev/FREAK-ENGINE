using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe partial class VulkanAPI : BaseAPI
{
    private CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1,
        };

        vk!.AllocateCommandBuffers(device, allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        vk!.BeginCommandBuffer(commandBuffer, beginInfo);

        return commandBuffer;
    }

    private void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        vk!.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        vk!.QueueSubmit(graphicsQueue, 1, submitInfo, default);
        vk!.QueueWaitIdle(graphicsQueue);

        vk!.FreeCommandBuffers(device, commandPool, 1, commandBuffer);
    }

    private void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* bufferPtr = &buffer)
        {
            if (vk!.CreateBuffer(device, bufferInfo, null, bufferPtr) != Result.Success)
                throw new Exception("failed to create vertex buffer!");
        }

        MemoryRequirements memRequirements = new();
        vk!.GetBufferMemoryRequirements(device, buffer, out memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
        {
            if (vk!.AllocateMemory(device, allocateInfo, null, bufferMemoryPtr) != Result.Success)
                throw new Exception("failed to allocate vertex buffer memory!");
        }

        vk!.BindBufferMemory(device, buffer, bufferMemory, 0);
    }

    //private void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    //{
    //    CommandBufferAllocateInfo allocateInfo = new()
    //    {
    //        SType = StructureType.CommandBufferAllocateInfo,
    //        Level = CommandBufferLevel.Primary,
    //        CommandPool = commandPool,
    //        CommandBufferCount = 1,
    //    };

    //    vk!.AllocateCommandBuffers(device, allocateInfo, out CommandBuffer commandBuffer);

    //    CommandBufferBeginInfo beginInfo = new()
    //    {
    //        SType = StructureType.CommandBufferBeginInfo,
    //        Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
    //    };

    //    vk!.BeginCommandBuffer(commandBuffer, beginInfo);

    //    BufferCopy copyRegion = new()
    //    {
    //        Size = size,
    //    };

    //    vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);
    //    vk!.EndCommandBuffer(commandBuffer);

    //    SubmitInfo submitInfo = new()
    //    {
    //        SType = StructureType.SubmitInfo,
    //        CommandBufferCount = 1,
    //        PCommandBuffers = &commandBuffer,
    //    };

    //    vk!.QueueSubmit(graphicsQueue, 1, submitInfo, default);
    //    vk!.QueueWaitIdle(graphicsQueue);

    //    vk!.FreeCommandBuffers(device, commandPool, 1, commandBuffer);
    //}

    private void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            Size = size,
        };

        vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

        EndSingleTimeCommands(commandBuffer);
    }

    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        vk!.GetPhysicalDeviceMemoryProperties(physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                return (uint)i;

        throw new Exception("failed to find suitable memory type!");
    }

}