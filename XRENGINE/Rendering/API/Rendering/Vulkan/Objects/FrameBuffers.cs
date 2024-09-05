using Silk.NET.Vulkan;

namespace XREngine.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private Framebuffer[]? swapChainFramebuffers;

    private void DestroyFrameBuffers()
    {
        foreach (var framebuffer in swapChainFramebuffers!)
            Api!.DestroyFramebuffer(device, framebuffer, null);
    }

    private void CreateFramebuffers()
    {
        swapChainFramebuffers = new Framebuffer[swapChainImageViews!.Length];

        for (int i = 0; i < swapChainImageViews.Length; i++)
        {
            //ImageView[]? attachments = _depth == null 
            //    ? ([swapChainImageViews[i]])
            //    : ([swapChainImageViews[i], _depth.View]);

            //fixed (ImageView* attachmentsPtr = attachments)
            //{
            //    FramebufferCreateInfo framebufferInfo = new()
            //    {
            //        SType = StructureType.FramebufferCreateInfo,
            //        RenderPass = renderPass,
            //        AttachmentCount = (uint)attachments.Length,
            //        PAttachments = attachmentsPtr,
            //        Width = swapChainExtent.Width,
            //        Height = swapChainExtent.Height,
            //        Layers = 1,
            //    };

            //    if (Api!.CreateFramebuffer(device, ref framebufferInfo, null, out swapChainFramebuffers[i]) != Result.Success)
            //        throw new Exception("Failed to create framebuffer.");
            //}
        }
    }
}