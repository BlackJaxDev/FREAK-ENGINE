using Extensions;
using System.ComponentModel;
using XREngine.Rendering;

namespace XREngine.Components
{
    /// <summary>
    /// This component wraps a camera object.
    /// </summary>
    public class CameraComponent : XRComponent
    {
        private readonly Lazy<XRCamera> _camera;
        public XRCamera Camera => _camera.Value;

        public UserInterfaceInputComponent? _userInterface;
        public UserInterfaceInputComponent? UserInterface
        {
            get => _userInterface;
            set
            {
                SetField(ref _userInterface, value);

                //TODO: resize based on if located on top, in camera, or in world
                //_hud?.Resize(Region.Extents);

                Debug.Out($"Set camera user interface: {_userInterface?.GetType()?.GetFriendlyName() ?? "null"}");
            }
        }

        private bool _cullWithFrustum = true;
        /// <summary>
        /// This should always be true, but can be set to false for debug purposes.
        /// </summary>
        public bool CullWithFrustum
        {
            get => _cullWithFrustum;
            set => SetField(ref _cullWithFrustum, value);
        }

        protected CameraComponent() : base()
            => _camera = new(() => new XRCamera(Transform), true);

        protected override void Constructing()
        {
            Camera.Transform = Transform;

            Camera.PropertyChanged += CameraPropertyChanged;
            SceneNode.PropertyChanged += SceneNodePropertyChanged;
        }

        protected override void OnDestroying()
        {
            Camera.PropertyChanged -= CameraPropertyChanged;
            SceneNode.PropertyChanged -= SceneNodePropertyChanged;
        }

        private void CameraPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                //The user is not allowed to update the camera's transform provider
                case nameof(Camera.Transform):
                    Camera.Transform = Transform;
                    break;
            }
        }

        private void SceneNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(XREngine.Scene.SceneNode.Transform):
                    Camera.Transform = Transform;
                    break;
            }
        }

        /// <summary>
        /// Renders this camera's view to the specified viewport.
        /// </summary>
        /// <param name="vp"></param>
        /// <param name="targetFbo"></param>
        public void Render(XRViewport vp, XRFrameBuffer? targetFbo = null, bool preRenderUpdateAndSwap = false)
        {
            if (preRenderUpdateAndSwap)
            {
                vp.RenderPipeline.PreRenderUpdate(this);
                vp.RenderPipeline.PreRenderSwap(this);
            }

            var world = World;
            if (world is null || vp.RenderPipeline.RegeneratingFBOs || Engine.Rendering.State.CurrentlyRenderingViewport == vp)
                return;

            targetFbo ??= vp.RenderPipeline.DefaultRenderTarget;

            world.VisualScene.PreRender(vp, Camera);
            UserInterface?.Canvas?.PreRender(vp, this);

            using (Engine.Rendering.State.PushRenderingViewport(vp))
            {
                bool iblCap = false;
                if (!vp.RenderPipeline.FBOsInitialized)
                {
                    vp.RenderPipeline.InitializeFBOs();
                    iblCap = true;
                }

                vp.RenderPipeline.Render(
                    world.VisualScene,
                    Camera,
                    vp,
                    targetFbo);

                if (iblCap)
                    world.CaptureIBL();

                //hud may sample scene colors, render it after scene
                if (vp.RenderPipeline.UserInterfaceFBO is not null)
                    UserInterface?.Canvas?.RenderScreenSpace(vp, vp.RenderPipeline.UserInterfaceFBO);
            }
        }
    }
}
