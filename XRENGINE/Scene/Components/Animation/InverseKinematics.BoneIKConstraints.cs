using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Transforms.Rotations;

namespace XREngine.Scene.Components.Animation
{
    public static partial class InverseKinematics
    {
        /// <summary>
        /// Contains constraint settings for a bone's IK.
        /// </summary>
        public class BoneIKConstraints : XRBase
        {
            private Rotator? _minRotation;
            private Rotator? _maxRotation;
            private Vector3 _minPositionOffset;
            private Vector3 _maxPositionOffset;

            public Rotator? MinRotation
            {
                get => _minRotation;
                set => SetField(ref _minRotation, value);
            }
            public Rotator? MaxRotation
            {
                get => _maxRotation;
                set => SetField(ref _maxRotation, value);
            }
            public Vector3 MinPositionOffset
            {
                get => _minPositionOffset;
                set => SetField(ref _minPositionOffset, value);
            }
            public Vector3 MaxPositionOffset
            {
                get => _maxPositionOffset;
                set => SetField(ref _maxPositionOffset, value);
            }
        }
    }
}