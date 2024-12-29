using System.Numerics;
using XREngine.Data.Core;

namespace XREngine.Rendering.UI
{
    public abstract class UIChildPlacementInfo(UITransform owner) : XRBase
    {
        public UITransform Owner { get; } = owner;

        public abstract Matrix4x4 GetRelativeItemMatrix();
    }
}
