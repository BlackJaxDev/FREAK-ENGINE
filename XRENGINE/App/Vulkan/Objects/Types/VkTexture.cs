using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

public unsafe partial class VulkanAPI
{
    public class VkTexture : VkObjectBase
    {
        public VkTexture(VulkanAPI api) : base(api) { }
        public VkTexture(VulkanAPI api, string path) : base(api)
            => LoadFromImage(path);
        public VkTexture(VulkanAPI api, uint width, uint height, VkMappedBuffer buffer) : base(api)
        {
            Width = width;
            Height = height;
            LoadFromBuffer(buffer);
        }
        public VkTexture(
            VulkanAPI api,
            uint width,
            uint height,
            Format format) : this(api)
        {
            Width = width;
            Height = height;
            Format = format;
        }

        private Image image;
        private DeviceMemory memory;
        private ImageView view;
        private Sampler sampler;

        internal ImageView View => view;
        internal Sampler Sampler => sampler;

        public bool CreateSampler { get; set; } = true;

        public uint Width { get; set; } = 1;
        public uint Height { get; set; } = 1;
        public uint Depth { get; set; } = 1;
        public uint MipLevels { get; set; } = 1;
        public uint TexArrayCount { get; set; } = 1;
        public Format Format { get; set; } = Format.R8G8B8A8Srgb;
        public ImageTiling Tiling { get; set; } = ImageTiling.Optimal;
        public ImageUsageFlags Usage { get; set; } = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit;
        public MemoryPropertyFlags Properties { get; set; } = MemoryPropertyFlags.DeviceLocalBit;
        public ImageType GetImageType()
            => ViewType switch
            {
                ImageViewType.Type1D or ImageViewType.Type1DArray => ImageType.Type1D,
                ImageViewType.Type3D or ImageViewType.TypeCube or ImageViewType.TypeCubeArray => ImageType.Type3D,
                _ => ImageType.Type2D,
            };

        //View
        public ImageAspectFlags AspectFlags { get; set; } = ImageAspectFlags.ColorBit;
        public ImageViewType ViewType { get; set; } = ImageViewType.Type2D;
        
        //Sampler
        public SamplerMipmapMode MipmapMode { get; set; } = SamplerMipmapMode.Linear;
        public SamplerAddressMode UWrap { get; set; } = SamplerAddressMode.Repeat;
        public SamplerAddressMode VWrap { get; set; } = SamplerAddressMode.Repeat;
        public SamplerAddressMode WWrap { get; set; } = SamplerAddressMode.Repeat;
        public Filter MinFilter { get; set; } = Filter.Linear;
        public Filter MagFilter { get; set; } = Filter.Linear;
        public bool UseAniso { get; set; } = true;

        public override void Destroy()
        {
            api.vk!.DestroySampler(api.device, sampler, null);
            api.vk!.DestroyImageView(api.device, view, null);

            api.vk!.DestroyImage(api.device, image, null);
            api.vk!.FreeMemory(api.device, memory, null);
        }

        public void LoadFromImage(string path)
        {
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
            Width = (uint)img.Width;
            Height = (uint)img.Height;

            using (VkMappedBuffer staging = new VkMappedBuffer(api))
            {
                staging.MapImage(img);
                LoadFromBuffer(staging);
            }
        }

        private void LoadFromBuffer(VkMappedBuffer buffer)
        {
            CreateImage();
            TransitionImageLayout(ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
            CopyBufferToImage(buffer.Buffer);
            TransitionImageLayout(ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
        }

        public override void Create()
        {
            CreateView();

            if (CreateSampler)
                GenSampler();
        }

        public void GenSampler()
        {
            api.vk!.GetPhysicalDeviceProperties(api.physicalDevice, out PhysicalDeviceProperties properties);

            SamplerCreateInfo samplerInfo = new()
            {
                SType = StructureType.SamplerCreateInfo,
                MagFilter = MagFilter,
                MinFilter = MinFilter,
                AddressModeU = UWrap,
                AddressModeV = VWrap,
                AddressModeW = WWrap,
                AnisotropyEnable = UseAniso,
                MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false,
                CompareEnable = false,
                CompareOp = CompareOp.Always,
                MipmapMode = MipmapMode,
            };

            fixed (Sampler* textureSamplerPtr = &sampler)
            {
                if (api.vk!.CreateSampler(api.device, samplerInfo, null, textureSamplerPtr) != Result.Success)
                    throw new Exception("failed to create texture sampler!");
            }
        }

        private void CreateView()
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = image,
                ViewType = ViewType,
                Format = Format,
                //Components =
                //    {
                //        R = ComponentSwizzle.Identity,
                //        G = ComponentSwizzle.Identity,
                //        B = ComponentSwizzle.Identity,
                //        A = ComponentSwizzle.Identity,
                //    },
                SubresourceRange =
                {
                    AspectMask = AspectFlags,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }

            };

            if (api.vk!.CreateImageView(api.device, createInfo, null, out view) != Result.Success)
                throw new Exception("failed to create image views!");
        }

        private void CreateImage()
        {
            ImageCreateInfo imageInfo = new()
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = GetImageType(),
                Extent =
                {
                    Width = Width,
                    Height = Height,
                    Depth = Depth,
                },
                MipLevels = MipLevels,
                ArrayLayers = TexArrayCount,
                Format = Format,
                Tiling = Tiling,
                InitialLayout = ImageLayout.Undefined,
                Usage = Usage,
                Samples = SampleCountFlags.Count1Bit,
                SharingMode = SharingMode.Exclusive,
            };

            fixed (Image* imagePtr = &image)
            {
                if (api.vk!.CreateImage(api.device, imageInfo, null, imagePtr) != Result.Success)
                    throw new Exception("failed to create image!");
            }

            api.vk!.GetImageMemoryRequirements(api.device, image, out MemoryRequirements memRequirements);

            MemoryAllocateInfo allocInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = api.FindMemoryType(memRequirements.MemoryTypeBits, Properties),
            };

            fixed (DeviceMemory* imageMemoryPtr = &memory)
            {
                if (api.vk!.AllocateMemory(api.device, allocInfo, null, imageMemoryPtr) != Result.Success)
                    throw new Exception("failed to allocate image memory!");
            }

            api.vk!.BindImageMemory(api.device, image, memory, 0);
        }

        private void TransitionImageLayout(ImageLayout oldLayout, ImageLayout newLayout)
        {
            CommandBuffer commandBuffer = api.BeginSingleTimeCommands();

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
                    AspectMask = AspectFlags,
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
            {
                throw new Exception("unsupported layout transition!");
            }

            api.vk!.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, barrier);

            api.EndSingleTimeCommands(commandBuffer);

        }

        private void CopyBufferToImage(Buffer buffer)
        {
            CommandBuffer commandBuffer = api.BeginSingleTimeCommands();

            BufferImageCopy region = new()
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource =
            {
                AspectMask = AspectFlags,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
                ImageOffset = new Offset3D(0, 0, 0),
                ImageExtent = new Extent3D(Width, Height, Depth),

            };

            api.vk!.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, region);

            api.EndSingleTimeCommands(commandBuffer);
        }

        public DescriptorImageInfo CreateImageInfo()
        {
            return new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = view,
                Sampler = sampler,
            };
        }
    }
}