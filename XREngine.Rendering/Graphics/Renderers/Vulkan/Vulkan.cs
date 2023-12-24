using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Buffer = Silk.NET.Vulkan.Buffer;
using File = System.IO.File;
using Format = Silk.NET.Vulkan.Format;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{
    public unsafe partial class VulkanRenderer : AbstractRenderer<Vk>
    {
        const int WIDTH = 800;
        const int HEIGHT = 600;

        const string MODEL_PATH = @"Assets\viking_room.obj";
        const string TEXTURE_PATH = @"Assets\viking_room.png";

        const int MAX_FRAMES_IN_FLIGHT = 2;

        bool EnableValidationLayers = true;

        private readonly string[] validationLayers = new[]
        {
            "VK_LAYER_KHRONOS_validation"
        };

        private readonly string[] deviceExtensions = new[]
        {
            KhrSwapchain.ExtensionName
        };

        public struct DataBuffer
        {
            private Buffer buffer;
            private DeviceMemory bufferMemory;
        }

        private Instance instance;

        private ExtDebugUtils? debugUtils;
        private DebugUtilsMessengerEXT debugMessenger;
        private KhrSurface? khrSurface;
        private SurfaceKHR surface;

        public PhysicalDevice physicalDevice;
        public PhysicalDeviceProperties physicalDeviceProperties;
        public Device device;

        private Queue graphicsQueue;
        private Queue presentQueue;

        private KhrSwapchain? khrSwapChain;
        public SwapchainKHR swapChain;
        private VkTexture[]? swapChainImages;
        public Extent2D swapChainExtent;
        private ImageView[]? swapChainImageViews;
        private Framebuffer[]? swapChainFramebuffers;

        private RenderPass renderPass;
        private DescriptorSetLayout descriptorSetLayout;

        private CommandPool commandPool;

        private Image depthImage;
        private DeviceMemory depthImageMemory;
        private ImageView depthImageView;

        private Buffer[]? uniformBuffers;
        private DeviceMemory[]? uniformBuffersMemory;

        private DescriptorPool descriptorPool;
        private DescriptorSet[]? descriptorSets;

        private CommandBuffer[]? commandBuffers;

        private Semaphore[]? imageAvailableSemaphores;
        private Semaphore[]? renderFinishedSemaphores;
        private Fence[]? inFlightFences;
        private Fence[]? imagesInFlight;
        private int currentFrame = 0;

        protected override void InitAPI()
        {
            CreateInstance();
            SetupDebugMessenger();
            CreateSurface();
            PickPhysicalDevice();
            CreateLogicalDevice();
            CreateSwapChain();
            CreateImageViews();
            CreateRenderPass();
            CreateDescriptorSetLayout();
            CreateGraphicsPipeline();
            CreateCommandPool();
            CreateDepthResources();
            CreateFramebuffers();
            CreateTextureImage();
            CreateTextureImageView();
            CreateTextureSampler();
            //LoadModel();
            //CreateVertexBuffer();
            //CreateIndexBuffer();
            CreateUniformBuffers();
            CreateDescriptorPool();
            CreateDescriptorSets();
            CreateCommandBuffers();
            CreateSyncObjects();
        }

        public class VkSwapChain
        {

        }

        private void CleanUpSwapChain()
        {
            API!.DestroyImageView(device, depthImageView, null);
            API!.DestroyImage(device, depthImage, null);
            API!.FreeMemory(device, depthImageMemory, null);

            foreach (var framebuffer in swapChainFramebuffers!)
                API!.DestroyFramebuffer(device, framebuffer, null);

            fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
            {
                API!.FreeCommandBuffers(device, commandPool, (uint)commandBuffers!.Length, commandBuffersPtr);
            }

            //TODO: Destroy material pipelines

            API!.DestroyRenderPass(device, renderPass, null);

            foreach (var imageView in swapChainImageViews!)
                API!.DestroyImageView(device, imageView, null);
            
            khrSwapChain!.DestroySwapchain(device, swapChain, null);

            for (int i = 0; i < swapChainImages!.Length; i++)
            {
                API!.DestroyBuffer(device, uniformBuffers![i], null);
                API!.FreeMemory(device, uniformBuffersMemory![i], null);
            }

            API!.DestroyDescriptorPool(device, descriptorPool, null);
        }

        protected override void CleanUp()
        {
            CleanUpSwapChain();

            //TODO: destroy all samplers, image views, images, and free image memory

            API!.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

            //TODO: Clean up vertex and index buffers here

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                API!.DestroySemaphore(device, renderFinishedSemaphores![i], null);
                API!.DestroySemaphore(device, imageAvailableSemaphores![i], null);
                API!.DestroyFence(device, inFlightFences![i], null);
            }

            API!.DestroyCommandPool(device, commandPool, null);
            API!.DestroyDevice(device, null);

            if (EnableValidationLayers)
            {
                //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
                debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
            }

            khrSurface!.DestroySurface(instance, surface, null);
            API!.DestroyInstance(instance, null);
            API!.Dispose();
        }

        public override void InitWindow(int w, int h, WindowOptions opts, string title)
        {
            base.InitWindow(w, h, opts, title);

            if (Window!.VkSurface is null)
                throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        private void RecreateSwapChain()
        {
            Vector2D<int> framebufferSize = Window!.FramebufferSize;

            while (framebufferSize.X == 0 || framebufferSize.Y == 0)
            {
                framebufferSize = Window.FramebufferSize;
                Window.DoEvents();
            }

            DeviceWaitIdle();

            CleanUpSwapChain();
            CreateSwapChain();
            CreateImageViews();
            CreateRenderPass();
            CreateGraphicsPipeline();
            CreateDepthResources();
            CreateFramebuffers();
            CreateUniformBuffers();
            CreateDescriptorPool();
            CreateDescriptorSets();
            CreateCommandBuffers();

            imagesInFlight = new Fence[swapChainImages!.Length];
        }

        private void DeviceWaitIdle()
            => API!.DeviceWaitIdle(device);

        protected override Vk GenerateAPI()
            => Vk.GetApi();

        private void CreateInstance()
        {
            if (EnableValidationLayers && !CheckValidationLayerSupport())
                throw new Exception("Validation layers requested, but not available.");

            ApplicationInfo appInfo = new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = ToAnsi("XRENGINE"),
                ApplicationVersion = new Version32(1, 0, 0),
                PEngineName = ToAnsi("XRENGINE"),
                EngineVersion = new Version32(1, 0, 0),
                ApiVersion = Vk.Version12
            };

            InstanceCreateInfo createInfo = new()
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            var extensions = GetRequiredExtensions();
            createInfo.EnabledExtensionCount = (uint)extensions.Length;
            createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);

            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

                DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
                PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
                createInfo.PNext = &debugCreateInfo;
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            if (API.CreateInstance(createInfo, null, out instance) != Result.Success)
                throw new Exception("Failed to create instance.");

            Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
            Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

            if (EnableValidationLayers)
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
        {
            createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
            createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
            createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
            createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
        }

        private void SetupDebugMessenger()
        {
            if (!EnableValidationLayers || !API!.TryGetInstanceExtension(instance, out debugUtils))
                return;

            DebugUtilsMessengerCreateInfoEXT createInfo = new();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
                throw new Exception("Failed to set up debug messenger.");
        }

        private void CreateSurface()
        {
            if (!API!.TryGetInstanceExtension<KhrSurface>(instance, out khrSurface))
                throw new NotSupportedException("KHR_surface extension not found.");

            surface = Window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
        }

        private void PickPhysicalDevice()
        {
            uint devicedCount = 0;
            API!.EnumeratePhysicalDevices(instance, ref devicedCount, null);

            if (devicedCount == 0)
                throw new Exception("Failed to find any GPU with Vulkan support.");

            var devices = new PhysicalDevice[devicedCount];
            fixed (PhysicalDevice* devicesPtr = devices)
            {
                API!.EnumeratePhysicalDevices(instance, ref devicedCount, devicesPtr);
            }

            foreach (var device in devices)
            {
                if (IsDeviceSuitable(device))
                {
                    physicalDevice = device;
                    API!.GetPhysicalDeviceProperties(physicalDevice, out physicalDeviceProperties);
                    break;
                }
            }

            if (physicalDevice.Handle == 0)
                throw new Exception("Failed to find a suitable GPU.");
        }

        private void CreateLogicalDevice()
        {
            var indices = FindQueueFamilies(physicalDevice);

            var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
            uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

            using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
            var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            float queuePriority = 1.0f;
            for (int i = 0; i < uniqueQueueFamilies.Length; i++)
            {
                queueCreateInfos[i] = new()
                {
                    SType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = uniqueQueueFamilies[i],
                    QueueCount = 1,
                    PQueuePriorities = &queuePriority
                };
            }

            PhysicalDeviceFeatures deviceFeatures = new()
            {
                SamplerAnisotropy = true,
            };

            DeviceCreateInfo createInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
                PQueueCreateInfos = queueCreateInfos,

                PEnabledFeatures = &deviceFeatures,

                EnabledExtensionCount = (uint)deviceExtensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions)
            };

            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
            }

            if (API!.CreateDevice(physicalDevice, in createInfo, null, out device) != Result.Success)
            {
                throw new Exception("failed to create logical device!");
            }

            API!.GetDeviceQueue(device, indices.GraphicsFamily!.Value, 0, out graphicsQueue);
            API!.GetDeviceQueue(device, indices.PresentFamily!.Value, 0, out presentQueue);

            if (EnableValidationLayers)
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
        }

        private void CreateSwapChain()
        {
            var swapChainSupport = QuerySwapChainSupport(physicalDevice);

            var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
            var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
            var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            
            SwapchainCreateInfoKHR createInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = surface,

                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            };

            var indices = FindQueueFamilies(physicalDevice);
            var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

            if (indices.GraphicsFamily == indices.PresentFamily)
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            else
                createInfo = createInfo with
                {
                    ImageSharingMode = SharingMode.Concurrent,
                    QueueFamilyIndexCount = 2,
                    PQueueFamilyIndices = queueFamilyIndices,
                };
            
            createInfo = createInfo with
            {
                PreTransform = swapChainSupport.Capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
            };

            if (khrSwapChain is null && !API!.TryGetDeviceExtension(instance, device, out khrSwapChain))
                throw new NotSupportedException("VK_KHR_swapchain extension not found.");

            if (khrSwapChain!.CreateSwapchain(device, createInfo, null, out swapChain) != Result.Success)
                throw new Exception("Failed to create swap chain.");
            
            khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, null);
            swapChainImages = new VkTexture[imageCount];
            Image[] swapChainImages2 = new Image[imageCount];
            fixed (Image* swapChainImagesPtr = swapChainImages2)
            {
                khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, swapChainImagesPtr);
            }
            for (int i = 0; i < imageCount; i++)
            {
                var swapChainImage = swapChainImages[i];
                swapChainImage.textureImage = swapChainImages2[i];
                swapChainImage.format = surfaceFormat.Format;
            }
            swapChainExtent = extent;
        }

        private void CreateImageViews()
        {
            swapChainImageViews = new ImageView[swapChainImages!.Length];
            for (int i = 0; i < swapChainImages.Length; i++)
                swapChainImageViews[i] = CreateImageView(swapChainImages[i], swapChainImageFormat, ImageAspectFlags.ColorBit);
        }

        private void CreateRenderPass()
        {
            AttachmentDescription colorAttachment = new()
            {
                Format = swapChainImageFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
            };

            AttachmentDescription depthAttachment = new()
            {
                Format = FindDepthFormat(),
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.DontCare,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            };

            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            AttachmentReference depthAttachmentRef = new()
            {
                Attachment = 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal,
            };

            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
                PDepthStencilAttachment = &depthAttachmentRef,
            };

            SubpassDependency dependency = new()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
            };

            var attachments = new[] { colorAttachment, depthAttachment };

            fixed (AttachmentDescription* attachmentsPtr = attachments)
            {
                RenderPassCreateInfo renderPassInfo = new()
                {
                    SType = StructureType.RenderPassCreateInfo,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    SubpassCount = 1,
                    PSubpasses = &subpass,
                    DependencyCount = 1,
                    PDependencies = &dependency,
                };

                if (API!.CreateRenderPass(device, renderPassInfo, null, out renderPass) != Result.Success)
                    throw new Exception("failed to create render pass!");
            }
        }

        private void CreateDescriptorSetLayout()
        {
            DescriptorSetLayoutBinding uboLayoutBinding = new()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                PImmutableSamplers = null,
                StageFlags = ShaderStageFlags.VertexBit,
            };

            DescriptorSetLayoutBinding samplerLayoutBinding = new()
            {
                Binding = 1,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                PImmutableSamplers = null,
                StageFlags = ShaderStageFlags.FragmentBit,
            };

            var bindings = new DescriptorSetLayoutBinding[] { uboLayoutBinding, samplerLayoutBinding };

            fixed (DescriptorSetLayoutBinding* bindingsPtr = bindings)
            fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
            {
                DescriptorSetLayoutCreateInfo layoutInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)bindings.Length,
                    PBindings = bindingsPtr,
                };

                if (API!.CreateDescriptorSetLayout(device, layoutInfo, null, descriptorSetLayoutPtr) != Result.Success)
                {
                    throw new Exception("Failed to create descriptor set layout.");
                }
            }
        }

        private void CreateFramebuffers()
        {
            swapChainFramebuffers = new Framebuffer[swapChainImageViews!.Length];

            for (int i = 0; i < swapChainImageViews.Length; i++)
            {
                var attachments = new[] { swapChainImageViews[i], depthImageView };

                fixed (ImageView* attachmentsPtr = attachments)
                {
                    FramebufferCreateInfo framebufferInfo = new()
                    {
                        SType = StructureType.FramebufferCreateInfo,
                        RenderPass = renderPass,
                        AttachmentCount = (uint)attachments.Length,
                        PAttachments = attachmentsPtr,
                        Width = swapChainExtent.Width,
                        Height = swapChainExtent.Height,
                        Layers = 1,
                    };

                    if (API!.CreateFramebuffer(device, framebufferInfo, null, out swapChainFramebuffers[i]) != Result.Success)
                    {
                        throw new Exception("failed to create framebuffer!");
                    }
                }
            }
        }

        private void CreateCommandPool()
        {
            var queueFamiliyIndicies = FindQueueFamilies(physicalDevice);

            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
            };

            if (API!.CreateCommandPool(device, poolInfo, null, out commandPool) != Result.Success)
            {
                throw new Exception("failed to create command pool!");
            }
        }
        private VkTexture depthImage2 = new VkTexture();
        private void CreateDepthResources()
        {
            Format depthFormat = FindDepthFormat();

            depthImage2.CreateImage(
                swapChainExtent.Width,
                swapChainExtent.Height,
                depthFormat,
                ImageTiling.Optimal,
                ImageUsageFlags.DepthStencilAttachmentBit,
                MemoryPropertyFlags.DeviceLocalBit,
                ref depthImage,
                ref depthImageMemory);

            depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit);
        }

        private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
        {
            foreach (var format in candidates)
            {
                API!.GetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);

                if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
                {
                    return format;
                }
                else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
                {
                    return format;
                }
            }

            throw new Exception("failed to find supported format!");
        }

        private Format FindDepthFormat()
            => FindSupportedFormat(new[] 
                {
                    Format.D32Sfloat,
                    Format.D32SfloatS8Uint,
                    Format.D24UnormS8Uint 
                },
                ImageTiling.Optimal,
                FormatFeatureFlags.DepthStencilAttachmentBit);


        public void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
        {
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
            };
            fixed (Buffer* bufferPtr = &buffer)
            {
                if (API!.CreateBuffer(device, bufferInfo, null, bufferPtr) != Result.Success)
                    throw new Exception("Failed to create vertex buffer.");
            }
            MemoryRequirements memRequirements = new();
            API!.GetBufferMemoryRequirements(device, buffer, out memRequirements);
            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
            };
            fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
            {
                if (API!.AllocateMemory(device, allocateInfo, null, bufferMemoryPtr) != Result.Success)
                    throw new Exception("Failed to allocate vertex buffer memory.");
            }
            API!.BindBufferMemory(device, buffer, bufferMemory, 0);
        }
        private CommandBuffer BeginSingleTimeCommands()
        {
            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1,
            };
            API!.AllocateCommandBuffers(device, allocateInfo, out CommandBuffer commandBuffer);
            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            };
            API!.BeginCommandBuffer(commandBuffer, beginInfo);
            return commandBuffer;
        }

        private void EndSingleTimeCommands(CommandBuffer commandBuffer)
        {
            API!.EndCommandBuffer(commandBuffer);
            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBuffer,
            };
            API!.QueueSubmit(graphicsQueue, 1, submitInfo, default);
            API!.QueueWaitIdle(graphicsQueue);
            API!.FreeCommandBuffers(device, commandPool, 1, commandBuffer);
        }

        private void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();
            BufferCopy copyRegion = new()
            {
                Size = size,
            };
            API!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);
            EndSingleTimeCommands(commandBuffer);
        }

        private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
        {
            API!.GetPhysicalDeviceMemoryProperties(physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

            for (int i = 0; i < memProperties.MemoryTypeCount; i++)
                if ((typeFilter & 1 << i) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                    return (uint)i;

            throw new Exception("Failed to find suitable memory type.");
        }

        private void CreateCommandBuffers()
        {
            commandBuffers = new CommandBuffer[swapChainFramebuffers!.Length];

            CommandBufferAllocateInfo allocInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = commandPool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = (uint)commandBuffers.Length,
            };

            fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
            {
                if (API!.AllocateCommandBuffers(device, allocInfo, commandBuffersPtr) != Result.Success)
                    throw new Exception("Failed to allocate command buffers.");
            }

            for (int i = 0; i < commandBuffers.Length; i++)
            {
                CommandBufferBeginInfo beginInfo = new()
                {
                    SType = StructureType.CommandBufferBeginInfo,
                };

                if (API!.BeginCommandBuffer(commandBuffers[i], beginInfo) != Result.Success)
                    throw new Exception("Failed to begin recording command buffer.");

                RenderPassBeginInfo renderPassInfo = new()
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = renderPass,
                    Framebuffer = swapChainFramebuffers[i],
                    RenderArea =
                    {
                        Offset = { X = 0, Y = 0 },
                        Extent = swapChainExtent,
                    }
                };

                var clearValues = new ClearValue[]
                {
                    new()
                    {
                        Color = new ()
                        {
                            Float32_0 = 0,
                            Float32_1 = 0,
                            Float32_2 = 0,
                            Float32_3 = 1,
                        },
                    },
                    new()
                    {
                        DepthStencil = new ()
                        {
                            Depth = 1,
                            Stencil = 0,
                        }
                    }
                };

                fixed (ClearValue* clearValuesPtr = clearValues)
                {
                    renderPassInfo.ClearValueCount = (uint)clearValues.Length;
                    renderPassInfo.PClearValues = clearValuesPtr;
                    API!.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
                }

                //TODO: render models

                API!.CmdEndRenderPass(commandBuffers[i]);

                if (API!.EndCommandBuffer(commandBuffers[i]) != Result.Success)
                    throw new Exception("Failed to record command buffer.");
            }
        }

        private void CreateSyncObjects()
        {
            imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
            renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
            inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
            imagesInFlight = new Fence[swapChainImages!.Length];

            SemaphoreCreateInfo semaphoreInfo = new()
            {
                SType = StructureType.SemaphoreCreateInfo,
            };

            FenceCreateInfo fenceInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.SignaledBit,
            };

            for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
                if (API!.CreateSemaphore(device, semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success ||
                    API!.CreateSemaphore(device, semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success ||
                    API!.CreateFence(device, fenceInfo, null, out inFlightFences[i]) != Result.Success)
                    throw new Exception("Failed to create synchronization objects for a frame.");
        }

        protected override void MainLoop()
        {
            base.MainLoop();
            DeviceWaitIdle();
        }
        protected override void DrawFrame(double delta)
        {
            API!.WaitForFences(device, 1, inFlightFences![currentFrame], true, ulong.MaxValue);

            uint imageIndex = 0;
            var result = khrSwapChain!.AcquireNextImage(device, swapChain, ulong.MaxValue, imageAvailableSemaphores![currentFrame], default, ref imageIndex);

            if (result == Result.ErrorOutOfDateKhr)
            {
                RecreateSwapChain();
                return;
            }
            else if (result != Result.Success && result != Result.SuboptimalKhr)
                throw new Exception("Failed to acquire swap chain image");

            UpdateUniformBuffer(imageIndex);

            if (imagesInFlight![imageIndex].Handle != default)
                API!.WaitForFences(device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
            
            imagesInFlight[imageIndex] = inFlightFences[currentFrame];

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
            };

            var waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };
            var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

            var buffer = commandBuffers![imageIndex];

            submitInfo = submitInfo with
            {
                WaitSemaphoreCount = 1,
                PWaitSemaphores = waitSemaphores,
                PWaitDstStageMask = waitStages,

                CommandBufferCount = 1,
                PCommandBuffers = &buffer
            };

            var signalSemaphores = stackalloc[] { renderFinishedSemaphores![currentFrame] };
            submitInfo = submitInfo with
            {
                SignalSemaphoreCount = 1,
                PSignalSemaphores = signalSemaphores,
            };

            API!.ResetFences(device, 1, inFlightFences[currentFrame]);

            if (API!.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]) != Result.Success)
                throw new Exception("Failed to submit draw command buffer.");

            var swapChains = stackalloc[] { swapChain };
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = signalSemaphores,

                SwapchainCount = 1,
                PSwapchains = swapChains,

                PImageIndices = &imageIndex
            };

            result = khrSwapChain.QueuePresent(presentQueue, presentInfo);

            if (result == Result.ErrorOutOfDateKhr || 
                result == Result.SuboptimalKhr || 
                _frameBufferResized)
            {
                _frameBufferResized = false;
                RecreateSwapChain();
            }
            else if (result != Result.Success)
                throw new Exception("Failed to present swap chain image.");

            currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
        }

        private ShaderModule CreateShaderModule(byte[] code)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
            };

            ShaderModule shaderModule;

            fixed (byte* codePtr = code)
            {
                createInfo.PCode = (uint*)codePtr;

                if (API!.CreateShaderModule(device, createInfo, null, out shaderModule) != Result.Success)
                    throw new Exception();
            }

            return shaderModule;
        }

        private static SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
        {
            foreach (var availableFormat in availableFormats)
                if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
                    return availableFormat;

            return availableFormats[0];
        }

        private static PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
        {
            foreach (var availablePresentMode in availablePresentModes)
                if (availablePresentMode == PresentModeKHR.MailboxKhr)
                    return availablePresentMode;

            return PresentModeKHR.FifoKhr;
        }

        private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
                return capabilities.CurrentExtent;
            else
            {
                var framebufferSize = Window!.FramebufferSize;

                Extent2D actualExtent = new()
                {
                    Width = (uint)framebufferSize.X,
                    Height = (uint)framebufferSize.Y
                };

                actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
                actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

                return actualExtent;
            }
        }

        private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice)
        {
            var details = new SwapChainSupportDetails();

            khrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out details.Capabilities);

            uint formatCount = 0;
            khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, null);

            if (formatCount == 0)
                details.Formats = Array.Empty<SurfaceFormatKHR>();
            else
            {
                details.Formats = new SurfaceFormatKHR[formatCount];
                fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
                {
                    khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, formatsPtr);
                }
            }

            uint presentModeCount = 0;
            khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);

            if (presentModeCount == 0)
                details.PresentModes = Array.Empty<PresentModeKHR>();
            else
            {
                details.PresentModes = new PresentModeKHR[presentModeCount];
                fixed (PresentModeKHR* formatsPtr = details.PresentModes)
                {
                    khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, formatsPtr);
                }
            }

            return details;
        }

        private bool IsDeviceSuitable(PhysicalDevice device)
        {
            var indices = FindQueueFamilies(device);

            bool extensionsSupported = CheckDeviceExtensionsSupport(device);

            bool swapChainAdequate = false;
            if (extensionsSupported)
            {
                var swapChainSupport = QuerySwapChainSupport(device);
                swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
            }

            API!.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures supportedFeatures);
            return indices.IsComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.SamplerAnisotropy;
        }

        private bool CheckDeviceExtensionsSupport(PhysicalDevice device)
        {
            uint extentionsCount = 0;
            API!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, null);

            var availableExtensions = new ExtensionProperties[extentionsCount];
            fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
            {
                API!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
            }

            var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();
            return deviceExtensions.All(availableExtensionNames.Contains);
        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            var indices = new QueueFamilyIndices();

            uint queueFamilityCount = 0;
            API!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

            var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
            fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
            {
                API!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
            }

            uint i = 0;
            foreach (var queueFamily in queueFamilies)
            {
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                    indices.GraphicsFamily = i;
                
                khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var presentSupport);

                if (presentSupport)
                    indices.PresentFamily = i;
                
                if (indices.IsComplete())
                    break;
                
                i++;
            }

            return indices;
        }

        private string[] GetRequiredExtensions()
        {
            var glfwExtensions = Window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
            var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

            if (EnableValidationLayers)
                return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
            
            return extensions;
        }

        private bool CheckValidationLayerSupport()
        {
            uint layerCount = 0;
            API.EnumerateInstanceLayerProperties(ref layerCount, null);
            var availableLayers = new LayerProperties[layerCount];
            fixed (LayerProperties* availableLayersPtr = availableLayers)
            {
                API.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
            }
            var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();
            return validationLayers.All(availableLayerNames.Contains);
        }

        private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            Debug.WriteLine($"validation layer:{FromAnsi(pCallbackData->PMessage)}");
            return Vk.False;
        }
    }
}
