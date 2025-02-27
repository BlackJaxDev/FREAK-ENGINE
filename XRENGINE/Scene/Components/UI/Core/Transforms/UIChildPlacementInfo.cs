using System.Numerics;
using XREngine.Data.Core;

namespace XREngine.Rendering.UI
{
    public abstract class UIChildPlacementInfo(UITransform owner) : XRBase
    {
        public UITransform Owner { get; } = owner;

        private bool _relativePositioningChanged = true;
        public bool RelativePositioningChanged
        {
            get => _relativePositioningChanged;
            set => SetField(ref _relativePositioningChanged, value);
        }

        public abstract Matrix4x4 GetRelativeItemMatrix();
    }
}
