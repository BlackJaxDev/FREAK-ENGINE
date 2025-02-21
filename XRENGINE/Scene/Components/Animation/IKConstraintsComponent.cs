using XREngine.Components;
using static XREngine.Scene.Components.Animation.InverseKinematics;

namespace XREngine.Scene.Components.Animation
{
    /// <summary>
    /// Specifies constraints for a scene node's in an IK bone chain.
    /// </summary>
    public class IKConstraintsComponent : XRComponent
    {
        private BoneIKConstraints? _constraints;
        public BoneIKConstraints? Constraints
        {
            get => _constraints;
            set => SetField(ref _constraints, value);
        }
    }
}
