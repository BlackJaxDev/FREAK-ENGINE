//using Extensions;

//namespace XREngine.Animation
//{
//    public class SkeletalAnimation : BaseAnimation
//    {
//        public SkeletalAnimation() 
//            : base(0.0f, false) { }
//        public SkeletalAnimation(float lengthInSeconds, bool looped)
//            : base(lengthInSeconds, looped) { }
//        public SkeletalAnimation(int frameCount, float FPS, bool looped)
//            : base(frameCount / FPS, looped) { }

//        [TSerialize("BoneAnimations")]
//        private Dictionary<string, BoneAnimation> _boneAnimations = new Dictionary<string, BoneAnimation>();

//        [TPostDeserialize]
//        internal override void PostDeserialize()
//        {
//            foreach (BoneAnimation b in _boneAnimations.Values)
//                b.Parent = this;
//            base.PostDeserialize();
//        }

//        public Dictionary<string, BoneAnimation> BoneAnimations { get => _boneAnimations; set => _boneAnimations = value; }

//        public override void SetLength(float seconds, bool stretchAnimation, bool notifyChanged = true)
//        {
//            foreach (BoneAnimation b in _boneAnimations.Values)
//                b.SetLength(seconds, stretchAnimation, notifyChanged);
//            base.SetLength(seconds, stretchAnimation);
//        }

//        protected internal void Tick(float delta)
//            => Progress(delta);

//        public BoneAnimation FindOrCreateBoneAnimation(string boneName, out bool wasFound)
//        {
//            if (wasFound = _boneAnimations.ContainsKey(boneName))
//                return _boneAnimations[boneName];
            
//            BoneAnimation bone = new BoneAnimation(this, boneName);
//            AddBoneAnimation(bone);
//            return bone;
//        }
//        public void AddBoneAnimation(BoneAnimation anim)
//        {
//            anim.Parent = this;
//            if (_boneAnimations.ContainsKey(anim.Name))
//                _boneAnimations[anim.Name] = anim;
//            else
//                _boneAnimations.Add(anim.Name, anim);
//        }
//        public void RemoveBoneAnimation(string boneName)
//        {
//            if (_boneAnimations.ContainsKey(boneName))
//                _boneAnimations.Remove(boneName);
//        }
//        public void UpdateSkeleton(Skeleton skeleton)
//        {
//            foreach (BoneAnimation bone in _boneAnimations.Values)
//                bone.UpdateSkeleton(skeleton);
//        }
//        public IEnumerable<string> GetAllNames(SkeletalAnimationPose other)
//        {
//            return other.BoneNamesUnion(this);
//        }
//        public IEnumerable<string> GetAllNames(SkeletalAnimation other)
//        {
//            string[] theseNames = new string[_boneAnimations.Keys.Count];
//            _boneAnimations.Keys.CopyTo(theseNames, 0);
//            string[] thoseNames = new string[other._boneAnimations.Keys.Count];
//            other._boneAnimations.Keys.CopyTo(thoseNames, 0);
//            return theseNames.Intersect(thoseNames);
//        }
//        public void UpdateSkeletonBlended(
//            Skeleton skeleton,
//            SkeletalAnimation other,
//            float otherWeight,
//            EAnimBlendType blendType)
//        {
//            foreach (string name in GetAllNames(other))
//            {
//                if (_boneAnimations.ContainsKey(name))
//                {
//                    if (other._boneAnimations.ContainsKey(name))
//                        _boneAnimations[name].UpdateSkeletonBlended(skeleton, other._boneAnimations[name], otherWeight, blendType);
//                    else
//                        _boneAnimations[name].UpdateSkeletonBlended(skeleton, null, otherWeight, blendType);
//                }
//                else
//                {
//                    if (other._boneAnimations.ContainsKey(name))
//                    {
//                        other._boneAnimations[name].UpdateSkeletonBlended(skeleton, null, 1.0f - otherWeight, blendType);
//                    }
//                }
//            }
//        }

//        public SkeletalAnimationPose GetFrame()
//        {
//            SkeletalAnimationPose frame = new SkeletalAnimationPose();
//            foreach (BoneAnimation bone in _boneAnimations.Values)
//                frame.AddBoneFrame(bone.GetFrame());
//            return frame;
//        }

//        public void UpdateSkeletonBlendedMulti(Skeleton skeleton, SkeletalAnimation[] other, float[] otherWeight)
//        {
//            //string[] theseNames = new string[_boneAnimations.Keys.Count];
//            //_boneAnimations.Keys.CopyTo(theseNames, 0);
//            //string[] thoseNames = new string[other._boneAnimations.Keys.Count];
//            //other._boneAnimations.Keys.CopyTo(thoseNames, 0);
//            //IEnumerable<string> names = theseNames.Intersect(thoseNames);
//            //foreach (string name in names)
//            //{
//            //    if (_boneAnimations.ContainsKey(name))
//            //    {
//            //        if (other._boneAnimations.ContainsKey(name))
//            //        {

//            //        }
//            //    }
//            //    else
//            //    {
//            //        if (other._boneAnimations.ContainsKey(name))
//            //        {

//            //        }
//            //    }
//            //}
//        }
        
//        protected override void OnProgressed(float delta)
//        {
//            BoneAnimations.ForEach(x => x.Value.Progress(delta));
//        }
//    }
//}
