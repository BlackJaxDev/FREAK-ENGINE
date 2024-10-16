﻿using Extensions;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using XREngine.Core;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Transforms.Rotations;
using XREngine.Input;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using static XREngine.Engine;

namespace XREngine.Rendering
{
    /// <summary>
    /// An abstract window renderer handles rendering to a specific window using a specific graphics API.
    /// </summary>
    public abstract unsafe class AbstractRenderer : XRBase//, IDisposable
    {
        /// <summary>
        /// If true, this renderer is currently being used to render a window.
        /// </summary>
        public bool Active { get; private set; } = false;

        public static readonly Vector3 UIPositionBias = new(0.0f, 0.0f, 0.1f);
        public static readonly Rotator UIRotation = new(90.0f, 0.0f, 0.0f, ERotationOrder.YPR);

        protected AbstractRenderer(XRWindow window)
        {
            _window = window;

            //Link resizing and rendering methods
            LinkWindow();

            //Set the initial object cache for this window of all existing render objects
            lock (_roCacheLock)
                _renderObjectCache = Engine.Rendering.CreateObjectsForNewRenderer(this);

            _viewports.CollectionChanged += ViewportsChanged;
        }

        protected virtual void RenderFrame()
        {
            Window.DoRender();
            Window.DoEvents();
        }

        protected virtual void SwapBuffers()
        {

        }

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
                if (!IsTickLinked)
                {
                    IsTickLinked = true;
                    BeginTick();
                }
            }
            else
            {
                if (IsTickLinked)
                {
                    IsTickLinked = false;
                    EndTick();
                }
            }
        }

        private void EndTick()
        {
            Time.Timer.SwapBuffers -= SwapBuffers;
            Time.Timer.RenderFrame -= RenderFrame;
            Engine.Rendering.DestroyObjectsForRenderer(this);
            CleanUp();
            Window.DoEvents();
        }

        private void BeginTick()
        {
            Initialize();
            Time.Timer.SwapBuffers += SwapBuffers;
            Time.Timer.RenderFrame += RenderFrame;
        }

        private bool ShouldBeRendering()
            => Viewports.Count > 0 && TargetWorldInstance is not null;

        private readonly object _roCacheLock = new();
        private readonly ConcurrentDictionary<GenericRenderObject, AbstractRenderAPIObject> _renderObjectCache = [];
        public IReadOnlyDictionary<GenericRenderObject, AbstractRenderAPIObject> RenderObjectCache => _renderObjectCache;

        private readonly Stack<BoundingRectangle> _renderAreaStack = new();
        public BoundingRectangle CurrentRenderArea
            => _renderAreaStack.Count > 0 
            ? _renderAreaStack.Peek()
            : new BoundingRectangle(0, 0, Window.Size.X, Window.Size.Y);

        private readonly EventList<XRViewport> _viewports = [];
        public EventList<XRViewport> Viewports => _viewports;

        protected bool _frameBufferInvalidated = false;
        private void FramebufferResizeCallback(Vector2D<int> obj)
        {
            _frameBufferInvalidated = true;
            Viewports.ForEach(vp =>
            {
                vp.Resize((uint)obj.X, (uint)obj.Y, false);
                //vp.SetInternalResolution((int)(obj.X * 0.5f), (int)(obj.X * 0.5f), false);
                //vp.SetInternalResolutionPercentage(0.5f, 0.5f);
                //x.SetInternalResolution(1920, 1080, true);
            });
        }

        public IWindow Window => XRWindow.Window;

        private XRWindow _window;
        public XRWindow XRWindow
        {
            get => _window;
            protected set
            {
                UnlinkWindow();
                _window = value;
                LinkWindow();
            }
        }

        private void UnlinkWindow()
        {
            var w = _window?.Window;
            if (w is null)
                return;
            
            w.Resize -= FramebufferResizeCallback;
            w.Render -= RenderCallback;
        }

        private void LinkWindow()
        {
            IWindow? w = _window?.Window;
            if (w is null)
                return;
            
            w.Resize += FramebufferResizeCallback;
            w.Render += RenderCallback;
        }

        private XRWorldInstance? _worldInstance;
        public XRWorldInstance? TargetWorldInstance
        {
            get => _worldInstance;
            set => SetField(ref _worldInstance, value);
        }

        /// <summary>
        /// Use this to retrieve the currently rendering window renderer.
        /// </summary>
        public static AbstractRenderer? Current { get; private set; }

        //private float _lastFrameTime = 0.0f;
        private void RenderCallback(double delta)
        {
            try
            {
                Active = true;
                Current = this;

                TargetWorldInstance?.GlobalPreRender();
                foreach (var viewport in Viewports)
                    viewport.Render();
                TargetWorldInstance?.GlobalPostRender();
            }
            finally
            {
                Active = false;
                Current = null;
            }
        }

        protected Dictionary<string, bool> _verifiedExtensions = [];
        protected void LogExtension(string name, bool exists)
            => _verifiedExtensions.Add(name, exists);
        protected bool ExtensionChecked(string name)
        {
            _verifiedExtensions.TryGetValue(name, out bool exists);
            return exists;
        }

        public static byte* ToAnsi(string str)
            => (byte*)Marshal.StringToHGlobalAnsi(str);
        public static string? FromAnsi(byte* ptr)
            => Marshal.PtrToStringAnsi((nint)ptr);

        protected abstract void Initialize();
        protected abstract void CleanUp();

        protected abstract void WindowRenderCallback(double delta);
        protected virtual void MainLoop() => Window?.Run();

        public const float DefaultPointSize = 5.0f;
        public const float DefaultLineSize = 1.0f;

        public abstract void CropRenderArea(BoundingRectangle region);
        public abstract void SetRenderArea(BoundingRectangle region);

        /// <summary>
        /// Gets or creates a new API-specific render object linked to this window renderer from a generic render object.
        /// </summary>
        /// <param name="renderObject"></param>
        /// <returns></returns>
        public AbstractRenderAPIObject? GetOrCreateAPIRenderObject(GenericRenderObject? renderObject, bool generateNow = false)
        {
            if (renderObject is null)
                return null;

            AbstractRenderAPIObject? obj;
            lock (_roCacheLock)
            {
                obj = _renderObjectCache.GetOrAdd(renderObject, _ => CreateAPIRenderObject(renderObject));
                if (generateNow && !obj.IsGenerated)
                    obj.Generate();
            }

            return obj;
        }

        public bool TryGetAPIRenderObject(GenericRenderObject renderObject, out AbstractRenderAPIObject? apiObject)
        {
            if (renderObject is null)
            {
                apiObject = null;
                return false;
            }
            lock (_roCacheLock)
            {
                return _renderObjectCache.TryGetValue(renderObject, out apiObject);
            }
        }

        /// <summary>
        /// Converts a generic render object reference into a reference to the wrapper object for this specific renderer.
        /// A generic render object can have multiple wrappers wrapping it at a time, but only one per renderer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="renderObject"></param>
        /// <returns></returns>
        public T? GenericToAPI<T>(GenericRenderObject? renderObject) where T : AbstractRenderAPIObject
            => GetOrCreateAPIRenderObject(renderObject) as T;

        /// <summary>
        /// Creates a new API-specific render object linked to this window renderer from a generic render object.
        /// </summary>
        /// <param name="renderObject"></param>
        /// <returns></returns>
        protected abstract AbstractRenderAPIObject CreateAPIRenderObject(GenericRenderObject renderObject);

        public XRViewport GetOrAddViewportForPlayer(LocalPlayerController controller, bool autoSizeAllViewports)
            => controller.Viewport ??= AddViewportForPlayer(controller, autoSizeAllViewports);

        private XRViewport AddViewportForPlayer(LocalPlayerController? controller, bool autoSizeAllViewports)
        {
            XRViewport newViewport = XRViewport.ForTotalViewportCount(XRWindow, Viewports.Count);
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
            Viewports.Clear();
            for (int i = 0; i < players.Length; i++)
                AddViewportForPlayer(players[i], false);
        }

        public void RegisterLocalPlayer(ELocalPlayerIndex playerIndex, bool autoSizeAllViewports)
            => RegisterController(State.GetOrCreateLocalPlayer(playerIndex), autoSizeAllViewports);

        public void RegisterController(LocalPlayerController controller, bool autoSizeAllViewports)
            => GetOrAddViewportForPlayer(controller, autoSizeAllViewports).AssociatedPlayer = controller;

        public void UnregisterLocalPlayer(ELocalPlayerIndex playerIndex)
        {
            LocalPlayerController? controller = State.GetLocalPlayer(playerIndex);
            if (controller is not null)
                UnregisterController(controller);
        }

        public void UnregisterController(LocalPlayerController controller)
        {
            if (controller.Viewport != null && Viewports.Contains(controller.Viewport))
                controller.Viewport = null;
        }

        public bool CalcDotLuminance(XRTexture2D texture, out float dotLuminance, bool genMipmapsNow)
            => CalcDotLuminance(texture, Engine.Rendering.Settings.DefaultLuminance, out dotLuminance, genMipmapsNow);
        public abstract bool CalcDotLuminance(XRTexture2D texture, Vector3 luminance, out float dotLuminance, bool genMipmapsNow);
        public float CalculateDotLuminance(XRTexture2D texture, bool generateMipmapsNow)
            => CalcDotLuminance(texture, out float dotLum, generateMipmapsNow) ? dotLum : 1.0f;

        public abstract void Clear(bool color, bool depth, bool stencil);
        public abstract void BindFrameBuffer(EFramebufferTarget fboTarget, int bindingId);
        public abstract void ClearColor(ColorF4 color);
        public abstract void SetReadBuffer(EDrawBuffersAttachment attachment);
        public abstract float GetDepth(float x, float y);
        public abstract byte GetStencilIndex(float x, float y);
        public abstract void EnableDepthTest(bool enable);
        public abstract void StencilMask(uint mask);
        public abstract void ClearStencil(int value);
        public abstract void ClearDepth(float value);
        public abstract void AllowDepthWrite(bool allow);
        public abstract void DepthFunc(EComparison always);

        public void Dispose()
        {
            //UnlinkWindow();
            //_viewports.Clear();
            //_currentCamera = null;
            //_worldInstance = null;
            //foreach (var obj in _renderObjectCache.Values)
            //    obj.Destroy();
            //_renderObjectCache.Clear();
            //GC.SuppressFinalize(this);
        }
    }
    public abstract unsafe partial class AbstractRenderer<TAPI>(XRWindow window) : AbstractRenderer(window) where TAPI : NativeAPI
    {
        ~AbstractRenderer() => _api?.Dispose();

        private TAPI? _api;
        protected TAPI Api 
        {
            get => _api ??= GetAPI();
            private set => _api = value;
        }
        protected abstract TAPI GetAPI();

        //protected void VerifyExt<T>(string name, ref T? output) where T : NativeExtension<TAPI>
        //{
        //    if (output is null && !ExtensionChecked(name))
        //        LogExtension(name, LoadExt(out output));
        //}
        //protected abstract bool LoadExt<T>(out T output) where T : NativeExtension<TAPI>?;
    }
}
