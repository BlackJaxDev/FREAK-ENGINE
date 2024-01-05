using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

public unsafe partial class VulkanAPI
{
    private KhrSurface? khrSurface;
    private SurfaceKHR surface;

    private void DestroySurface()
        => khrSurface!.DestroySurface(instance, surface, null);

    private void CreateSurface()
    {
        if (!vk!.TryGetInstanceExtension<KhrSurface>(instance, out khrSurface))
            throw new NotSupportedException("KHR_surface extension not found.");
        
        surface = window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
    }
}