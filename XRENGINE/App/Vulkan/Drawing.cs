using Silk.NET.Vulkan;

public unsafe partial class VulkanAPI : BaseAPI
{
    public void DrawFrame(double delta)
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
            throw new Exception("failed to acquire swap chain image!");
        
        SubmitUniformBuffers(imageIndex);

        if (imagesInFlight![imageIndex].Handle != default)
            vk!.WaitForFences(device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
        
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
            throw new Exception("failed to submit draw command buffer!");
        
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

        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || _frameBufferInvalidated)
        {
            _frameBufferInvalidated = false;
            RecreateSwapChain();
        }
        else if (result != Result.Success)
            throw new Exception("failed to present swap chain image!");
        
        currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
    }
}