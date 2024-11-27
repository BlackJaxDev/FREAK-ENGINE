using Extensions;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using XREngine.Core;
using XREngine.Data.Core;
using XREngine.Input;
using XREngine.Rendering.OpenGL;
using XREngine.Rendering.Vulkan;

namespace XREngine.Rendering
{
    /// <summary>
    /// Links a Silk.NET generated window to an API-specific engine renderer.
    /// </summary>
    public sealed class XRWindow : XRBase
    {
        private XRWorldInstance? _worldInstance;
        public XRWorldInstance? TargetWorldInstance
        {
            get => _worldInstance;
            set => SetField(ref _worldInstance, value);
        }

        private readonly EventList<XRViewport> _viewports = [];
        public EventList<XRViewport> Viewports => _viewports;

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(TargetWorldInstance):
                    VerifyTick();
                    break;
            }
        }

        private void ViewportsChanged(object sender, TCollectionChangedEventArgs<XRViewport> e)
        {
            switch (e.Action)
            {
                case ECollectionChangedAction.Remove:
                    foreach (var viewport in e.OldItems)
                        viewport.Destroy();
                    break;
                case ECollectionChangedAction.Clear:
                    foreach (var viewport in e.OldItems)
                        viewport.Destroy();
                    break;
            }
            VerifyTick();
        }

        public bool IsTickLinked { get; private set; } = false;
        private void VerifyTick()
        {
            if (ShouldBeRendering())
            {
                if (IsTickLinked)
                    return;

                IsTickLinked = true;
                BeginTick();
            }
            else
            {
                if (!IsTickLinked)
                    return;

                IsTickLinked = false;
                EndTick();
            }
        }

        public void RenderViewports()
        {
            foreach (var viewport in Viewports)
                viewport.Render();
        }

        private void UnlinkWindow()
        {
            var w = Window;
            if (w is null)
                return;

            w.Resize -= FramebufferResizeCallback;
            w.Render -= RenderCallback;
        }

        private void LinkWindow()
        {
            IWindow? w = Window;
            if (w is null)
                return;

            w.Resize += FramebufferResizeCallback;
            w.Render += RenderCallback;
        }

        private void EndTick()
        {
            Engine.Time.Timer.SwapBuffers -= SwapBuffers;
            Engine.Time.Timer.RenderFrame -= RenderFrame;
            //Engine.Rendering.DestroyObjectsForRenderer(Renderer);
            Renderer.CleanUp();
            Window.DoEvents();
        }

        private void BeginTick()
        {
            Renderer.Initialize();
            Engine.Time.Timer.SwapBuffers += SwapBuffers;
            Engine.Time.Timer.RenderFrame += RenderFrame;
        }

        private void SwapBuffers()
        {

        }

        private void RenderFrame()
        {
            Window.DoRender();
            Window.DoEvents();
        }

        public event Action? RenderViewportsCallback;

        //private float _lastFrameTime = 0.0f;
        private void RenderCallback(double delta)
        {
            //using var d = Profiler.Start();

            try
            {
                Renderer.Active = true;
                AbstractRenderer.Current = Renderer;

                TargetWorldInstance?.GlobalPreRender();
                RenderViewportsCallback?.Invoke();
                RenderViewports();
                TargetWorldInstance?.GlobalPostRender();
            }
            finally
            {
                Renderer.Active = false;
                AbstractRenderer.Current = null;
            }
        }

        private bool ShouldBeRendering()
            => Viewports.Count > 0 && TargetWorldInstance is not null;

        private void FramebufferResizeCallback(Vector2D<int> obj)
        {
            Renderer.FrameBufferInvalidated();
            Viewports.ForEach(vp => vp.Resize((uint)obj.X, (uint)obj.Y, false));
        }

        public XRViewport GetOrAddViewportForPlayer(LocalPlayerController controller, bool autoSizeAllViewports)
            => controller.Viewport ??= AddViewportForPlayer(controller, autoSizeAllViewports);

        private XRViewport AddViewportForPlayer(LocalPlayerController? controller, bool autoSizeAllViewports)
        {
            XRViewport newViewport = XRViewport.ForTotalViewportCount(this, Viewports.Count);
            newViewport.AssociatedPlayer = controller;
            Viewports.Add(newViewport);

            Debug.Out("Added new viewport to {0}: {1}", GetType().GetFriendlyName(), newViewport.Index);

            if (autoSizeAllViewports)
                ResizeAllViewportsAccordingToPlayers();

            return newViewport;
        }

        /// <summary>
        /// Remakes all viewports in order of active local player indices.
        /// </summary>
        public void ResizeAllViewportsAccordingToPlayers()
        {
            LocalPlayerController[] players = [.. Viewports.Select(x => x.AssociatedPlayer).Where(x => x is not null).Distinct().OrderBy(x => (int)x!.LocalPlayerIndex)];
            foreach (var viewport in Viewports)
                viewport.Destroy();
            Viewports.Clear();
            for (int i = 0; i < players.Length; i++)
                AddViewportForPlayer(players[i], false);
        }

        public void RegisterLocalPlayer(ELocalPlayerIndex playerIndex, bool autoSizeAllViewports)
            => RegisterController(Engine.State.GetOrCreateLocalPlayer(playerIndex), autoSizeAllViewports);

        public void RegisterController(LocalPlayerController controller, bool autoSizeAllViewports)
            => GetOrAddViewportForPlayer(controller, autoSizeAllViewports).AssociatedPlayer = controller;

        public void UnregisterLocalPlayer(ELocalPlayerIndex playerIndex)
        {
            LocalPlayerController? controller = Engine.State.GetLocalPlayer(playerIndex);
            if (controller is not null)
                UnregisterController(controller);
        }

        public void UnregisterController(LocalPlayerController controller)
        {
            if (controller.Viewport != null && Viewports.Contains(controller.Viewport))
                controller.Viewport = null;
        }

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
            _viewports.CollectionChanged += ViewportsChanged;
            Window = Silk.NET.Windowing.Window.Create(options);
            Window.Load += Window_Load;
            Window.Initialize();
            Renderer = Window.API.API switch
            {
                ContextAPI.OpenGL => new OpenGLRenderer(this, true),
                ContextAPI.Vulkan => new VulkanRenderer(this, true),
                _ => throw new Exception($"Unsupported API: {Window.API.API}"),
            };
            Window.Closing += Window_Closing;
            LinkWindow();
        }

        private void Window_Load()
        {
            //Task.Run(() =>
            //{
                Input = Window.CreateInput();
                Input.ConnectionChanged += Input_ConnectionChanged;
            //});
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
            Viewports.ForEach(SetSize);
        }

        public void UpdateViewportSizes()
            => Window_Resize(Window.Size);
        
        private void Window_FramebufferResize(Vector2D<int> obj)
        {

        }
    }
}
