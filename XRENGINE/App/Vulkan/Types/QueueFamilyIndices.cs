public unsafe partial class VulkanAPI
{
    struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete()
            => GraphicsFamily.HasValue;
    }
}