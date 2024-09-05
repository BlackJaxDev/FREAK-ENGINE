namespace XREngine.Rendering.Vulkan;
public unsafe partial class VulkanRenderer
{
    public abstract class VkTexture<T>(VulkanRenderer api, T data) : VkObject<T>(api, data) where T : XRTexture
    {
        public override VkObjectType Type => VkObjectType.Image;
    }
}