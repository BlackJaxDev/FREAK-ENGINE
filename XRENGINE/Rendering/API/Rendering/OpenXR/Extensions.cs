using Silk.NET.Core.Native;
using Silk.NET.OpenXR.Extensions.KHR;
using Silk.NET.Vulkan.Extensions.EXT;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;

public unsafe partial class OpenXRAPI
{
    public override bool CalcDotLuminance(XRTexture2D texture, Vector3 luminance, out float dotLuminance, bool genMipmapsNow)
    {
        throw new NotImplementedException();
    }
    public override void AllowDepthWrite(bool v)
    {
        throw new NotImplementedException();
    }
    public override void ClearDepth(float v)
    {
        throw new NotImplementedException();
    }
    public override void ClearStencil(int v)
    {
        throw new NotImplementedException();
    }
    public override void EnableDepthTest(bool v)
    {
        throw new NotImplementedException();
    }
    public override void DepthFunc(EComparison always)
    {
        throw new NotImplementedException();
    }
    public override void SetReadBuffer(EDrawBuffersAttachment attachment)
    {
        throw new NotImplementedException();
    }
    public override void BindFrameBuffer(EFramebufferTarget fboTarget, int bindingId)
    {
        throw new NotImplementedException();
    }
    public override void Clear(EFrameBufferTextureType type)
    {
        throw new NotImplementedException();
    }
    public override void StencilMask(uint mask)
    {
        throw new NotImplementedException();
    }
    public override void ClearColor(ColorF4 color)
    {
        throw new NotImplementedException();
    }
    public override float GetDepth(float x, float y)
    {
        throw new NotImplementedException();
    }
    public override byte GetStencilIndex(float x, float y)
    {
        throw new NotImplementedException();
    }

    //private readonly string[] deviceExtensions =
    //[
    //    HtcxViveTrackerInteraction.ExtensionName,
    //    //HtcFacialTracking.ExtensionName,
    //    //HtcFoveation.ExtensionName,
    //    //HtcPassthrough.ExtensionName,
    //    KhrVulkanEnable.ExtensionName,
    //    KhrVulkanEnable2.ExtensionName,
    //    //KhrAndroidSurfaceSwapchain.ExtensionName,
    //];
    public enum Renderer
    {
        OpenGL,
        Vulkan,
    }
    private string[] GetRequiredExtensions(Renderer renderer, params string[] otherExtensions)
    {
        string[] extensions = [];
        switch (renderer)
        {
            case Renderer.Vulkan:
                {
                    var glfwExtensions = Window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
                    extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);
                    if (EnableValidationLayers)
                        extensions = [.. extensions, ExtDebugUtils.ExtensionName];
                    extensions = [.. extensions, KhrVulkanEnable.ExtensionName, KhrVulkanEnable2.ExtensionName];
                }
                break;
            case Renderer.OpenGL:
                {
                    extensions = [.. extensions, KhrOpenglEnable.ExtensionName];
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(renderer), renderer, null);
        }

        if (otherExtensions.Length > 0)
            extensions = [.. extensions, .. otherExtensions];

        return extensions;
    }
}