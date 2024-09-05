﻿using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Image = Silk.NET.Vulkan.Image;
using Format = Silk.NET.Vulkan.Format;

namespace XREngine.Rendering.Vulkan;
public unsafe partial class VulkanRenderer
{
    public Format PreferredFormat { get; set; } = Format.B8G8R8A8Srgb;
    public ColorSpaceKHR PreferredColorSpace { get; set; } = ColorSpaceKHR.SpaceSrgbNonlinearKhr;
    public PresentModeKHR PreferredPresentMode { get; set; } = PresentModeKHR.MailboxKhr;
    public PresentModeKHR FallbackPresentMode { get; set; } = PresentModeKHR.FifoKhr;

    struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }

    private KhrSwapchain? khrSwapChain;
    private SwapchainKHR swapChain;
    private Image[]? swapChainImages;
    //private VkBuffer<UniformBufferObject>[]? uniformBuffers;
    private Format swapChainImageFormat;
    private Extent2D swapChainExtent;
    //private VkTexture? _depth;

    private void RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = Window!.FramebufferSize;
        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = Window.FramebufferSize;
            Window.DoEvents();
        }

        DeviceWaitIdle();
        DestroyAllSwapChainObjects();
        CreateAllSwapChainObjects();

        imagesInFlight = new Fence[swapChainImages!.Length];
    }
    private void DestroyAllSwapChainObjects()
    {
        DestroyDepth();
        DestroyCommandBuffers();
        DestroyFrameBuffers();
        //_testModel?.Destroy();
        DestroyRenderPasses();
        DestroyImageViews();
        DestroySwapChain();
        //DestroyUniformBuffers();
        DestroyDescriptorPool();
    }

    private void CreateAllSwapChainObjects()
    {
        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        //_testModel?.Generate();
        CreateDepth();
        CreateFramebuffers();
        //CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();
    }

    private void DestroyDepth()
    {
        //_depth?.Deallocate();
    }

    private void CreateDepth()
    {
        //_depth = new VkTexture(this, swapChainExtent.Width, swapChainExtent.Height, FindDepthFormat())
        //{
        //    Tiling = ImageTiling.Optimal,
        //    Usage = ImageUsageFlags.DepthStencilAttachmentBit,
        //    Properties = MemoryPropertyFlags.DeviceLocalBit,
        //    AspectFlags = ImageAspectFlags.DepthBit,
        //    CreateSampler = false,
        //};
        //_depth.Allocate();
    }

    private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            Api!.GetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);
            if ((tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features) || 
                (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features))
                return format;
        }

        throw new Exception("failed to find supported format!");
    }

    private Format FindDepthFormat()
        => FindSupportedFormat([Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint], ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);

    private void DestroySwapChain()
        => khrSwapChain!.DestroySwapchain(device, swapChain, null);

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
            createInfo.ImageSharingMode = SharingMode.Exclusive;
        
        createInfo = createInfo with
        {
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,

            OldSwapchain = default
        };

        if (!Api!.TryGetDeviceExtension(instance, device, out khrSwapChain))
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        
        if (khrSwapChain!.CreateSwapchain(device, ref createInfo, null, out swapChain) != Result.Success)
            throw new Exception("failed to create swap chain!");
        
        khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, null);
        swapChainImages = new Image[imageCount];
        fixed (Image* swapChainImagesPtr = swapChainImages)
        {
            khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, swapChainImagesPtr);
        }

        swapChainImageFormat = surfaceFormat.Format;
        swapChainExtent = extent;
    }

    private SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
    {
        foreach (var availableFormat in availableFormats)
            if (availableFormat.Format == PreferredFormat && 
                availableFormat.ColorSpace == PreferredColorSpace)
                return availableFormat;

        return availableFormats[0];
    }

    private PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
    {
        foreach (var availablePresentMode in availablePresentModes)
            if (availablePresentMode == PreferredPresentMode)
                return availablePresentMode;

        return FallbackPresentMode;
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
            details.Formats = [];
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
            details.PresentModes = [];
        
        return details;
    }


}