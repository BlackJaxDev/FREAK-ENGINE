﻿using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

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

    struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }

    struct Vertex
    {
        public Vector3D<float> pos;
        public Vector3D<float> color;
        public Vector2D<float> textCoord;

        public static VertexInputBindingDescription GetBindingDescription()
        {
            VertexInputBindingDescription bindingDescription = new()
            {
                Binding = 0,
                Stride = (uint)Unsafe.SizeOf<Vertex>(),
                InputRate = VertexInputRate.Vertex,
            };

            return bindingDescription;
        }

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            var attributeDescriptions = new[]
            {
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(textCoord)),
            }
        };

            return attributeDescriptions;
        }
    }

    struct UniformBufferObject
    {
        public Matrix4X4<float> model;
        public Matrix4X4<float> view;
        public Matrix4X4<float> proj;
    }

    public unsafe partial class Vulkan : AbstractRenderer
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

        private IWindow? window;
        private Vk? vk;

        private Instance instance;

        private ExtDebugUtils? debugUtils;
        private DebugUtilsMessengerEXT debugMessenger;
        private KhrSurface? khrSurface;
        private SurfaceKHR surface;

        private PhysicalDevice physicalDevice;
        private Device device;

        private Queue graphicsQueue;
        private Queue presentQueue;

        private KhrSwapchain? khrSwapChain;
        private SwapchainKHR swapChain;
        private Image[]? swapChainImages;
        private Format swapChainImageFormat;
        private Extent2D swapChainExtent;
        private ImageView[]? swapChainImageViews;
        private Framebuffer[]? swapChainFramebuffers;

        private RenderPass renderPass;
        private DescriptorSetLayout descriptorSetLayout;
        private PipelineLayout pipelineLayout;
        private Pipeline graphicsPipeline;

        private CommandPool commandPool;

        private Image depthImage;
        private DeviceMemory depthImageMemory;
        private ImageView depthImageView;

        private Image textureImage;
        private DeviceMemory textureImageMemory;
        private ImageView textureImageView;
        private Sampler textureSampler;

        //private Buffer vertexBuffer;
        //private DeviceMemory vertexBufferMemory;
        //private Buffer indexBuffer;
        //private DeviceMemory indexBufferMemory;

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

        //private Vertex[]? vertices;
        //private uint[]? indices;

        public void Run()
        {
            InitWindow();
            InitVulkan();
            MainLoop();
            CleanUp();
        }

        public void InitWindow()
        {
            //Create a window.
            var options = WindowOptions.DefaultVulkan with
            {
                Size = new Vector2D<int>(WIDTH, HEIGHT),
                Title = "Vulkan",
            };

            window = Window.Create(options);
            window.Initialize();

            if (window.VkSurface is null)
            {
                throw new Exception("Windowing platform doesn't support Vulkan.");
            }

            window.Resize += FramebufferResizeCallback;
        }

        private void FramebufferResizeCallback(Vector2D<int> obj)
        {
            frameBufferResized = true;
        }

        private void InitVulkan()
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

        private void MainLoop()
        {
            window!.Render += DrawFrame;
            window!.Run();
            vk!.DeviceWaitIdle(device);
        }

        private void CleanUpSwapChain()
        {
            vk!.DestroyImageView(device, depthImageView, null);
            vk!.DestroyImage(device, depthImage, null);
            vk!.FreeMemory(device, depthImageMemory, null);

            foreach (var framebuffer in swapChainFramebuffers!)
            {
                vk!.DestroyFramebuffer(device, framebuffer, null);
            }

            fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
            {
                vk!.FreeCommandBuffers(device, commandPool, (uint)commandBuffers!.Length, commandBuffersPtr);
            }

            vk!.DestroyPipeline(device, graphicsPipeline, null);
            vk!.DestroyPipelineLayout(device, pipelineLayout, null);
            vk!.DestroyRenderPass(device, renderPass, null);

            foreach (var imageView in swapChainImageViews!)
            {
                vk!.DestroyImageView(device, imageView, null);
            }

            khrSwapChain!.DestroySwapchain(device, swapChain, null);

            for (int i = 0; i < swapChainImages!.Length; i++)
            {
                vk!.DestroyBuffer(device, uniformBuffers![i], null);
                vk!.FreeMemory(device, uniformBuffersMemory![i], null);
            }

            vk!.DestroyDescriptorPool(device, descriptorPool, null);
        }

        private void CleanUp()
        {
            CleanUpSwapChain();

            vk!.DestroySampler(device, textureSampler, null);
            vk!.DestroyImageView(device, textureImageView, null);

            vk!.DestroyImage(device, textureImage, null);
            vk!.FreeMemory(device, textureImageMemory, null);

            vk!.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

            //vk!.DestroyBuffer(device, indexBuffer, null);
            //vk!.FreeMemory(device, indexBufferMemory, null);

            //vk!.DestroyBuffer(device, vertexBuffer, null);
            //vk!.FreeMemory(device, vertexBufferMemory, null);

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                vk!.DestroySemaphore(device, renderFinishedSemaphores![i], null);
                vk!.DestroySemaphore(device, imageAvailableSemaphores![i], null);
                vk!.DestroyFence(device, inFlightFences![i], null);
            }

            vk!.DestroyCommandPool(device, commandPool, null);

            vk!.DestroyDevice(device, null);

            if (EnableValidationLayers)
            {
                //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
                debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
            }

            khrSurface!.DestroySurface(instance, surface, null);
            vk!.DestroyInstance(instance, null);
            vk!.Dispose();

            window?.Dispose();
        }

        private void RecreateSwapChain()
        {
            Vector2D<int> framebufferSize = window!.FramebufferSize;

            while (framebufferSize.X == 0 || framebufferSize.Y == 0)
            {
                framebufferSize = window.FramebufferSize;
                window.DoEvents();
            }

            vk!.DeviceWaitIdle(device);

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

        private void CreateInstance()
        {
            vk = Vk.GetApi();

            if (EnableValidationLayers && !CheckValidationLayerSupport())
            {
                throw new Exception("validation layers requested, but not available!");
            }

            ApplicationInfo appInfo = new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
                ApplicationVersion = new Version32(1, 0, 0),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
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

            if (vk.CreateInstance(createInfo, null, out instance) != Result.Success)
            {
                throw new Exception("failed to create instance!");
            }

            Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
            Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

            if (EnableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }
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
            createInfo.PfnUserCallback = DebugCallback;
        }

        private void SetupDebugMessenger()
        {
            if (!EnableValidationLayers) return;

            //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
            if (!vk!.TryGetInstanceExtension(instance, out debugUtils)) return;

            DebugUtilsMessengerCreateInfoEXT createInfo = new();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
            {
                throw new Exception("failed to set up debug messenger!");
            }
        }

        private void CreateSurface()
        {
            if (!vk!.TryGetInstanceExtension<KhrSurface>(instance, out khrSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }

            surface = window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
        }

        private void PickPhysicalDevice()
        {
            uint devicedCount = 0;
            vk!.EnumeratePhysicalDevices(instance, ref devicedCount, null);

            if (devicedCount == 0)
            {
                throw new Exception("failed to find GPUs with Vulkan support!");
            }

            var devices = new PhysicalDevice[devicedCount];
            fixed (PhysicalDevice* devicesPtr = devices)
            {
                vk!.EnumeratePhysicalDevices(instance, ref devicedCount, devicesPtr);
            }

            foreach (var device in devices)
            {
                if (IsDeviceSuitable(device))
                {
                    physicalDevice = device;
                    break;
                }
            }

            if (physicalDevice.Handle == 0)
            {
                throw new Exception("failed to find a suitable GPU!");
            }
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

            if (vk!.CreateDevice(physicalDevice, in createInfo, null, out device) != Result.Success)
            {
                throw new Exception("failed to create logical device!");
            }

            vk!.GetDeviceQueue(device, indices.GraphicsFamily!.Value, 0, out graphicsQueue);
            vk!.GetDeviceQueue(device, indices.PresentFamily!.Value, 0, out presentQueue);

            if (EnableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }

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
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

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

            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                createInfo = createInfo with
                {
                    ImageSharingMode = SharingMode.Concurrent,
                    QueueFamilyIndexCount = 2,
                    PQueueFamilyIndices = queueFamilyIndices,
                };
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            createInfo = createInfo with
            {
                PreTransform = swapChainSupport.Capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
            };

            if (khrSwapChain is null)
            {
                if (!vk!.TryGetDeviceExtension(instance, device, out khrSwapChain))
                {
                    throw new NotSupportedException("VK_KHR_swapchain extension not found.");
                }
            }

            if (khrSwapChain!.CreateSwapchain(device, createInfo, null, out swapChain) != Result.Success)
            {
                throw new Exception("failed to create swap chain!");
            }

            khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, null);
            swapChainImages = new Image[imageCount];
            fixed (Image* swapChainImagesPtr = swapChainImages)
            {
                khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, swapChainImagesPtr);
            }

            swapChainImageFormat = surfaceFormat.Format;
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

                if (vk!.CreateRenderPass(device, renderPassInfo, null, out renderPass) != Result.Success)
                {
                    throw new Exception("failed to create render pass!");
                }
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

                if (vk!.CreateDescriptorSetLayout(device, layoutInfo, null, descriptorSetLayoutPtr) != Result.Success)
                {
                    throw new Exception("failed to create descriptor set layout!");
                }
            }
        }

        private void CreateGraphicsPipeline()
        {
            var vertShaderCode = File.ReadAllBytes("shaders/vert.spv");
            var fragShaderCode = File.ReadAllBytes("shaders/frag.spv");

            var vertShaderModule = CreateShaderModule(vertShaderCode);
            var fragShaderModule = CreateShaderModule(fragShaderCode);

            PipelineShaderStageCreateInfo vertShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            PipelineShaderStageCreateInfo fragShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            var shaderStages = stackalloc[]
            {
                vertShaderStageInfo,
                fragShaderStageInfo
            };

            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
            fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
            {

                PipelineVertexInputStateCreateInfo vertexInputInfo = new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    PVertexBindingDescriptions = &bindingDescription,
                    PVertexAttributeDescriptions = attributeDescriptionsPtr,
                };

                PipelineInputAssemblyStateCreateInfo inputAssembly = new()
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false,
                };

                Viewport viewport = new()
                {
                    X = 0,
                    Y = 0,
                    Width = swapChainExtent.Width,
                    Height = swapChainExtent.Height,
                    MinDepth = 0,
                    MaxDepth = 1,
                };

                Rect2D scissor = new()
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = swapChainExtent,
                };

                PipelineViewportStateCreateInfo viewportState = new()
                {
                    SType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    PViewports = &viewport,
                    ScissorCount = 1,
                    PScissors = &scissor,
                };

                PipelineRasterizationStateCreateInfo rasterizer = new()
                {
                    SType = StructureType.PipelineRasterizationStateCreateInfo,
                    DepthClampEnable = false,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    LineWidth = 1,
                    CullMode = CullModeFlags.BackBit,
                    FrontFace = FrontFace.CounterClockwise,
                    DepthBiasEnable = false,
                };

                PipelineMultisampleStateCreateInfo multisampling = new()
                {
                    SType = StructureType.PipelineMultisampleStateCreateInfo,
                    SampleShadingEnable = false,
                    RasterizationSamples = SampleCountFlags.Count1Bit,
                };

                PipelineDepthStencilStateCreateInfo depthStencil = new()
                {
                    SType = StructureType.PipelineDepthStencilStateCreateInfo,
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.Less,
                    DepthBoundsTestEnable = false,
                    StencilTestEnable = false,
                };

                PipelineColorBlendAttachmentState colorBlendAttachment = new()
                {
                    ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                    BlendEnable = false,
                };

                PipelineColorBlendStateCreateInfo colorBlending = new()
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOpEnable = false,
                    LogicOp = LogicOp.Copy,
                    AttachmentCount = 1,
                    PAttachments = &colorBlendAttachment,
                };

                colorBlending.BlendConstants[0] = 0;
                colorBlending.BlendConstants[1] = 0;
                colorBlending.BlendConstants[2] = 0;
                colorBlending.BlendConstants[3] = 0;

                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PushConstantRangeCount = 0,
                    SetLayoutCount = 1,
                    PSetLayouts = descriptorSetLayoutPtr
                };

                if (vk!.CreatePipelineLayout(device, pipelineLayoutInfo, null, out pipelineLayout) != Result.Success)
                {
                    throw new Exception("failed to create pipeline layout!");
                }

                GraphicsPipelineCreateInfo pipelineInfo = new()
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = 2,
                    PStages = shaderStages,
                    PVertexInputState = &vertexInputInfo,
                    PInputAssemblyState = &inputAssembly,
                    PViewportState = &viewportState,
                    PRasterizationState = &rasterizer,
                    PMultisampleState = &multisampling,
                    PDepthStencilState = &depthStencil,
                    PColorBlendState = &colorBlending,
                    Layout = pipelineLayout,
                    RenderPass = renderPass,
                    Subpass = 0,
                    BasePipelineHandle = default
                };

                if (vk!.CreateGraphicsPipelines(device, default, 1, pipelineInfo, null, out graphicsPipeline) != Result.Success)
                {
                    throw new Exception("failed to create graphics pipeline!");
                }
            }

            vk!.DestroyShaderModule(device, fragShaderModule, null);
            vk!.DestroyShaderModule(device, vertShaderModule, null);

            SilkMarshal.Free((nint)vertShaderStageInfo.PName);
            SilkMarshal.Free((nint)fragShaderStageInfo.PName);
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

                    if (vk!.CreateFramebuffer(device, framebufferInfo, null, out swapChainFramebuffers[i]) != Result.Success)
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

            if (vk!.CreateCommandPool(device, poolInfo, null, out commandPool) != Result.Success)
            {
                throw new Exception("failed to create command pool!");
            }
        }

        private void CreateDepthResources()
        {
            Format depthFormat = FindDepthFormat();

            CreateImage(swapChainExtent.Width, swapChainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref depthImage, ref depthImageMemory);
            depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit);
        }

        private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
        {
            foreach (var format in candidates)
            {
                vk!.GetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);

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
        {
            return FindSupportedFormat(new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
        }

        private void CreateTextureImage()
        {
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(TEXTURE_PATH);

            ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

            Buffer stagingBuffer = default;
            DeviceMemory stagingBufferMemory = default;
            CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

            void* data;
            vk!.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
            img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
            vk!.UnmapMemory(device, stagingBufferMemory);

            CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref textureImage, ref textureImageMemory);

            TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
            CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
            TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

            vk!.DestroyBuffer(device, stagingBuffer, null);
            vk!.FreeMemory(device, stagingBufferMemory, null);
        }

        private void CreateTextureImageView()
        {
            textureImageView = CreateImageView(textureImage, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);
        }

        private void CreateTextureSampler()
        {
            vk!.GetPhysicalDeviceProperties(physicalDevice, out PhysicalDeviceProperties properties);

            SamplerCreateInfo samplerInfo = new()
            {
                SType = StructureType.SamplerCreateInfo,
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                AddressModeU = SamplerAddressMode.Repeat,
                AddressModeV = SamplerAddressMode.Repeat,
                AddressModeW = SamplerAddressMode.Repeat,
                AnisotropyEnable = true,
                MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false,
                CompareEnable = false,
                CompareOp = CompareOp.Always,
                MipmapMode = SamplerMipmapMode.Linear,
            };

            fixed (Sampler* textureSamplerPtr = &textureSampler)
                if (vk!.CreateSampler(device, samplerInfo, null, textureSamplerPtr) != Result.Success)
                    throw new Exception("failed to create texture sampler!");
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

            if (vk!.CreateImageView(device, createInfo, null, out ImageView imageView) != Result.Success)
                throw new Exception("Failed to create image views");

            return imageView;
        }

        private void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory)
        {
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
                if (vk!.CreateImage(device, imageInfo, null, imagePtr) != Result.Success)
                {
                    throw new Exception("failed to create image!");
                }
            }

            vk!.GetImageMemoryRequirements(device, image, out MemoryRequirements memRequirements);

            MemoryAllocateInfo allocInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
            };

            fixed (DeviceMemory* imageMemoryPtr = &imageMemory)
                if (vk!.AllocateMemory(device, allocInfo, null, imageMemoryPtr) != Result.Success)
                    throw new Exception("failed to allocate image memory!");

            vk!.BindImageMemory(device, image, imageMemory, 0);
        }

        private void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();

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
                throw new Exception("unsupported layout transition!");

            vk!.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, barrier);

            EndSingleTimeCommands(commandBuffer);
        }

        private void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();

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

            vk!.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, region);

            EndSingleTimeCommands(commandBuffer);
        }

        //private void LoadModel()
        //{
        //    using var assimp = Assimp.GetApi();
        //    var scene = assimp.ImportFile(MODEL_PATH, (uint)PostProcessPreset.TargetRealTimeMaximumQuality);

        //    var vertexMap = new Dictionary<Vertex, uint>();
        //    var vertices = new List<Vertex>();
        //    var indices = new List<uint>();

        //    VisitSceneNode(scene->MRootNode);

        //    assimp.ReleaseImport(scene);

        //    this.vertices = vertices.ToArray();
        //    this.indices = indices.ToArray();

        //    void VisitSceneNode(Node* node)
        //    {
        //        for (int m = 0; m < node->MNumMeshes; m++)
        //        {
        //            var mesh = scene->MMeshes[node->MMeshes[m]];

        //            for (int f = 0; f < mesh->MNumFaces; f++)
        //            {
        //                var face = mesh->MFaces[f];

        //                for (int i = 0; i < face.MNumIndices; i++)
        //                {
        //                    uint index = face.MIndices[i];

        //                    var position = mesh->MVertices[index];
        //                    var texture = mesh->MTextureCoords[0][(int)index];

        //                    Vertex vertex = new Vertex
        //                    {
        //                        pos = new Vector3D<float>(position.X, position.Y, position.Z),
        //                        color = new Vector3D<float>(1, 1, 1),
        //                        //Flip Y for OBJ in Vulkan
        //                        textCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
        //                    };

        //                    if (vertexMap.TryGetValue(vertex, out var meshIndex))
        //                    {
        //                        indices.Add(meshIndex);
        //                    }
        //                    else
        //                    {
        //                        indices.Add((uint)vertices.Count);
        //                        vertexMap[vertex] = (uint)vertices.Count;
        //                        vertices.Add(vertex);
        //                    }
        //                }
        //            }
        //        }

        //        for (int c = 0; c < node->MNumChildren; c++)
        //        {
        //            VisitSceneNode(node->MChildren[c]);
        //        }
        //    }
        //}


        //private void CreateVertexBuffer()
        //{
        //    ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * vertices!.Length);

        //    Buffer stagingBuffer = default;
        //    DeviceMemory stagingBufferMemory = default;
        //    CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferSrcBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        //    void* data;
        //    vk!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        //    vertices.AsSpan().CopyTo(new Span<Vertex>(data, vertices.Length));
        //    vk!.UnmapMemory(device, stagingBufferMemory);

        //    CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferDstBit | BufferUsageFlags.BufferUsageVertexBufferBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, ref vertexBuffer, ref vertexBufferMemory);

        //    CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

        //    vk!.DestroyBuffer(device, stagingBuffer, null);
        //    vk!.FreeMemory(device, stagingBufferMemory, null);
        //}

        //private void CreateIndexBuffer()
        //{
        //    ulong bufferSize = (ulong)(Unsafe.SizeOf<uint>() * indices!.Length);

        //    Buffer stagingBuffer = default;
        //    DeviceMemory stagingBufferMemory = default;
        //    CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferSrcBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        //    void* data;
        //    vk!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        //    indices.AsSpan().CopyTo(new Span<uint>(data, indices.Length));
        //    vk!.UnmapMemory(device, stagingBufferMemory);

        //    CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferDstBit | BufferUsageFlags.BufferUsageIndexBufferBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, ref indexBuffer, ref indexBufferMemory);

        //    CopyBuffer(stagingBuffer, indexBuffer, bufferSize);

        //    vk!.DestroyBuffer(device, stagingBuffer, null);
        //    vk!.FreeMemory(device, stagingBufferMemory, null);
        //}

        private void CreateUniformBuffers()
        {
            ulong bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

            uniformBuffers = new Buffer[swapChainImages!.Length];
            uniformBuffersMemory = new DeviceMemory[swapChainImages!.Length];

            for (int i = 0; i < swapChainImages.Length; i++)
            {
                CreateBuffer(bufferSize, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref uniformBuffers[i], ref uniformBuffersMemory[i]);
            }

        }

        private void CreateDescriptorPool()
        {
            var poolSizes = new DescriptorPoolSize[]
            {
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = (uint)swapChainImages!.Length,
                },
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.CombinedImageSampler,
                    DescriptorCount = (uint)swapChainImages!.Length,
                }
            };

            fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
            fixed (DescriptorPool* descriptorPoolPtr = &descriptorPool)
            {

                DescriptorPoolCreateInfo poolInfo = new()
                {
                    SType = StructureType.DescriptorPoolCreateInfo,
                    PoolSizeCount = (uint)poolSizes.Length,
                    PPoolSizes = poolSizesPtr,
                    MaxSets = (uint)swapChainImages!.Length,
                };

                if (vk!.CreateDescriptorPool(device, poolInfo, null, descriptorPoolPtr) != Result.Success)
                {
                    throw new Exception("failed to create descriptor pool!");
                }

            }
        }

        private void CreateDescriptorSets()
        {
            var layouts = new DescriptorSetLayout[swapChainImages!.Length];
            Array.Fill(layouts, descriptorSetLayout);

            fixed (DescriptorSetLayout* layoutsPtr = layouts)
            {
                DescriptorSetAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.DescriptorSetAllocateInfo,
                    DescriptorPool = descriptorPool,
                    DescriptorSetCount = (uint)swapChainImages!.Length,
                    PSetLayouts = layoutsPtr,
                };

                descriptorSets = new DescriptorSet[swapChainImages.Length];
                fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
                {
                    if (vk!.AllocateDescriptorSets(device, allocateInfo, descriptorSetsPtr) != Result.Success)
                    {
                        throw new Exception("failed to allocate descriptor sets!");
                    }
                }
            }

            for (int i = 0; i < swapChainImages.Length; i++)
            {
                DescriptorBufferInfo bufferInfo = new()
                {
                    Buffer = uniformBuffers![i],
                    Offset = 0,
                    Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),

                };

                DescriptorImageInfo imageInfo = new()
                {
                    ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                    ImageView = textureImageView,
                    Sampler = textureSampler,
                };

                var descriptorWrites = new WriteDescriptorSet[]
                {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
                };

                fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
                {
                    vk!.UpdateDescriptorSets(device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
                }
            }
        }

        private void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
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
                if (vk!.CreateBuffer(device, bufferInfo, null, bufferPtr) != Result.Success)
                    throw new Exception("failed to create vertex buffer!");
            }

            MemoryRequirements memRequirements = new();
            vk!.GetBufferMemoryRequirements(device, buffer, out memRequirements);

            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
            };

            fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
            {
                if (vk!.AllocateMemory(device, allocateInfo, null, bufferMemoryPtr) != Result.Success)
                    throw new Exception("failed to allocate vertex buffer memory!");
            }

            vk!.BindBufferMemory(device, buffer, bufferMemory, 0);
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

            vk!.AllocateCommandBuffers(device, allocateInfo, out CommandBuffer commandBuffer);

            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            };

            vk!.BeginCommandBuffer(commandBuffer, beginInfo);

            return commandBuffer;
        }

        private void EndSingleTimeCommands(CommandBuffer commandBuffer)
        {
            vk!.EndCommandBuffer(commandBuffer);

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBuffer,
            };

            vk!.QueueSubmit(graphicsQueue, 1, submitInfo, default);
            vk!.QueueWaitIdle(graphicsQueue);

            vk!.FreeCommandBuffers(device, commandPool, 1, commandBuffer);
        }

        private void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();

            BufferCopy copyRegion = new()
            {
                Size = size,
            };

            vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

            EndSingleTimeCommands(commandBuffer);
        }

        private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
        {
            vk!.GetPhysicalDeviceMemoryProperties(physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

            for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            {
                if ((typeFilter & 1 << i) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                {
                    return (uint)i;
                }
            }

            throw new Exception("failed to find suitable memory type!");
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
                if (vk!.AllocateCommandBuffers(device, allocInfo, commandBuffersPtr) != Result.Success)
                    throw new Exception("Failed to allocate command buffers");
            }

            for (int i = 0; i < commandBuffers.Length; i++)
            {
                CommandBufferBeginInfo beginInfo = new()
                {
                    SType = StructureType.CommandBufferBeginInfo,
                };

                if (vk!.BeginCommandBuffer(commandBuffers[i], beginInfo) != Result.Success)
                    throw new Exception("Failed to begin recording command buffer");

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
                    vk!.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
                }

                vk!.CmdBindPipeline(commandBuffers[i], PipelineBindPoint.Graphics, graphicsPipeline);

                //var vertexBuffers = new Buffer[] { vertexBuffer };
                //var offsets = new ulong[] { 0 };

                //fixed (ulong* offsetsPtr = offsets)
                //fixed (Buffer* vertexBuffersPtr = vertexBuffers)
                //{
                //    vk!.CmdBindVertexBuffers(commandBuffers[i], 0, 1, vertexBuffersPtr, offsetsPtr);
                //}

                //vk!.CmdBindIndexBuffer(commandBuffers[i], indexBuffer, 0, IndexType.Uint32);

                //vk!.CmdBindDescriptorSets(commandBuffers[i], PipelineBindPoint.Graphics, pipelineLayout, 0, 1, descriptorSets![i], 0, null);

                //vk!.CmdDrawIndexed(commandBuffers[i], (uint)indices!.Length, 1, 0, 0, 0);

                vk!.CmdEndRenderPass(commandBuffers[i]);

                if (vk!.EndCommandBuffer(commandBuffers[i]) != Result.Success)
                    throw new Exception("Failed to record command buffer");
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
            {
                if (vk!.CreateSemaphore(device, semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success ||
                    vk!.CreateSemaphore(device, semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success ||
                    vk!.CreateFence(device, fenceInfo, null, out inFlightFences[i]) != Result.Success)
                {
                    throw new Exception("Failed to create synchronization objects for a frame");
                }
            }
        }

        private void UpdateUniformBuffer(uint currentImage)
        {
            //Silk Window has timing information so we are skipping the time code.
            var time = (float)window!.Time;

            UniformBufferObject ubo = new()
            {
                model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle(new Vector3D<float>(0, 0, 1), time * Scalar.DegreesToRadians(90.0f)),
                view = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
                proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), swapChainExtent.Width / swapChainExtent.Height, 0.1f, 10.0f),
            };
            ubo.proj.M22 *= -1;

            void* data;
            vk!.MapMemory(device, uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
            new Span<UniformBufferObject>(data, 1)[0] = ubo;
            vk!.UnmapMemory(device, uniformBuffersMemory![currentImage]);
        }

        private void DrawFrame(double delta)
        {
            vk!.WaitForFences(device, 1, inFlightFences![currentFrame], true, ulong.MaxValue);

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
            {
                vk!.WaitForFences(device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
            }
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

            vk!.ResetFences(device, 1, inFlightFences[currentFrame]);

            if (vk!.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]) != Result.Success)
                throw new Exception("Failed to submit draw command buffer");

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

            if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || frameBufferResized)
            {
                frameBufferResized = false;
                RecreateSwapChain();
            }
            else if (result != Result.Success)
                throw new Exception("Failed to present swap chain image");

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

                if (vk!.CreateShaderModule(device, createInfo, null, out shaderModule) != Result.Success)
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
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                var framebufferSize = window!.FramebufferSize;

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

            if (formatCount != 0)
            {
                details.Formats = new SurfaceFormatKHR[formatCount];
                fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
                {
                    khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, formatsPtr);
                }
            }
            else
            {
                details.Formats = Array.Empty<SurfaceFormatKHR>();
            }

            uint presentModeCount = 0;
            khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);

            if (presentModeCount != 0)
            {
                details.PresentModes = new PresentModeKHR[presentModeCount];
                fixed (PresentModeKHR* formatsPtr = details.PresentModes)
                {
                    khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, formatsPtr);
                }

            }
            else
            {
                details.PresentModes = Array.Empty<PresentModeKHR>();
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

            vk!.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures supportedFeatures);

            return indices.IsComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.SamplerAnisotropy;
        }

        private bool CheckDeviceExtensionsSupport(PhysicalDevice device)
        {
            uint extentionsCount = 0;
            vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, null);

            var availableExtensions = new ExtensionProperties[extentionsCount];
            fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
            {
                vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
            }

            var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

            return deviceExtensions.All(availableExtensionNames.Contains);

        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            var indices = new QueueFamilyIndices();

            uint queueFamilityCount = 0;
            vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

            var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
            fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
            {
                vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
            }


            uint i = 0;
            foreach (var queueFamily in queueFamilies)
            {
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    indices.GraphicsFamily = i;
                }

                khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var presentSupport);

                if (presentSupport)
                {
                    indices.PresentFamily = i;
                }

                if (indices.IsComplete())
                {
                    break;
                }

                i++;
            }

            return indices;
        }

        private string[] GetRequiredExtensions()
        {
            var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
            var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

            if (EnableValidationLayers)
            {
                return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
            }

            return extensions;
        }

        private bool CheckValidationLayerSupport()
        {
            uint layerCount = 0;
            vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
            var availableLayers = new LayerProperties[layerCount];
            fixed (LayerProperties* availableLayersPtr = availableLayers)
            {
                vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
            }

            var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

            return validationLayers.All(availableLayerNames.Contains);
        }

        private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            System.Diagnostics.Debug.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

            return Vk.False;
        }

        //public const string VK_KHR_SURFACE_EXTENSION_NAME = "VK_KHR_SURFACE_EXTENSION_NAME";
        //public const string VK_KHR_WIN32_SURFACE_EXTENSION_NAME = "VK_KHR_WIN32_SURFACE_EXTENSION_NAME";
        //public const string VK_KHR_XCB_SURFACE_EXTENSION_NAME = "VK_KHR_XCB_SURFACE_EXTENSION_NAME";

        //public static Vk? API { get; set; }
        //public static Instance? Instance { get; set; }
        //public static KhrSurface? Surface { get; set; }

        //private static void EnsureResult(Result result)
        //{
        //    if (result != Result.Success)
        //    {
        //        throw new InvalidOperationException($"Vulkan operation failed with error: {result}");
        //    }
        //}
        //public static unsafe void Initialize()
        //{
        //    API = Vk.GetApi();

        //    // Define Vulkan application info
        //    var appInfo = new ApplicationInfo
        //    {
        //        SType = StructureType.ApplicationInfo,
        //        //PApplicationName = "VulkanOpenXR",
        //        ApplicationVersion = new Version32(1, 0, 0),
        //        //PEngineName = "VulkanEngine",
        //        EngineVersion = new Version32(1, 0, 0),
        //        ApiVersion = Vk.Version11
        //    };

        //    // Define Vulkan instance create info
        //    var createInfo = new InstanceCreateInfo
        //    {
        //        SType = StructureType.InstanceCreateInfo,
        //        PApplicationInfo = &appInfo
        //    };

        //    // Create a Vulkan instance
        //    EnsureResult(API.CreateInstance(&createInfo, null, out var instance));

        //    Instance = instance;
        //}
        //private static unsafe List<ExtensionProperties> GetInstanceExtensions(Vk vk)
        //{
        //    // Get the number of available extensions
        //    uint extensionCount;
        //    EnsureResult(vk.EnumerateInstanceExtensionProperties((byte*)null, &extensionCount, null));

        //    // Allocate memory for extension properties
        //    ExtensionProperties[] extensions = new ExtensionProperties[extensionCount];

        //    // Retrieve extension properties
        //    fixed (ExtensionProperties* pExtensions = extensions)
        //    {
        //        EnsureResult(vk.EnumerateInstanceExtensionProperties((byte*)null, ref extensionCount, pExtensions));
        //    }

        //    return extensions.ToList();
        //}
        //private static unsafe void CreateSurface(IWindow window, out SurfaceKHR surface)
        //{
        //    var instance = Instance.GetValueOrDefault();
        //    var glfw = Glfw.GetApi();
        //    var glfwWindow = ((WindowHandle*)GlfwProvider.GLFW.Value.GetWindow(window))->GlfwWindow;
        //    var result = glfw.CreateWindowSurface(_instance, glfwWindow, null, out surface);

        //    if (API is not null)
        //    {
        //        API.TryGetInstanceExtension(instance, out KhrSurface vkSurface);

        //        var requiredExtensions = glfw.GetRequiredInstanceExtensions(out uint count);

        //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //        {
        //            var surfaceCreateInfo = new Win32SurfaceCreateInfoKHR
        //            {
        //                SType = StructureType.Win32SurfaceCreateInfoKhr,
        //                Hinstance = Process.GetCurrentProcess().Handle,
        //                Hwnd = glfw.GetWin32Window(window)
        //            };

        //            vkSurface.CreateWin32Surface(instance, &surfaceCreateInfo, null, out surface).EnsureVulkanResult();
        //        }
        //        else
        //        {
        //            // Create surface for other platforms (Linux, macOS)
        //            // ... Implement platform-specific surface creation here ...
        //        }

        //        // Create the Vulkan surface
        //        fixed (SurfaceKHR* pSurface = &surface)
        //        {
        //            glfw.CreateWindowSurface(&surfaceCreateInfo, pSurface, out var result);
        //            EnsureResult(result);
        //        }
        //    }
        //}
        //private static unsafe PhysicalDevice ChoosePhysicalDevice(Vk vk, Instance instance)
        //{
        //    // Get the available physical devices
        //    uint deviceCount;
        //    EnsureResult(vk.EnumeratePhysicalDevices(instance, &deviceCount, null));
        //    var devices = new PhysicalDevice[deviceCount];
        //    fixed (PhysicalDevice* dev = devices)
        //        EnsureResult(vk.EnumeratePhysicalDevices(instance, &deviceCount, dev));

        //    // Choose a suitable physical device
        //    foreach (var device in devices)
        //    {
        //        vk.GetPhysicalDeviceProperties(device, out var deviceProperties);

        //        if (deviceProperties.DeviceType == PhysicalDeviceType.DiscreteGpu)
        //        {
        //            return device;
        //        }
        //    }

        //    throw new InvalidOperationException("No suitable physical device found");
        //}
        //private static unsafe void CreateDeviceAndGraphicsQueue(Vk vk, PhysicalDevice physicalDevice, out Device device, out Queue graphicsQueue)
        //{
        //    // Get the queue family properties
        //    uint queueFamilyCount;
        //    vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);
        //    var queueFamilyProperties = new QueueFamilyProperties[queueFamilyCount];
        //    vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilyCount, out queueFamilyProperties[0]);

        //    // Find a suitable queue family
        //    int? queueFamilyIndex = null;
        //    for (var i = 0; i < queueFamilyProperties.Length; i++)
        //    {
        //        if ((queueFamilyProperties[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
        //        {
        //            queueFamilyIndex = i;
        //            break;
        //        }
        //    }

        //    if (!queueFamilyIndex.HasValue)
        //    {
        //        throw new InvalidOperationException("No suitable queue family found");
        //    }

        //    // Create a logical device and a graphics queue
        //    var queueCreateInfo = new DeviceQueueCreateInfo
        //    {
        //        SType = StructureType.DeviceQueueCreateInfo,
        //        QueueFamilyIndex = (uint)queueFamilyIndex.Value,
        //        QueueCount = 1
        //    };

        //    var deviceCreateInfo = new DeviceCreateInfo
        //    {
        //        SType = StructureType.DeviceCreateInfo,
        //        QueueCreateInfoCount = 1,
        //        PQueueCreateInfos = &queueCreateInfo
        //    };

        //    EnsureResult(vk.CreateDevice(physicalDevice, &deviceCreateInfo, null, out device));
        //    vk.GetDeviceQueue(device, (uint)queueFamilyIndex.Value, 0, out graphicsQueue);
        //}

        //public static unsafe void Cleanup()
        //{
        //    if (Instance is null)
        //        return;

        //    var instance = Instance.GetValueOrDefault();
        //    API?.TryGetInstanceExtension(instance, out KhrSurface vkSurface);
        //    vkSurface.DestroySurface(instance, _surface, null);
        //    API?.DestroyInstance(instance, null);
        //    Instance = null;
        //}
    }
}
