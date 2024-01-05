using Silk.NET.Maths;
using Silk.NET.Vulkan;

public unsafe partial class VulkanAPI : BaseAPI
{
    private Framebuffer[]? swapChainFramebuffers;

    private void DestroyFrameBuffers()
    {
        foreach (var framebuffer in swapChainFramebuffers!)
            vk!.DestroyFramebuffer(device, framebuffer, null);
    }

    private void CreateFramebuffers()
    {
        swapChainFramebuffers = new Framebuffer[swapChainImageViews!.Length];

        for (int i = 0; i < swapChainImageViews.Length; i++)
        {
            ImageView[]? attachments = _depth == null 
                ? (new[] { swapChainImageViews[i] })
                : (new[] { swapChainImageViews[i], _depth.View });

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
                    throw new Exception("failed to create framebuffer!");
            }
        }
    }
}