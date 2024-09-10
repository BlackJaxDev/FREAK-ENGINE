using System.Numerics;
using Valve.VR;

namespace XREngine.Scene.Transforms
{
    public class VREyeTransform(TransformBase? parent = null) : Transform(parent)
    {
        public bool IsLeftEye { get; }

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