using Silk.NET.Maths;
using Silk.NET.Windowing;
using XREngine.Rendering;
using XREngine.Rendering.Graphics.Renderers;

namespace XREDITOR
{
    public class Program
    {
        static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "Silk.NET Window Example";

            using (var window = Window.Create(options))
            {
                window.Initialize();
                window.Load += OnLoad;
                window.Render += OnRender;
                window.Closing += OnClosing;

                window.Run();
            }
        }
        private static void OnLoad()
        {
            Console.WriteLine("Window loaded");
            Vulkan.Initialize();
            OpenXR.Initialize();
        }

        private static void OnRender(double delta)
        {

        }

        private static void OnClosing()
        {
            Console.WriteLine("Window closing");
            Cleanup();
        }

        private static void Cleanup()
        {
            Vulkan.Cleanup();
            OpenXR.Cleanup();
        }
    }
}