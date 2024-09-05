namespace XREngine.Rendering.Vulkan;
public unsafe partial class VulkanRenderer
{
    struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public readonly bool IsComplete()
            => GraphicsFamily.HasValue;
    }
}