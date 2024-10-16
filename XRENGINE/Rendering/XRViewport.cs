﻿using Extensions;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Input;
using XREngine.Physics;
using XREngine.Physics.RayTracing;
using XREngine.Rendering.UI;
using XREngine.Scene;
using State = XREngine.Engine.Rendering.State;

namespace XREngine.Rendering
{
    /// <summary>
    /// Defines a rectangular area to render to.
    /// Can either be a window or render texture.
    /// </summary>
    public sealed class XRViewport : XRBase
    {
        public XRWindow? Window { get; set; }

        private XRCamera? _camera = null;
        private readonly RayTraceClosest _closestPick = new(Vector3.Zero, Vector3.Zero, 0, 0xFFFF);

        private BoundingRectangle _region = new();
        private BoundingRectangle _internalResolutionRegion = new();

        public BoundingRectangle Region => _region;
        public BoundingRectangle InternalResolutionRegion => _internalResolutionRegion;

        public float _leftPercentage = 0.0f;
        public float _rightPercentage = 1.0f;
        public float _bottomPercentage = 0.0f;
        public float _topPercentage = 1.0f;
        private LocalPlayerController? _associatedPlayer = null;

        public int X
        {
            get => _region.X;
            set => _region.X = value;
        }
        public int Y
        {
            get => _region.Y;
            set => _region.Y = value;
        }
        public int Width
        {
            get => _region.Width;
            set => _region.Width = value;
        }
        public int Height
        {
            get => _region.Height;
            set => _region.Height = value;
        }
        public int InternalWidth => _internalResolutionRegion.Width;
        public int InternalHeight => _internalResolutionRegion.Height;

        /// <summary>
        /// The index of the viewport.
        /// Viewports with a lower index will be rendered first, and viewports with a higher index will be rendered last, on top of the previous viewports.
        /// </summary>
        public int Index { get; set; } = 0;
        public LocalPlayerController? AssociatedPlayer
        {
            get => _associatedPlayer;
            internal set
            {
                if (_associatedPlayer == value)
                    return;

                if (_associatedPlayer is not null)
                    _associatedPlayer.Viewport = null;
                SetField(ref _associatedPlayer, value);
                if (_associatedPlayer is not null)
                    _associatedPlayer.Viewport = this;
            
            }
        }

        public XRWorldInstance? WorldInstanceOverride { get; set; } = null;
        public XRWorldInstance? World => WorldInstanceOverride ?? CameraComponent?.SceneNode?.World;

        public Vector2 CursorPosition { get; set; }

        public void Destroy()
        {

        }

        public XRViewport(XRWindow? window)
        {
            Window = window;
            Index = 0;
            SetFullScreen();
        }

        public XRViewport(XRWindow? window, int x, int y, uint width, uint height, int index = 0)
        {
            Window = window;
            X = x;
            Y = y;
            Index = index;
            Resize(width, height);
        }

        public XRViewport(XRWindow? window, uint width, uint height)
        {
            Window = window;
            Index = 0;
            SetFullScreen();
            Resize(width, height);
        }

        private void CollectVisible()
        {
            XRCamera? camera = ActiveCamera;
            if (camera is null)
                return;

            var cameraComponent = CameraComponent;
            World?.VisualScene?.CollectRenderedItems(_renderPipeline.MeshRenderCommands, (cameraComponent?.CullWithFrustum ?? true) ? camera.WorldFrustum() : cameraComponent.CullingFrustumOverride, camera);
            //cameraComponent?.UserInterface?.PreRender(this, cameraComponent);
        }

        private void SwapBuffers()
        {
            _renderPipeline.MeshRenderCommands.SwapBuffers();
        }

        public static XRViewport ForTotalViewportCount(XRWindow window, int totalViewportCount)
        {
            int index = totalViewportCount;
            XRViewport viewport = new(window);
            if (index == 0)
            {
                viewport.Index = index;
                viewport.SetFullScreen();
            }
            else
                viewport.ViewportCountChanged(
                    index,
                    totalViewportCount + 1,
                    Engine.GameSettings.TwoPlayerViewportPreference,
                    Engine.GameSettings.ThreePlayerViewportPreference);

            viewport.Index = index;
            return viewport;
        }

        private CameraComponent? _cameraComponent = null;
        public CameraComponent? CameraComponent
        {
            get => _cameraComponent;
            set => SetField(ref _cameraComponent, value);
        }

        /// <summary>
        /// The camera that will render to this viewport.
        /// </summary>
        public XRCamera? Camera
        {
            get => _camera;
            set => SetField(ref _camera, value);
        }

        public XRCamera? ActiveCamera => _cameraComponent?.Camera ?? _camera;

        /// <summary>
        /// Renders this camera's view to the specified viewport.
        /// </summary>
        /// <param name="vp"></param>
        /// <param name="targetFbo"></param>
        public void Render(XRFrameBuffer? targetFbo = null, VisualScene? sceneOverride = null)
        {
            XRCamera? camera = ActiveCamera;
            if (camera is null)
                return;

            var scene = sceneOverride ?? World?.VisualScene;
            if (scene is null || (State.PipelineState?.ViewportStack.Contains(this) ?? false))
                return;

            if (sceneOverride is not null)
            {
                //Collect and swap now
                CollectVisible();
                SwapBuffers();
            }

            _renderPipeline.Render(scene, camera, this, targetFbo, CameraComponent?.UserInterface);
        }

        private readonly XRRenderPipelineInstance _renderPipeline = new();

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Camera):
                        if (_camera is not null)
                        {
                            _camera.Viewports.Remove(this);
                            Engine.Time.Timer.SwapBuffers -= SwapBuffers;
                            Engine.Time.Timer.CollectVisible -= CollectVisible;
                        }
                        break;
                }
            }
            return change;
        }
        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Camera):
                    if (_camera is not null)
                    {
                        if (!_camera.Viewports.Contains(this))
                            _camera.Viewports.Add(this);
                        SetAspectRatioToCamera();
                        Engine.Time.Timer.SwapBuffers += SwapBuffers;
                        Engine.Time.Timer.CollectVisible += CollectVisible;
                    }
                    if (SetRenderPipelineFromCamera)
                        _renderPipeline.Pipeline = _camera?.RenderPipeline;
                    break;
                case nameof(CameraComponent):
                    ResizeCameraComponentUI();
                    Camera = CameraComponent?.Camera;
                    //_renderPipeline.Pipeline = CameraComponent?.RenderPipeline;
                    break;
            }
        }

        public RenderPipeline? RenderPipeline
        {
            get => _renderPipeline.Pipeline;
            set => _renderPipeline.Pipeline = value;
        }
        public bool SetRenderPipelineFromCamera { get; set; } = true;

        /// <summary>
        /// Sizes the viewport according to the window's size and the sizing percentages set to this viewport.
        /// </summary>
        /// <param name="windowWidth"></param>
        /// <param name="windowHeight"></param>
        /// <param name="setInternalResolution"></param>
        /// <param name="internalResolutionWidth"></param>
        /// <param name="internalResolutionHeight"></param>
        public void Resize(
            uint windowWidth,
            uint windowHeight,
            bool setInternalResolution = true,
            int internalResolutionWidth = -1,
            int internalResolutionHeight = -1)
        {
            float w = windowWidth.ClampMin(1u);
            float h = windowHeight.ClampMin(1u);

            _region.X = (int)(_leftPercentage * w);
            _region.Y = (int)(_bottomPercentage * h);
            _region.Width = (int)(_rightPercentage * w - _region.X);
            _region.Height = (int)(_topPercentage * h - _region.Y);

            if (setInternalResolution)
                SetInternalResolution(
                    internalResolutionWidth <= 0 ? _region.Width : internalResolutionWidth,
                    internalResolutionHeight <= 0 ? _region.Height : internalResolutionHeight,
                    true);

            ResizeCameraComponentUI();
            SetAspectRatioToCamera();
            ResizeRenderPipeline();
        }

        private void ResizeRenderPipeline()
            => _renderPipeline.ViewportResized(Width, Height);

        private void SetAspectRatioToCamera()
        {
            if (Camera?.Parameters is not XRPerspectiveCameraParameters p || !p.InheritAspectRatio)
                return;

            p.AspectRatio = (float)_region.Width / _region.Height;
        }

        private void ResizeCameraComponentUI()
            => CameraComponent?.UserInterface?.Resize(_region.Extents);

        /// <summary>
        /// This is texture dimensions that the camera will render at, which will be scaled up to the actual resolution of the viewport.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetInternalResolution(int width, int height, bool correctAspect)
        {
            _internalResolutionRegion.Width = width;
            _internalResolutionRegion.Height = height;
            if (correctAspect)
            {
                //Shrink the internal resolution to fit the aspect ratio of the viewport
                float aspect = (float)_region.Width / _region.Height;
                if (aspect > 1.0f)
                    _internalResolutionRegion.Height = (int)(_internalResolutionRegion.Width / aspect);
                else
                    _internalResolutionRegion.Width = (int)(_internalResolutionRegion.Height * aspect);
            }
            _renderPipeline.InternalResolutionResized(InternalWidth, InternalHeight);
        }

        public void SetInternalResolutionPercentage(float widthPercent, float heightPercent)
            => SetInternalResolution((int)(widthPercent * _region.Width), (int)(heightPercent * _region.Height), true);

        #region Coordinate conversion
        public Vector3 ScreenToWorld(Vector2 viewportPoint, float depth)
        {
            if (_camera is null)
                throw new InvalidOperationException("No camera is set to this viewport.");

            return _camera.ScreenToWorld(ToInternalResolution(viewportPoint) / _internalResolutionRegion.Extents, depth);
        }
        public Vector3 ScreenToWorld(Vector3 viewportPoint)
            => ScreenToWorld(new Vector2(viewportPoint.X, viewportPoint.Y), viewportPoint.Z);
        public Vector3 WorldToScreen(Vector3 worldPoint)
        {
            if (_camera is null)
                throw new InvalidOperationException("No camera is set to this viewport.");

            Vector3 normScreenPoint = _camera.WorldToScreen(worldPoint);
            return new Vector3(FromInternalResolution(new Vector2(normScreenPoint.X, normScreenPoint.Y) * _internalResolutionRegion.Extents), normScreenPoint.Z);
        }

        public Vector3 NormalizedScreenToWorld(Vector2 viewportPoint, float depth)
        {
            if (_camera is null)
                throw new InvalidOperationException("No camera is set to this viewport.");

            return _camera.ScreenToWorld(viewportPoint, depth);
        }
        public Vector3 NormalizedScreenToWorld(Vector3 viewportPoint)
            => NormalizedScreenToWorld(new Vector2(viewportPoint.X, viewportPoint.Y), viewportPoint.Z);

        public Vector3 WorldToNormalizedScreen(Vector3 worldPoint)
        {
            if (_camera is null)
                throw new InvalidOperationException("No camera is set to this viewport.");

            return _camera.WorldToScreen(worldPoint);
        }

        public Vector2 AbsoluteToRelative(Vector2 absolutePoint)
            => new(absolutePoint.X - _region.X, absolutePoint.Y - _region.Y);

        public Vector2 RelativeToAbsolute(Vector2 viewportPoint)
            => new(viewportPoint.X + _region.X, viewportPoint.Y + _region.Y);

        /// <summary>
        /// Converts a viewport point relative to actual screen resolution
        /// to a point relative to the internal resolution.
        /// </summary>
        public Vector2 ToInternalResolution(Vector2 viewportPoint)
            => viewportPoint * (_internalResolutionRegion.Extents / _region.Extents);

        /// <summary>
        /// Converts a viewport point relative to the internal resolution
        /// to a point relative to the actual screen resolution.
        /// </summary>
        public Vector2 FromInternalResolution(Vector2 viewportPoint)
            => viewportPoint * (_internalResolutionRegion.Extents / _region.Extents);

        #endregion

        #region Picking
        public float GetDepth(Vector2 viewportPoint)
        {
            State.BindFrameBuffer(EFramebufferTarget.ReadFramebuffer, 0);
            State.SetReadBuffer(EDrawBuffersAttachment.None);
            return State.GetDepth(viewportPoint.X, viewportPoint.Y);
        }
        public byte GetStencil(Vector2 viewportPoint)
        {
            State.BindFrameBuffer(EFramebufferTarget.ReadFramebuffer, 0);
            State.SetReadBuffer(EDrawBuffersAttachment.None);
            return State.GetStencilIndex(viewportPoint.X, viewportPoint.Y);
        }
        /// <summary>
        /// Returns a ray projected from the given screen location.
        /// </summary>
        public Ray GetWorldRay(Vector2 viewportPoint)
        {
            if (_camera is null)
                throw new InvalidOperationException("No camera is set to this viewport.");

            return _camera.GetWorldRay(ToInternalResolution(viewportPoint));
        }
        /// <summary>
        /// Returns a segment projected from the given screen location.
        /// Endpoints are located on the NearZ and FarZ planes.
        /// </summary>
        public Segment GetWorldSegment(Vector2 viewportPoint)
        {
            if (_camera is null)
                throw new InvalidOperationException("No camera is set to this viewport.");

            return _camera.GetWorldSegment(ToInternalResolution(viewportPoint));
        }

        /// <summary>
        /// Tests against the HUD and the world for a collision hit at the provided viewport point ray.
        /// </summary>
        /// <param name="viewportPoint"></param>
        /// <param name="testHud"></param>
        /// <param name="interactableHudOnly"></param>
        /// <param name="testWorld"></param>
        /// <param name="hitNormal"></param>
        /// <param name="hitPoint"></param>
        /// <param name="distance"></param>
        /// <param name="ignored"></param>
        /// <returns></returns>
        public XRComponent? PickScene(
            Vector2 viewportPoint,
            bool testHud,
            bool interactableHudOnly,
            bool testWorld,
            out Vector3 hitNormal,
            out Vector3 hitPoint,
            out float distance,
            params XRCollisionObject[] ignored)
        {
            if (testHud)
            {
                UIComponent? hudComp = CameraComponent?.UserInterface?.FindDeepestComponent(viewportPoint);
                bool hasHit = hudComp?.IsVisible ?? false;
                bool hitValidated = !interactableHudOnly || hudComp is UIInteractableComponent;
                if (hasHit && hitValidated)
                {
                    hitNormal = Globals.Backward;
                    hitPoint = new Vector3(viewportPoint, 0.0f);
                    distance = 0.0f;
                    return hudComp;
                }
                //Continue on to test the world is nothing of importance in the HUD was hit
            }
            if (testWorld)
            {
                Segment cursor = GetWorldSegment(viewportPoint);

                _closestPick.StartPointWorld = cursor.Start;
                _closestPick.EndPointWorld = cursor.End;
                _closestPick.Ignored = ignored;

                if (_closestPick.Trace(CameraComponent?.SceneNode?.World))
                {
                    hitNormal = _closestPick.HitNormalWorld;
                    hitPoint = _closestPick.HitPointWorld;
                    distance = hitPoint.Distance(cursor.Start);
                    return _closestPick.CollisionObject?.Owner as XRComponent;
                }

                //Vector3 worldPoint = ScreenToWorld(viewportPoint, depth);
                //ThreadSafeList<I3DRenderable> r = Engine.Scene.RenderTree.FindClosest(worldPoint);
            }
            hitNormal = Vector3.Zero;
            hitPoint = Vector3.Zero;
            distance = 0.0f;
            return null;
        }
        #endregion

        #region Viewport Resizing
        public void ViewportCountChanged(int newIndex, int total, ETwoPlayerPreference twoPlayerPref, EThreePlayerPreference threePlayerPref)
        {
            Index = newIndex;
            switch (total)
            {
                case 1:
                    SetFullScreen();
                    break;
                case 2:
                    switch (newIndex)
                    {
                        case 0:
                            if (twoPlayerPref == ETwoPlayerPreference.SplitHorizontally)
                                SetTop();
                            else
                                SetLeft();
                            break;
                        case 1:
                            if (twoPlayerPref == ETwoPlayerPreference.SplitHorizontally)
                                SetBottom();
                            else
                                SetRight();
                            break;
                    }
                    break;
                case 3:
                    switch (newIndex)
                    {
                        case 0:
                            switch (threePlayerPref)
                            {
                                case EThreePlayerPreference.BlankBottomRight:
                                    SetTopLeft();
                                    break;
                                case EThreePlayerPreference.PreferFirstPlayer:
                                    SetTop();
                                    break;
                                case EThreePlayerPreference.PreferSecondPlayer:
                                    SetBottomLeft();
                                    break;
                                case EThreePlayerPreference.PreferThirdPlayer:
                                    SetTopLeft();
                                    break;
                            }
                            break;
                        case 1:
                            switch (threePlayerPref)
                            {
                                case EThreePlayerPreference.BlankBottomRight:
                                    SetTopRight();
                                    break;
                                case EThreePlayerPreference.PreferFirstPlayer:
                                    SetBottomLeft();
                                    break;
                                case EThreePlayerPreference.PreferSecondPlayer:
                                    SetTop();
                                    break;
                                case EThreePlayerPreference.PreferThirdPlayer:
                                    SetTopRight();
                                    break;
                            }
                            break;
                        case 2:
                            switch (threePlayerPref)
                            {
                                case EThreePlayerPreference.BlankBottomRight:
                                    SetBottomLeft();
                                    break;
                                case EThreePlayerPreference.PreferFirstPlayer:
                                    SetBottomRight();
                                    break;
                                case EThreePlayerPreference.PreferSecondPlayer:
                                    SetBottomRight();
                                    break;
                                case EThreePlayerPreference.PreferThirdPlayer:
                                    SetBottom();
                                    break;
                            }
                            break;
                    }
                    break;
                case 4:
                    switch (newIndex)
                    {
                        case 0: SetTopLeft(); break;
                        case 1: SetTopRight(); break;
                        case 2: SetBottomLeft(); break;
                        case 3: SetBottomRight(); break;
                    }
                    break;
            }
        }
        private void SetTopLeft()
        {
            _leftPercentage = 0.0f;
            _rightPercentage = 0.5f;
            _topPercentage = 1.0f;
            _bottomPercentage = 0.5f;
        }
        private void SetTopRight()
        {
            _leftPercentage = 0.0f;
            _rightPercentage = 0.5f;
            _topPercentage = 1.0f;
            _bottomPercentage = 0.5f;
        }
        private void SetBottomLeft()
        {
            _leftPercentage = 0.0f;
            _rightPercentage = 0.5f;
            _topPercentage = 1.0f;
            _bottomPercentage = 0.5f;
        }
        private void SetBottomRight()
        {
            _leftPercentage = 0.0f;
            _rightPercentage = 0.5f;
            _topPercentage = 1.0f;
            _bottomPercentage = 0.5f;
        }
        private void SetTop()
        {
            _leftPercentage = 0.0f;
            _rightPercentage = 1.0f;
            _topPercentage = 1.0f;
            _bottomPercentage = 0.5f;
        }
        private void SetBottom()
        {
            _leftPercentage = 0.0f;
            _rightPercentage = 1.0f;
            _topPercentage = 0.5f;
            _bottomPercentage = 0.0f;
        }
        private void SetLeft()
        {
            _leftPercentage = 0.0f;
            _rightPercentage = 0.5f;
            _topPercentage = 1.0f;
            _bottomPercentage = 0.0f;
        }
        private void SetRight()
        {
            _leftPercentage = 0.5f;
            _rightPercentage = 1.0f;
            _topPercentage = 1.0f;
            _bottomPercentage = 0.0f;
        }
        private void SetFullScreen()
        {
            _leftPercentage = _bottomPercentage = 0.0f;
            _rightPercentage = _topPercentage = 1.0f;
        }
        #endregion
    }
}