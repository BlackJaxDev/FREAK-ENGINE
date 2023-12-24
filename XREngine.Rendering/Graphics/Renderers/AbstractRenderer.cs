using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using System.Runtime.InteropServices;

namespace XREngine.Rendering.Graphics.Renderers
{
    public unsafe abstract class AbstractRenderer<TAPI> where TAPI : NativeAPI
    {
        public class GraphicObject<T> where T : AbstractRenderer<Vk>
        {
            public T Renderer;
            protected Vk API => Renderer.API;
        }
        public class GraphicCamera<T> : GraphicObject<T> where T : AbstractRenderer<Vk>
        {

        }
        public class GraphicModel<T> : GraphicObject<T> where T : AbstractRenderer<Vk>
        {

        }
        public class GraphicMaterial<T> : GraphicObject<T> where T : AbstractRenderer<Vk>
        {

        }
        public class GraphicShader<T> : GraphicObject<T> where T : AbstractRenderer<Vk>
        {

        }
        public class GraphicTexture<T> : GraphicObject<T> where T : AbstractRenderer<Vk>
        {

        }

        protected Dictionary<string, bool> _verifiedExtensions = new Dictionary<string, bool>();
        protected bool _frameBufferResized = false;

        private TAPI? _api;
        public TAPI API
        {
            get => _api ??= GenerateAPI();
            set => _api = value ?? GenerateAPI();
        }

        public IWindow? Window { get; private set; }

        protected abstract TAPI GenerateAPI();

        private void LogExtension(string name, bool exists)
            => _verifiedExtensions.Add(name, exists);
        private bool ExtensionChecked(string name)
        {
            _verifiedExtensions.TryGetValue(name, out bool exists);
            return exists;
        }
        protected void VerifyExt<T>(string name, ref T? output) where T : NativeExtension<TAPI>
        {
            if (output is null && !ExtensionChecked(name))
                LogExtension(name, LoadExt(out output));
        }
        protected abstract bool LoadExt<T>(out T? output) where T : NativeExtension<TAPI>;

        public byte* ToAnsi(string str)
            => (byte*)Marshal.StringToHGlobalAnsi(str);
        public string? FromAnsi(byte* ptr)
            => Marshal.PtrToStringAnsi((nint)ptr);

        public virtual void InitWindow(int w, int h, WindowOptions opts, string title)
        {
            var options = opts with
            {
                Size = new Vector2D<int>(w, h),
                Title = title,
            };

            Window = Silk.NET.Windowing.Window.Create(options);
            Window.Initialize();

            Window.Resize += FramebufferResizeCallback;
            Window.Render += DrawFrame;
        }

        private void FramebufferResizeCallback(Vector2D<int> obj)
            => _frameBufferResized = true;

        protected abstract void InitAPI();
        protected abstract void CleanUp();

        public void Run()
        {
            if (Window is null)
                throw new Exception($"Please initialize a window first with {nameof(InitWindow)}");

            InitAPI();
            MainLoop();
            CleanUp();

            Window?.Dispose();
        }
        protected abstract void DrawFrame(double delta);
        protected virtual void MainLoop() => Window?.Run();
    }
}
