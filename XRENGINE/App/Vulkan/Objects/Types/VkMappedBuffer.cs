using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe partial class VulkanAPI
{
    public class VkMappedBuffer : VkObjectBase
    {
        private Buffer buffer = default;
        private DeviceMemory memory = default;

        public Buffer Buffer { get => buffer; set => buffer = value; }
        public DeviceMemory Memory { get => memory; set => memory = value; }
        public ulong Size { get; set; }
        public BufferUsageFlags Usage { get; set; } = BufferUsageFlags.TransferSrcBit;
        public MemoryPropertyFlags Properties { get; set; } = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;

        public VkMappedBuffer(VulkanAPI api) : base(api) { }

        public void MapImage(Image<Rgba32> image)
        {
            Size = (ulong)(image.Width * image.Height * image.PixelType.BitsPerPixel / 8);

            void* data;
            api.vk!.MapMemory(api.device, Memory, 0, Size, 0, &data);
            var span = new Span<byte>(data, (int)Size);
            image.CopyPixelDataTo(span);
            api.vk!.UnmapMemory(api.device, Memory);
        }

        public override void Create()
        {
            Allocate();

        }

        private void Allocate()
        {
            api.CreateBuffer(Size, Usage, Properties, ref buffer, ref memory);
        }

        public override void Destroy()
        {
            api.vk!.DestroyBuffer(api.device, buffer, null);
            api.vk!.FreeMemory(api.device, memory, null);
        }
    }
}