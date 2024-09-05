//using Extensions;

//namespace XREngine.Components
//{
//    public class PoseBlend : PoseGenBase
//    {
//        public PoseBlend() { }
//        public PoseBlend(SkeletalAnimation anim1, SkeletalAnimation anim2)
//        {
//            AnimationRef1 = anim1;
//            AnimationRef2 = anim2;
//        }

//        public SkeletalAnimation AnimationRef1 { get; set; } = null;
//        public SkeletalAnimation AnimationRef2 { get; set; } = null;

//        private float _interpValue;

//        public float InterpValue 
//        {
//            get => _interpValue; 
//            set => Set(ref _interpValue, value.Clamp(0.0f, 1.0f));
//        }

//        public override SkeletalAnimationPose GetPose()
//        {
//            SkeletalAnimationPose frame1 = AnimationRef1?.File?.GetFrame();
//            SkeletalAnimationPose frame2 = AnimationRef2?.File?.GetFrame();
//            return frame1?.BlendedWith(frame2, InterpValue);
//        }
//        public override void Tick(float delta)
//        {
//            AnimationRef1?.File?.Tick(delta);
//            AnimationRef2?.File?.Tick(delta);
//        }
//    }
//}
