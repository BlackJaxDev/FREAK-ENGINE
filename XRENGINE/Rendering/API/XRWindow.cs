using Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using XREngine.Data.Core;
using XREngine.Rendering.OpenGL;
using XREngine.Rendering.Vulkan;

namespace XREngine.Rendering
{
    /// <summary>
    /// Links a Silk.NET generated window to an API-specific engine renderer.
    /// </summary>
    public sealed class XRWindow : XRBase
    {
        /// <summary>
        /// Silk.NET window instance.
        /// </summary>
        public IWindow Window { get; }

        /// <summary>
        /// Interface to render a scene for this window using the requested graphics API.
        /// </summary>
        public AbstractRenderer Renderer { get; }

        public XRWindow(WindowOptions options)
        {
            Window = Silk.NET.Windowing.Window.Create(options);
            Window.Initialize();
            Renderer = Window.API.API switch
            {
                ContextAPI.OpenGL => new OpenGLRenderer(this),
                ContextAPI.Vulkan => new VulkanRenderer(this),
                _ => throw new Exception($"Unsupported API: {Window.API.API}"),
            };
            Window.Closing += Window_Closing;
        }

        private void Window_Closing()
        {
            //Renderer.Dispose();
            //Window.Dispose();
            Engine.RemoveWindow(this);
        }

        private void Window_Resize(Vector2D<int> obj)
        {
            void SetSize(XRViewport vp)
            {
                vp.Resize((uint)obj.X, (uint)obj.Y, false);
                vp.SetInternalResolution(1920, 1080, true);
            }
            Renderer.Viewports.ForEach(SetSize);
        }

        public void UpdateViewportSizes()
            => Window_Resize(Window.Size);
        
        private void Window_FramebufferResize(Vector2D<int> obj)
        {

        }
    }
}
