using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

public unsafe partial class VulkanAPI : BaseAPI
{
    private DescriptorPool descriptorPool;

    private void DestroyDescriptorPool()
        => vk!.DestroyDescriptorPool(device, descriptorPool, null);

    private void CreateDescriptorPool()
    {
        var poolSizes = new DescriptorPoolSize[]
         {
            new DescriptorPoolSize()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)swapChainImages!.Length,
            },
            new DescriptorPoolSize()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)swapChainImages!.Length,
            }
         };

        fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
        fixed (DescriptorPool* descriptorPoolPtr = &descriptorPool)
        {

            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = poolSizesPtr,
                MaxSets = (uint)swapChainImages!.Length,
            };

            if (vk!.CreateDescriptorPool(device, poolInfo, null, descriptorPoolPtr) != Result.Success)
                throw new Exception("failed to create descriptor pool!");
        }
    }
}