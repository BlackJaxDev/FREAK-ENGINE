﻿using XREngine.Components;
using XREngine.Data.Geometry;
using XREngine.Rendering.UI;
using XREngine.Scene;

namespace XREngine.Rendering;

public sealed partial class XRRenderPipelineInstance
{
    public class RenderingState
    {
        /// <summary>
        /// The viewport being rendered to.
        /// May be null if rendering directly to a framebuffer.
        /// </summary>
        public XRViewport? WindowViewport { get; private set; }
        /// <summary>
        /// The scene being rendered.
        /// </summary>
        public VisualScene? Scene { get; private set; }
        /// <summary>
        /// The camera this render pipeline is rendering the scene through.
        /// </summary>
        public XRCamera? SceneCamera { get; private set; }
        /// <summary>
        /// The right eye camera for stereo rendering.
        /// </summary>
        public XRCamera? StereoRightEyeCamera { get; private set; }
        /// <summary>
        /// The output FBO target for the render pass.
        /// May be null if rendering to the screen.
        /// </summary>
        public XRFrameBuffer? OutputFBO { get; private set; }
        /// <summary>
        /// If this pipeline is rendering a shadow pass.
        /// Shadow passes do not need to execute all rendering commands.
        /// </summary>
        public bool ShadowPass { get; private set; } = false;
        /// <summary>
        /// If this pipeline is rendering a stereo pass.
        /// Stereo passes will inject a geometry shader into each mesh pipeline, or expect the mesh to already have a vertex or geometry shader that supports it.
        /// </summary>
        public bool StereoPass { get; private set; } = false;
        /// <summary>
        /// If set, this material will be used to render all objects in the scene.
        /// Typically used for shadow passes.
        /// </summary>
        public XRMaterial? GlobalMaterialOverride { get; set; }
        /// <summary>
        /// The screen-space UI to render over the scene.
        /// </summary>
        public UICanvasComponent? UserInterface { get; private set; }

        //TODO: instead of bools for shadow and stereo passes, use an int for the pass type.

        public StateObject PushMainAttributes(
            XRViewport? viewport,
            VisualScene? scene,
            XRCamera? camera,
            XRCamera? stereoRightEyeCamera,
            XRFrameBuffer? target,
            bool shadowPass,
            bool stereoPass,
            XRMaterial? globalMaterialOverride,
            UICanvasComponent? screenSpaceUI)
        {
            WindowViewport = viewport;
            Scene = scene;
            SceneCamera = camera;
            StereoRightEyeCamera = stereoRightEyeCamera;
            OutputFBO = target;
            ShadowPass = shadowPass;
            StereoPass = stereoPass;
            GlobalMaterialOverride = globalMaterialOverride;
            UserInterface = screenSpaceUI?.CanvasTransform?.DrawSpace == ECanvasDrawSpace.Screen ? screenSpaceUI : null;

            if (WindowViewport is not null)
                _renderingViewports.Push(WindowViewport);

            if (Scene is not null)
                _renderingScenes.Push(Scene);

            if (SceneCamera is not null)
                _renderingCameras.Push(SceneCamera);

            return StateObject.New(PopMainAttributes);
        }

        public void PopMainAttributes()
        {
            if (WindowViewport is not null)
                _renderingViewports.Pop();

            if (Scene is not null)
                _renderingScenes.Pop();

            if (SceneCamera is not null)
                _renderingCameras.Pop();

            WindowViewport = null;
            Scene = null;
            SceneCamera = null;
            StereoRightEyeCamera = null;
            OutputFBO = null;
            ShadowPass = false;
            StereoPass = false;
            GlobalMaterialOverride = null;
            UserInterface = null;
        }

        public XRCamera? RenderingCamera
            => _renderingCameras.TryPeek(out var c) ? c : null;
        private readonly Stack<XRCamera?> _renderingCameras = new();
        public StateObject PushRenderingCamera(XRCamera? camera)
        {
            _renderingCameras.Push(camera);
            return StateObject.New(PopRenderingCamera);
        }
        public void PopRenderingCamera()
            => _renderingCameras.Pop();

        public BoundingRectangle CurrentRenderRegion
            => _renderRegionStack.TryPeek(out var area) ? area : BoundingRectangle.Empty;
        private readonly Stack<BoundingRectangle> _renderRegionStack = new();
        public StateObject PushRenderArea(int width, int height)
            => PushRenderArea(0, 0, width, height);
        public StateObject PushRenderArea(int x, int y, int width, int height)
            => PushRenderArea(new BoundingRectangle(x, y, width, height));
        public StateObject PushRenderArea(BoundingRectangle region)
        {
            _renderRegionStack.Push(region);
            AbstractRenderer.Current?.SetRenderArea(region);
            return StateObject.New(PopRenderArea);
        }
        public void PopRenderArea()
        {
            if (_renderRegionStack.Count <= 0)
                return;

            _renderRegionStack.Pop();
            if (_renderRegionStack.Count > 0)
                AbstractRenderer.Current?.SetRenderArea(_renderRegionStack.Peek());
        }

        public BoundingRectangle CurrentCropRegion
            => _cropRegionStack.TryPeek(out var area) ? area : BoundingRectangle.Empty;
        private readonly Stack<BoundingRectangle> _cropRegionStack = new();
        public StateObject PushCropArea(int width, int height)
            => PushCropArea(0, 0, width, height);
        public StateObject PushCropArea(int x, int y, int width, int height)
            => PushCropArea(new BoundingRectangle(x, y, width, height));
        public StateObject PushCropArea(BoundingRectangle region)
        {
            _cropRegionStack.Push(region);
            AbstractRenderer.Current?.SetCroppingEnabled(true);
            AbstractRenderer.Current?.CropRenderArea(region);
            return StateObject.New(PopCropArea);
        }
        public void PopCropArea()
        {
            if (_cropRegionStack.Count <= 0)
                return;

            _cropRegionStack.Pop();
            if (_cropRegionStack.Count > 0)
                AbstractRenderer.Current?.CropRenderArea(_cropRegionStack.Peek());
        }

        /// <summary>
        /// This material will be used to render all objects in the scene if set.
        /// </summary>
        public XRMaterial? OverrideMaterial
            => _overrideMaterials.TryPeek(out var m) ? m : null;
        private readonly Stack<XRMaterial> _overrideMaterials = new();
        public StateObject PushOverrideMaterial(XRMaterial material)
        {
            _overrideMaterials.Push(material);
            return StateObject.New(PopOverrideMaterial);
        }
        public void PopOverrideMaterial()
            => _overrideMaterials.Pop();

        public IReadOnlyCollection<XRViewport?> ViewportStack => _renderingViewports;

        public XRViewport? RenderingViewport
            => _renderingViewports.TryPeek(out var v) ? v : null;
        private readonly Stack<XRViewport> _renderingViewports = new();
        public StateObject PushViewport(XRViewport viewport)
        {
            _renderingViewports.Push(viewport);
            PushRenderArea(viewport.Region);
            return StateObject.New(PopViewport);
        }
        public void PopViewport()
        {
            _renderingViewports.Pop();
            PopRenderArea();
        }

        public VisualScene? RenderingScene
            => _renderingScenes.TryPeek(out var s) ? s : null;

        private readonly Stack<VisualScene> _renderingScenes = new();
        public StateObject PushRenderingScene(VisualScene scene)
        {
            _renderingScenes.Push(scene);
            return StateObject.New(PopRenderingScene);
        }
        public void PopRenderingScene()
            => _renderingScenes.Pop();
    }
}