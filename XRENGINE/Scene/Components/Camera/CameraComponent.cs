using Extensions;
using System.ComponentModel;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Input;
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

        private XRFrameBuffer? _defaultRenderTarget = null;
        public XRFrameBuffer? DefaultRenderTarget 
        {
            get => _defaultRenderTarget;
            set => SetField(ref _defaultRenderTarget, value);
        }

        private ELocalPlayerIndex? _localPlayerIndex = null;
        public ELocalPlayerIndex? LocalPlayerIndex
        {
            get => _localPlayerIndex;
            set => SetField(ref _localPlayerIndex, value);
        }

        public UICanvasComponent? _userInterface;
        public UICanvasComponent? UserInterface
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

        private IVolume? _cullingFrustumOverride = null;
        /// <summary>
        /// When CullWithFrustum is false 
        /// </summary>
        public IVolume? CullingFrustumOverride
        {
            get => _cullingFrustumOverride;
            set => SetField(ref _cullingFrustumOverride, value);
        }

        public CameraComponent() : base()
        {
            _camera = new(() => new XRCamera(Transform), true);
            Engine.State.LocalPlayerAdded += LocalPlayerAdded;
        }
        ~CameraComponent()
        {
            Engine.State.LocalPlayerAdded -= LocalPlayerAdded;
        }

        private void LocalPlayerAdded(LocalPlayerController controller)
        {
            if (controller.LocalPlayerIndex == LocalPlayerIndex)
                controller.Cameras.Add(this);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(LocalPlayerIndex):
                        if (LocalPlayerIndex is not null)
                            Engine.State.GetLocalPlayer(LocalPlayerIndex.Value)?.Cameras.Remove(this);
                        break;
                    case nameof(DefaultRenderTarget):
                        if (DefaultRenderTarget is not null && World is not null)
                            World.FramebufferCameras.Remove(this);
                        break;
                    case nameof(World):
                        if (DefaultRenderTarget is not null && World is not null)
                            World.FramebufferCameras.Remove(this);
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
                case nameof(LocalPlayerIndex):
                    if (LocalPlayerIndex is not null)
                        Engine.State.GetLocalPlayer(LocalPlayerIndex.Value)?.Cameras.Add(this);
                    break;
                //case nameof(RenderPipeline):
                //    if (_fboRenderPipeline is not null)
                //        _fboRenderPipeline.Pipeline = RenderPipeline;
                //    break;
                case nameof(DefaultRenderTarget):
                    if (DefaultRenderTarget is not null && World is not null)
                        if (!World.FramebufferCameras.Contains(this))
                            World.FramebufferCameras.Add(this);
                    break;
                case nameof(World):
                    if (DefaultRenderTarget is not null && World is not null)
                        if (!World.FramebufferCameras.Contains(this))
                            World.FramebufferCameras.Add(this);
                    break;
            }
        }

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

        private readonly XRRenderPipelineInstance _fboRenderPipeline = new();

        //public List<View> CalculateMirrorBounces(int max = 4)
        //{
        //    List<View> bounces = [];
        //    if (max < 1)
        //        return bounces;

        //    Frustum lastFrustum = Camera.WorldFrustum();
        //    bounces.Add(new View(Camera.WorldViewProjectionMatrix, lastFrustum));
        //    //Determine if there are any mirror components that intersect the camera frustum
        //    if (World?.VisualScene?.RenderablesTree is not I3DRenderTree tree)
        //        return bounces;

        //    SortedSet<RenderInfo3D> mirrors = [];
        //    tree.CollectIntersecting(lastFrustum, false, x =>
        //    {
        //        if (x is RenderInfo3D info && info.Owner is MirrorComponent mirror)
        //            return true;
        //    });
        //    Matrix4x4 mirrorScaleZ = Matrix4x4.CreateScale(1, 1, -1);
        //    foreach (var mirror in Engine.State.GetComponents<MirrorComponent>())
        //    {
        //        if (mirror.CullWithFrustum && !lastFrustum.Intersects(mirror.WorldVolume))
        //            continue;

        //        //Calculate the reflection matrix
        //        Matrix4x4 mirrorMatrix = mirror.WorldMatrix;
        //        Matrix4x4 reflectionMatrix = mirrorScaleZ * mirrorMatrix;
        //        Matrix4x4 mvp = Camera.WorldViewProjectionMatrix * reflectionMatrix;
        //        Frustum frustum = lastFrustum.Transform(reflectionMatrix);
        //        bounces.Add(new View(mvp, frustum));
        //        lastFrustum = frustum;
        //    }


        //    return bounces;
        //}
    }

    internal record struct View(Matrix4x4 MVP, Frustum Frustum)
    {
        public static implicit operator (Matrix4x4 mvp, Frustum frustum)(View value)
            => (value.MVP, value.Frustum);
        public static implicit operator View((Matrix4x4 mvp, Frustum frustum) value)
            => new(value.mvp, value.frustum);
    }
}
