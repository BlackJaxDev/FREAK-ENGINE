using System.Numerics;
using Valve.VR;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Transforms from the headset to the left or right eye.
    /// </summary>
    /// <param name="parent"></param>
    public class VREyeTransform : TransformBase
    {
        public bool IsLeftEye { get; }

        public VREyeTransform() { }
        public VREyeTransform(TransformBase? parent)
            : base(parent) { }
        public VREyeTransform(bool isLeftEye, TransformBase? parent = null)
            : this(parent) => IsLeftEye = isLeftEye;

        protected override Matrix4x4 CreateLocalMatrix()
        {
            var eyeEnum = IsLeftEye 
                ? EVREye.Eye_Left 
                : EVREye.Eye_Right;

            return Engine.VRState.Api.IsHeadsetPresent 
                ? Engine.VRState.Api.CVR.GetEyeToHeadTransform(eyeEnum).ToNumerics() 
                : Matrix4x4.Identity;
        }
    }
}