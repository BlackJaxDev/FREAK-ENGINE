using static XREngine.Input.Devices.InputInterface;

namespace XREngine.Input.Devices.Types.OpenVR
{
    public interface IVRHandSkeletonBoneActionTransform<TCategory, TName> : IVRActionTransformBase<TCategory, TName>
        where TCategory : struct, Enum
        where TName : struct, Enum
    {
        public bool LeftHand { get; set; }
        public EVRHandSkeletonBone Bone { get; set; }
        public EVRSkeletalTransformSpace TransformSpace { get; set; }
        public EVRSkeletalMotionRange MotionRange { get; set; }
        public EVRSkeletalReferencePose? ReferencePose { get; set; }
    }
}
