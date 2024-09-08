using Extensions;
using System.ComponentModel;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
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

        protected CameraComponent() : base()
            => _camera = new(() => new XRCamera(Transform), true);

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(LocalPlayerIndex):

                        if (LocalPlayerIndex is not null)
                        {
                            var player = Engine.State.GetLocalPlayer(LocalPlayerIndex.Value);
                            player?.Cameras.Remove(this);
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
                case nameof(LocalPlayerIndex):
                    if (LocalPlayerIndex is not null)
                    {
                        var player = Engine.State.GetLocalPlayer(LocalPlayerIndex.Value);
                        player?.Cameras.Add(this);
                    }
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
    }
}
