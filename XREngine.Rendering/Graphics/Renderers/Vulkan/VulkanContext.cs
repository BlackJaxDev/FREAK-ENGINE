using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Runtime.InteropServices;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{
    struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete()
        {
            return GraphicsFamily.HasValue && PresentFamily.HasValue;
        }
    }
    public unsafe class VulkanContext
    {
        private Vk vk;

        // Device, swapchain, and other resources
        public Device Device { get; private set; }
        public PhysicalDevice PhysicalDevice { get; private set; }
        public SwapchainKHR Swapchain { get; private set; }
        public Framebuffer SwapchainFramebuffer { get; private set; }

        DescriptorSetLayout DescriptorSetLayout;
        PhysicalDeviceMemoryProperties PhysicalDeviceMemoryProperties;
        RenderPass RenderPass;
        DescriptorPool DescriptorPool;
        Extent2D SwapchainExtent;
        SurfaceKHR Surface;
        Format SwapchainFormat;
        List<ImageView> SwapchainImageViews;
        ImageView SwapchainImageView;
        Instance Instance;
        private KhrSurface? khrSurface;

        // Fullscreen quad resources
        public Buffer FullscreenQuadVertexBuffer { get; private set; }
        public Buffer FullscreenQuadIndexBuffer { get; private set; }
        public bool EnableValidationLayers { get; private set; }

        public VulkanContext()
        {
            // Initialize device, swapchain, and other resources
            InitializeResources();
        }

        private void InitializeResources()
        {
            // Initialize Vulkan resources like instance, device, swapchain, etc.
            vk = Vk.GetApi();

            // Create a framebuffer for the swapchain
            SwapchainFramebuffer = CreateFramebuffer();

            CreateSwapchain();
            CreateRenderPass();
            CreateDescriptorPool();
            CreateDescriptorSetLayout();

            // Create fullscreen quad vertex and index buffers
            CreateFullscreenQuadBuffers();
        }
        private void CreateInstance()
        {
            ApplicationInfo appInfo = new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = "Your Application Name",
                ApplicationVersion = new Version32(1, 0, 0),
                PEngineName = "No Engine",
                EngineVersion = new Version32(1, 0, 0),
                ApiVersion = Vk.Version11
            };

            InstanceCreateInfo createInfo = new()
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            string[] instanceExtensions = { "VK_KHR_surface", "VK_KHR_win32_surface" }; // Adjust based on your platform
            string[] validationLayers = { "VK_LAYER_KHRONOS_validation" };

            // Enable instance extensions
            createInfo.EnabledExtensionCount = (uint)instanceExtensions.Length;
            createInfo.PpEnabledExtensionNames = instanceExtensions;

            // Enable validation layers (optional)
            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                createInfo.PpEnabledLayerNames = validationLayers;
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
            }

            // Check for required extensions
            uint availableExtensionsCount = 0;
            vk.EnumerateInstanceExtensionProperties((byte*)null, &availableExtensionsCount, (ExtensionProperties*)IntPtr.Zero);
            ExtensionProperties[] availableExtensions = new ExtensionProperties[availableExtensionsCount];
            Vk.EnumerateInstanceExtensionProperties((byte*)null, &availableExtensionsCount, availableExtensions);

            foreach (string requiredExtension in instanceExtensions)
            {
                bool found = false;
                foreach (var extension in availableExtensions)
                {
                    if (requiredExtension == extension.ExtensionName.ToString())
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    throw new InvalidOperationException($"Failed to find required instance extension: {requiredExtension}");
                }
            }

            Result result = vk.CreateInstance(&createInfo, null, out Instance);
            if (result != Result.Success)
                throw new InvalidOperationException("Failed to create instance.");
        }
        private void CreateSwapchain()
        {
            // Query surface capabilities and find optimal surface format, present mode, and extent
            vk.GetPhysicalDeviceSurfaceCapabilitiesKHR(PhysicalDevice, Surface, out SurfaceCapabilitiesKHR surfaceCapabilities);

            uint formatCount;
            vk.GetPhysicalDeviceSurfaceFormatsKHR(PhysicalDevice, Surface, &formatCount, null);
            SurfaceFormatKHR[] surfaceFormats = new SurfaceFormatKHR[formatCount];
            vk.GetPhysicalDeviceSurfaceFormatsKHR(PhysicalDevice, Surface, &formatCount, surfaceFormats);

            uint presentModeCount;
            vk.GetPhysicalDeviceSurfacePresentModesKHR(PhysicalDevice, Surface, &presentModeCount, null);
            PresentModeKHR[] presentModes = new PresentModeKHR[presentModeCount];
            vk.GetPhysicalDeviceSurfacePresentModesKHR(PhysicalDevice, Surface, &presentModeCount, presentModes);

            // Choose appropriate swapchain settings based on surface properties
            SurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(surfaceFormats);
            PresentModeKHR presentMode = ChooseSwapPresentMode(presentModes);
            Extent2D extent = ChooseSwapExtent(surfaceCapabilities);

            // Create swapchain
            SwapchainCreateInfoKHR swapchainCreateInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = Surface,
                MinImageCount = surfaceCapabilities.MinImageCount + 1,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageSharingMode = SharingMode.Exclusive,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
                PreTransform = surfaceCapabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
                OldSwapchain = new SwapchainKHR(),
                QueueFamilyIndexCount = 0,
                PQueueFamilyIndices = null,
            };

            Result result = vk.CreateSwapchainKHR(Device, &swapchainCreateInfo, null, out Swapchain);
            if (result != Result.Success)
                throw new InvalidOperationException("Failed to create swapchain.");
            
            // Store swapchain extent
            SwapchainExtent = extent;

            // Create image views for swapchain images
            uint imageCount;
            vk.GetSwapchainImagesKHR(Device, Swapchain, &imageCount, null);
            Image[] swapchainImages = new Image[imageCount];
            vk.GetSwapchainImagesKHR(Device, Swapchain, &imageCount, swapchainImages);

            SwapchainImageViews = new List<ImageView>((int)imageCount);

            foreach (var image in swapchainImages)
            {
                ImageViewCreateInfo viewCreateInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = image,
                    ViewType = ImageViewType.Type2D,
                    Format = surfaceFormat.Format,
                    Components = new ComponentMapping
                    {
                        R = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        B = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity
                    },
                    SubresourceRange = new ImageSubresourceRange
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    }
                };

                result = vk.CreateImageView(Device, &viewCreateInfo, null, out ImageView imageView);
                if (result != Result.Success)
                    throw new InvalidOperationException("Failed to create image view.");
                
                SwapchainImageViews.Add(imageView);
            }
        }
        private void CreateDevice()
        {
            // Choose a physical device
            uint deviceCount = 0;
            Instance.EnumeratePhysicalDevices(ref deviceCount, null);
            if (deviceCount == 0)
            {
                throw new InvalidOperationException("Failed to find GPUs with Vulkan support.");
            }

            PhysicalDevice[] devices = new PhysicalDevice[deviceCount];
            Instance.EnumeratePhysicalDevices(ref deviceCount, devices);

            foreach (var device in devices)
            {
                if (IsDeviceSuitable(device))
                {
                    PhysicalDevice = device;
                    break;
                }
            }

            if (PhysicalDevice.Handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to find a suitable GPU.");
            }

            // Specify required device extensions
            string[] deviceExtensions = { "VK_KHR_swapchain" };

            // Create a logical device
            float[] queuePriority = { 1.0f };
            QueueFamilyIndices indices = FindQueueFamilies(PhysicalDevice);

            DeviceQueueCreateInfo queueCreateInfo = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = indices.GraphicsFamily.Value,
                QueueCount = 1,
                PQueuePriorities = queuePriority
            };

            DeviceCreateInfo createInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = 1,
                PQueueCreateInfos = &queueCreateInfo,
                PEnabledFeatures = null,
                EnabledExtensionCount = (uint)deviceExtensions.Length,
                PpEnabledExtensionNames = deviceExtensions
            };

            Instance.CreateDevice(PhysicalDevice, createInfo, null, out Device);
        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice physicalDevice)
        {

        }

        private bool IsDeviceSuitable(PhysicalDevice device)
        {
            // Check for specific features, extensions, and properties required by your application
            // For this example, we just check for graphics support and swapchain support

            QueueFamilyIndices indices = FindQueueFamilies(device);

            bool extensionsSupported = CheckDeviceExtensionSupport(device);

            return indices.GraphicsFamily.HasValue && extensionsSupported;
        }

        private bool CheckDeviceExtensionSupport(PhysicalDevice device)
        {
            uint extensionCount = 0;
            Instance.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, null);

            ExtensionProperties[] availableExtensions = new ExtensionProperties[extensionCount];
            Instance.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, availableExtensions);

            string[] requiredExtensions = { "VK_KHR_swapchain" };

            foreach (string requiredExtension in requiredExtensions)
            {
                bool found = false;

                foreach (var extension in availableExtensions)
                {
                    if (requiredExtension == extension.ExtensionName.ToString())
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR surfaceCapabilities)
        {

        }

        private PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] presentModes)
        {

        }

        private SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] surfaceFormats)
        {

        }

        private void CreateRenderPass()
        {
            AttachmentDescription colorAttachment = new()
            {
                Format = SwapchainFormat, // Assuming SwapchainFormat is available in the context
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr
            };

            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal
            };

            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef
            };

            RenderPassCreateInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                PAttachments = &colorAttachment,
                SubpassCount = 1,
                PSubpasses = &subpass
            };

            Result result = vk.CreateRenderPass(Device, &renderPassInfo, null, out RenderPass);
            if (result != Result.Success)
                throw new InvalidOperationException("Failed to create render pass.");
        }
        private void CreateFullscreenQuadBuffers()
        {
            // Define fullscreen quad vertex positions and texture coordinates
            float[] vertices = {
                -1.0f, -1.0f, 0.0f, 0.0f,
                1.0f, -1.0f, 1.0f, 0.0f,
                1.0f, 1.0f, 1.0f, 1.0f,
                -1.0f, 1.0f, 0.0f, 1.0f
            };
            ushort[] indices = {
                0, 1, 2,
                2, 3, 0
            };

            // Create and fill the vertex buffer
            // Assuming you have a utility function to create buffers
            FullscreenQuadVertexBuffer = CreateBuffer(vertices, BufferUsageFlags.VertexBufferBit);

            // Create and fill the index buffer
            FullscreenQuadIndexBuffer = CreateBuffer(indices, BufferUsageFlags.IndexBufferBit);
        }

        public Framebuffer CreateFramebuffer()
        {
            // Create a framebuffer for the swapchain
            // This implementation assumes that you've set up a render pass for post-processing

            ImageView[] attachments = new ImageView[1];
            attachments[0] = SwapchainImageView; // Assuming SwapchainImageView is available in the context

            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = RenderPass, // Assuming RenderPass is available in the context
                AttachmentCount = 1,
                PAttachments = attachments,
                Width = SwapchainExtent.Width, // Assuming SwapchainExtent is available in the context
                Height = SwapchainExtent.Height,
                Layers = 1
            };

            Result result = vk.CreateFramebuffer(Device, &framebufferInfo, null, out Framebuffer framebuffer);
            if (result != Result.Success)
                throw new InvalidOperationException("Failed to create framebuffer.");
            
            return framebuffer;
        }
        private void CreateDescriptorPool()
        {
            DescriptorPoolSize poolSize = new()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)SwapchainImageViews.Count // Assuming SwapchainImageViews is available in the context
            };

            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = 1,
                PPoolSizes = &poolSize,
                MaxSets = (uint)SwapchainImageViews.Count
            };

            Result result = vk.CreateDescriptorPool(Device, &poolInfo, null, out DescriptorPool);
            if (result != Result.Success)
                throw new InvalidOperationException("Failed to create descriptor pool.");
        }
        public DescriptorSet GetFramebufferDescriptorSet(Framebuffer framebuffer)
        {
            // Return a descriptor set for the given framebuffer
            // This implementation depends on how you set up descriptor sets and layouts
            // In this example, we assume that you have a descriptor set layout and pool available in the context

            DescriptorSetAllocateInfo allocInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = DescriptorPool, // Assuming DescriptorPool is available in the context
                DescriptorSetCount = 1,
                PSetLayouts = new DescriptorSetLayout[] { DescriptorSetLayout } // Assuming DescriptorSetLayout is available in the context
            };

            Result result = vk.AllocateDescriptorSets(Device, &allocInfo, out DescriptorSet descriptorSet);
            if (result != Result.Success)
                throw new InvalidOperationException("Failed to allocate descriptor set.");
            
            // Update the descriptor set with the framebuffer's texture
            // Assuming the framebuffer's texture is available in the context as a VkImageView
            DescriptorImageInfo imageInfo = new()
            {
                ImageView = framebuffer.TextureImageView,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal
            };

            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSet,
                DstBinding = 0,
                DstArrayElement = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                PImageInfo = &imageInfo
            };

            vk.UpdateDescriptorSets(Device, 1, &descriptorWrite, 0, null);

            return descriptorSet;
        }

        private Buffer CreateBuffer<T>(T[] data, BufferUsageFlags usage) where T : struct
        {
            ulong bufferSize = (ulong)(Marshal.SizeOf<T>() * data.Length);

            // Create the buffer
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = bufferSize,
                Usage = usage,
                SharingMode = SharingMode.Exclusive
            };

            Result result = vk.CreateBuffer(Device, &bufferInfo, null, out Buffer buffer);
            if (result != Result.Success)
            {
                throw new InvalidOperationException("Failed to create buffer.");
            }

            // Get memory requirements for the buffer
            vk.GetBufferMemoryRequirements(Device, buffer, out MemoryRequirements memRequirements);

            // Allocate memory for the buffer
            MemoryAllocateInfo allocInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
            };

            result = vk.AllocateMemory(Device, &allocInfo, null, out DeviceMemory bufferMemory);
            if (result != Result.Success)
                throw new InvalidOperationException("Failed to allocate buffer memory.");
            
            // Bind the buffer memory
            vk.BindBufferMemory(Device, buffer, bufferMemory, 0);

            // Fill the buffer with data
            void* dataPtr;
            vk.MapMemory(Device, bufferMemory, 0, bufferSize, 0, &dataPtr);
            Marshal.Copy(data, 0, (nint)dataPtr, data.Length);
            vk.UnmapMemory(Device, bufferMemory);

            return buffer;
        }
        private void CreateDescriptorSetLayout()
        {
            DescriptorSetLayoutBinding samplerLayoutBinding = new()
            {
                Binding = 0,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
                PImmutableSamplers = null
            };

            DescriptorSetLayoutCreateInfo layoutInfo = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = 1,
                PBindings = &samplerLayoutBinding
            };

            Result result = vk.CreateDescriptorSetLayout(Device, &layoutInfo, null, out DescriptorSetLayout);
            if (result != Result.Success)
                throw new InvalidOperationException("Failed to create descriptor set layout.");
        }
        private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
        {
            // Assuming you have PhysicalDeviceMemoryProperties available in the context
            PhysicalDeviceMemoryProperties memProperties = PhysicalDeviceMemoryProperties;
            for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
                if ((typeFilter & (1 << (int)i)) != 0 && (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
                    return i;
            
            throw new InvalidOperationException("Failed to find suitable memory type.");
        }
        private void CleanupSwapchain()
        {
            foreach (var imageView in SwapchainImageViews)
            {
                vk.DestroyImageView(Device, imageView, null);
            }
            vk.DestroySwapchainKHR(Device, Swapchain, null);
        }

        private void CleanupRenderPass()
        {
            vk.DestroyRenderPass(Device, RenderPass, null);
        }

        private void CleanupDescriptorPool()
        {
            vk.DestroyDescriptorPool(Device, DescriptorPool, null);
        }

        private void CleanupDescriptorSetLayout()
        {
            vk.DestroyDescriptorSetLayout(Device, DescriptorSetLayout, null);
        }
    }
}
