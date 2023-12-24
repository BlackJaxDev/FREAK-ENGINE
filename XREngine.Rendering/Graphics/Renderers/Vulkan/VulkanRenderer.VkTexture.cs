using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Format = Silk.NET.Vulkan.Format;
using Image = Silk.NET.Vulkan.Image;

namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{
    public unsafe partial class VulkanRenderer
    {
        public class VkTexture : GraphicTexture<VulkanRenderer>
        {
            public uint width;
            public uint height;
            public Format format = Format.R8G8B8A8Srgb;
            public Image textureImage;
            public DeviceMemory textureImageMemory;
            public ImageView textureImageView;
            public Sampler textureSampler;

            public void CleanUp()
            {
                var device = Renderer.device;

                API.DestroySampler(device, textureSampler, null);
                API.DestroyImageView(device, textureImageView, null);

                API.DestroyImage(device, textureImage, null);
                API.FreeMemory(device, textureImageMemory, null);

            }
            private void CreateTextureImage()
            {
                var device = Renderer.device;
                using Image<Rgba32> img = LoadImage();
                format = Format.R8G8B8A8Srgb;
                width = (uint)img.Width;
                height = (uint)img.Height;

                ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

                Buffer stagingBuffer = default;
                DeviceMemory stagingBufferMemory = default;
                Renderer.CreateBuffer(
                    imageSize,
                    BufferUsageFlags.TransferSrcBit,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                    ref stagingBuffer,
                    ref stagingBufferMemory);

                void* data;
                API.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
                img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
                API.UnmapMemory(device, stagingBufferMemory);

                CreateImage(
                    ImageTiling.Optimal,
                    ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
                    MemoryPropertyFlags.DeviceLocalBit,
                    ref textureImage,
                    ref textureImageMemory);

                TransitionImageLayout(textureImage, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
                CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
                TransitionImageLayout(textureImage, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

                API.DestroyBuffer(device, stagingBuffer, null);
                API.FreeMemory(device, stagingBufferMemory, null);
            }

            private static Image<Rgba32> LoadImage()
            {
                return SixLabors.ImageSharp.Image.Load<Rgba32>(TEXTURE_PATH);
            }

            private void CreateTextureImageView()
                => textureImageView = CreateImageView(textureImage, format, ImageAspectFlags.ColorBit);

            private void CreateTextureSampler()
            {
                SamplerCreateInfo samplerInfo = new()
                {
                    SType = StructureType.SamplerCreateInfo,
                    MagFilter = Filter.Linear,
                    MinFilter = Filter.Linear,
                    AddressModeU = SamplerAddressMode.Repeat,
                    AddressModeV = SamplerAddressMode.Repeat,
                    AddressModeW = SamplerAddressMode.Repeat,
                    AnisotropyEnable = true,
                    MaxAnisotropy = Renderer.physicalDeviceProperties.Limits.MaxSamplerAnisotropy,
                    BorderColor = BorderColor.IntOpaqueBlack,
                    UnnormalizedCoordinates = false,
                    CompareEnable = false,
                    CompareOp = CompareOp.Always,
                    MipmapMode = SamplerMipmapMode.Linear,
                };

                fixed (Sampler* textureSamplerPtr = &textureSampler)
                    if (API.CreateSampler(Renderer.device, samplerInfo, null, textureSamplerPtr) != Result.Success)
                        throw new Exception("Failed to create texture sampler.");
            }

            private ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags)
            {
                ImageViewCreateInfo createInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = image,
                    ViewType = ImageViewType.Type2D,
                    Format = format,
                    //Components =
                    //    {
                    //        R = ComponentSwizzle.Identity,
                    //        G = ComponentSwizzle.Identity,
                    //        B = ComponentSwizzle.Identity,
                    //        A = ComponentSwizzle.Identity,
                    //    },
                    SubresourceRange =
                    {
                        AspectMask = aspectFlags,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                };

                if (API.CreateImageView(Renderer.device, createInfo, null, out ImageView imageView) != Result.Success)
                    throw new Exception("Failed to create image views.");

                return imageView;
            }

            public void CreateImage(
                ImageTiling tiling,
                ImageUsageFlags usage,
                MemoryPropertyFlags properties,
                ref Image image,
                ref DeviceMemory imageMemory)
            {
                var device = Renderer.device;

                ImageCreateInfo imageInfo = new()
                {
                    SType = StructureType.ImageCreateInfo,
                    ImageType = ImageType.Type2D,
                    Extent =
                    {
                        Width = width,
                        Height = height,
                        Depth = 1,
                    },
                    MipLevels = 1,
                    ArrayLayers = 1,
                    Format = format,
                    Tiling = tiling,
                    InitialLayout = ImageLayout.Undefined,
                    Usage = usage,
                    Samples = SampleCountFlags.Count1Bit,
                    SharingMode = SharingMode.Exclusive,
                };

                fixed (Image* imagePtr = &image)
                {
                    if (API.CreateImage(device, imageInfo, null, imagePtr) != Result.Success)
                        throw new Exception("Failed to create image.");
                }

                API.GetImageMemoryRequirements(device, image, out MemoryRequirements memRequirements);

                MemoryAllocateInfo allocInfo = new()
                {
                    SType = StructureType.MemoryAllocateInfo,
                    AllocationSize = memRequirements.Size,
                    MemoryTypeIndex = Renderer.FindMemoryType(memRequirements.MemoryTypeBits, properties),
                };

                fixed (DeviceMemory* imageMemoryPtr = &imageMemory)
                    if (API.AllocateMemory(device, allocInfo, null, imageMemoryPtr) != Result.Success)
                        throw new Exception("Failed to allocate image memory.");

                API.BindImageMemory(device, image, imageMemory, 0);
            }

            private void TransitionImageLayout(Image image, ImageLayout oldLayout, ImageLayout newLayout)
            {
                CommandBuffer commandBuffer = Renderer.BeginSingleTimeCommands();

                ImageMemoryBarrier barrier = new()
                {
                    SType = StructureType.ImageMemoryBarrier,
                    OldLayout = oldLayout,
                    NewLayout = newLayout,
                    SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                    DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                    Image = image,
                    SubresourceRange =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                };

                PipelineStageFlags sourceStage;
                PipelineStageFlags destinationStage;

                if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
                {
                    barrier.SrcAccessMask = 0;
                    barrier.DstAccessMask = AccessFlags.TransferWriteBit;

                    sourceStage = PipelineStageFlags.TopOfPipeBit;
                    destinationStage = PipelineStageFlags.TransferBit;
                }
                else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
                {
                    barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                    barrier.DstAccessMask = AccessFlags.ShaderReadBit;

                    sourceStage = PipelineStageFlags.TransferBit;
                    destinationStage = PipelineStageFlags.FragmentShaderBit;
                }
                else
                    throw new Exception("Unsupported layout transition.");

                API.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, barrier);

                Renderer.EndSingleTimeCommands(commandBuffer);
            }

            private void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
            {
                CommandBuffer commandBuffer = Renderer.BeginSingleTimeCommands();

                BufferImageCopy region = new()
                {
                    BufferOffset = 0,
                    BufferRowLength = 0,
                    BufferImageHeight = 0,
                    ImageSubresource =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
                    ImageOffset = new Offset3D(0, 0, 0),
                    ImageExtent = new Extent3D(width, height, 1),
                };

                API.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, region);

                Renderer.EndSingleTimeCommands(commandBuffer);
            }
        }
    }
}
