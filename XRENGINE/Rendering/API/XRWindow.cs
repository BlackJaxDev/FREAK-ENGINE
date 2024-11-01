using Extensions;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using XREngine.Data.Core;
using XREngine.Input.Devices;
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

        public IInputContext? Input { get; private set; }

        public XRWindow(WindowOptions options)
        {
            Window = Silk.NET.Windowing.Window.Create(options);
            Window.Load += Window_Load;
            Window.Initialize();
            Renderer = Window.API.API switch
            {
                ContextAPI.OpenGL => new OpenGLRenderer(this),
                ContextAPI.Vulkan => new VulkanRenderer(this),
                _ => throw new Exception($"Unsupported API: {Window.API.API}"),
            };
            Window.Closing += Window_Closing;
        }

        private void Window_Load()
        {
            Input = Window.CreateInput();
            Input.ConnectionChanged += Input_ConnectionChanged;
        }

        private void Input_ConnectionChanged(IInputDevice device, bool connected)
        {
            switch (device)
            {
                case IKeyboard keyboard:
                    
                    break;
                case IMouse mouse:

                    break;
                case IGamepad gamepad:

                    break;
            }
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
                vp.Resize((uint)obj.X, (uint)obj.Y, true);
                //vp.SetInternalResolution((int)(obj.X * 0.5f), (int)(obj.X * 0.5f), false);
                //vp.SetInternalResolutionPercentage(0.5f, 0.5f);
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
