//using Extensions;
//using System.ComponentModel;

//namespace XREngine.Components
//{
//    public class PoseBlend1D : PoseGenBase
//    {
//        public PoseBlend1D() { }
//        public PoseBlend1D(params SkelAnimKeyframe[] keyframes)
//            => _poses.AddRange(keyframes);
//        public PoseBlend1D(IEnumerable<SkelAnimKeyframe> keyframes)
//            => _poses.AddRange(keyframes);

//        private KeyframeTrack<SkelAnimKeyframe> _poses = new KeyframeTrack<SkelAnimKeyframe>();
//        private float _interpValue = 0.0f;

//        public float InterpValue 
//        {
//            get => _interpValue; 
//            set => Set(ref _interpValue, value);
//        }
//        public KeyframeTrack<SkelAnimKeyframe> Poses 
//        {
//            get => _poses;
//            set => Set(ref _poses, value);
//        }

//        public override SkeletalAnimationPose GetPose()
//        {
//            SkelAnimKeyframe kf = Poses.GetKeyBefore(InterpValue);
//            if (kf is null)
//                return null;

//            SkeletalAnimationPose frame = kf.AnimationRef?.File?.GetFrame();
//            if (kf.Next is SkelAnimKeyframe kf2)
//            {
//                SkeletalAnimationPose frame2 = kf2.AnimationRef?.File?.GetFrame();
//                if (frame2 != null)
//                {
//                    float diff = InterpValue - kf.Second;
//                    float span = kf2.Second - kf.Second;
//                    float weight = span < float.Epsilon ? 0.0f : diff / span;
//                    return frame?.BlendedWith(frame2, weight);
//                }
//            }
//            return frame;
//        }
//        public override void Tick(float delta)
//        {
//            foreach (SkelAnimKeyframe pose in Poses)
//                pose?.AnimationRef?.File?.Tick(delta);
//        }
//        public class SkelAnimKeyframe : Keyframe
//        {
//            [TSerialize]
//            public GlobalFileRef<SkeletalAnimation> AnimationRef { get; set; } = null;

//            [Browsable(false)]
//            public override Type ValueType => throw new NotImplementedException();
//            public override void ReadFromString(string str)
//            {

//            }
//            public override string WriteToString()
//            {
//                return null;
//            }
//        }
//    }
//}
