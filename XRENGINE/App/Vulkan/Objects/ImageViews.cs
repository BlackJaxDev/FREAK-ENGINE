using Silk.NET.Vulkan;

public unsafe partial class VulkanAPI : BaseAPI
{
    private ImageView[]? swapChainImageViews;

    private void DestroyImageViews()
    {
        foreach (var imageView in swapChainImageViews!)
            vk!.DestroyImageView(device, imageView, null);
    }

    private void CreateImageViews()
    {
        swapChainImageViews = new ImageView[swapChainImages!.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = swapChainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = swapChainImageFormat,
                Components =
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity,
                },
                SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }

            };

            if (vk!.CreateImageView(device, createInfo, null, out swapChainImageViews[i]) != Result.Success)
                throw new Exception("failed to create image views!");
        }
    }
}