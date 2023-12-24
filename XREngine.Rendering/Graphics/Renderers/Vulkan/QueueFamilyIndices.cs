namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{
    struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete() => GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
}
