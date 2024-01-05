using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe partial class VulkanAPI : BaseAPI
{
    private Buffer vertexBuffer;
    private Buffer indexBuffer;
    private DeviceMemory vertexBufferMemory;
    private DeviceMemory indexBufferMemory;

    private Vertex[]? vertices;
    private uint[]? indices;

    private VkTexture? _modelTexture;

    private void DestroyModelBuffers()
    {
        DestroyIndexBuffer();
        DestroyVertexBuffer();
    }

    private void DestroyIndexBuffer()
    {
        vk!.DestroyBuffer(device, indexBuffer, null);
        vk!.FreeMemory(device, indexBufferMemory, null);
    }

    private void DestroyVertexBuffer()
    {
        vk!.DestroyBuffer(device, vertexBuffer, null);
        vk!.FreeMemory(device, vertexBufferMemory, null);
    }

    private void CreateModelBuffers()
    {
        _modelTexture = new VkTexture(this, "Assets/Textures/texture.jpg");
        _modelTexture.CreateSampler = true;
        _modelTexture.Create();

        CreateVertexBuffer();
        CreateIndexBuffer();
    }

    private void CreateVertexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * vertices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        vertices.AsSpan().CopyTo(new Span<Vertex>(data, vertices.Length));
        vk!.UnmapMemory(device, stagingBufferMemory);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref vertexBuffer, ref vertexBufferMemory);

        CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

        vk!.DestroyBuffer(device, stagingBuffer, null);
        vk!.FreeMemory(device, stagingBufferMemory, null);
    }

    private void CreateIndexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<ushort>() * indices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        indices.AsSpan().CopyTo(new Span<ushort>(data, indices.Length));
        vk!.UnmapMemory(device, stagingBufferMemory);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref indexBuffer, ref indexBufferMemory);

        CopyBuffer(stagingBuffer, indexBuffer, bufferSize);

        vk!.DestroyBuffer(device, stagingBuffer, null);
        vk!.FreeMemory(device, stagingBufferMemory, null);
    }
}