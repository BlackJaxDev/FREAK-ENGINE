using Silk.NET.Maths;
using Silk.NET.OpenAL;
using Silk.NET.OpenXR;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using static Silk.NET.GLFW.GlfwCallbacks;
using Semaphore = Silk.NET.Vulkan.Semaphore;

Window app = new Window();
app.Run();

unsafe partial class Window
{
    const int WIDTH = 800;
    const int HEIGHT = 600;

    private IWindow? window;
    private readonly VulkanAPI vulkan = new VulkanAPI();
    private readonly OpenXRAPI openXR = new OpenXRAPI();

    public void Run()
    {
        InitWindow();
        InitAPIs();
        MainLoop();
        CleanUp();
    }

    private void InitAPIs()
    {
        vulkan.Init();
        openXR.Init();
    }

    private void MainLoop()
    {
        window!.Render += DrawFrame;
        window!.Run();
        vulkan.DeviceWaitIdle();
    }

    private void InitWindow()
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(WIDTH, HEIGHT),
            Title = "XRENGINE",
        };

        vulkan.UpdateWindowOptions(options);
        openXR.UpdateWindowOptions(options);
        
        window = Silk.NET.Windowing.Window.Create(options);
        window.Initialize();

        vulkan.SetWindow(window);
        openXR.SetWindow(window);

        window.Resize += FramebufferResizeCallback;
    }

    private void FramebufferResizeCallback(Vector2D<int> obj)
        => vulkan.FrameBufferResized();

    private void CleanUp()
    {
        vulkan.CleanUp();
        openXR.CleanUp();
        window?.Dispose();
    }

    private void DrawFrame(double delta)
    {
        vulkan.DrawFrame(delta);
        openXR.DrawFrame(delta);
    }
}