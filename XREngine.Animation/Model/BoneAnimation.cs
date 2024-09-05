//using System.ComponentModel;
//using XREngine.Data.Core;

//namespace XREngine.Animation
//{
//    public class BoneAnimation : XRBase
//    {
//        public BoneAnimation() { }
//        public BoneAnimation(SkeletalAnimation parent, string name)
//        {
//            Name = name;
//            Parent = parent;
//        }

//        /// <summary>
//        /// Determines which method to use, baked or keyframed.
//        /// Keyframed takes up less memory and calculates in-between frames on the fly, which allows for time dilation.
//        /// Baked takes up more memory but requires no calculations. However, the animation cannot be sped up at all, nor slowed down without artifacts.
//        /// </summary>
//        public bool UseKeyframes
//        {
//            get => _useKeyframes;
//            set
//            {
//                _useKeyframes = value;
//                UseKeyframesChanged();
//            }
//        }

//        protected virtual void UseKeyframesChanged() { }

//        public float LengthInSeconds => _tracks.LengthInSeconds;

//        public void SetLength(float seconds, bool stretchAnimation, bool notifyChanged = true)
//            => _tracks.SetLength(seconds, stretchAnimation, notifyChanged);

//        internal SkeletalAnimation Parent { get; set; }

//        public override string Name { get => base.Name; set => base.Name = value; }

//        private bool _useKeyframes = true;

//        private TransformKeyCollection _tracks = new TransformKeyCollection();

//        [Category("Bone Animation")]
//        public PropAnimFloat TranslationX => _tracks.TranslationX;
//        [Category("Bone Animation")]
//        public PropAnimFloat TranslationY => _tracks.TranslationY;
//        [Category("Bone Animation")]
//        public PropAnimFloat TranslationZ => _tracks.TranslationZ;
        
//        [Category("Bone Animation")]
//        public PropAnimQuat Rotation => _tracks.Rotation;

//        [Category("Bone Animation")]
//        public PropAnimFloat ScaleX => _tracks.ScaleX;
//        [Category("Bone Animation")]
//        public PropAnimFloat ScaleY => _tracks.ScaleY;
//        [Category("Bone Animation")]
//        public PropAnimFloat ScaleZ => _tracks.ScaleZ;

//        public void Progress(float delta)
//            => _tracks.Progress(delta);
        
//        //TODO: pool bone frames
//        public BoneFrame GetFrame()
//            => new BoneFrame(Name, _tracks);
//        public BoneFrame GetFrame(float second)
//            => new BoneFrame(Name, _tracks, second);
        
//        //public void SetValue(Matrix4 transform, float frameIndex, PlanarInterpType planar, RadialInterpType radial)
//        //{
//        //    FrameState state = FrameState.DeriveTRS(transform);
//        //    _translation.Add(new Vector3Keyframe(frameIndex, state.Translation, planar));
//        //    _rotation.Add(new QuatKeyframe(frameIndex, state.Quaternion, radial));
//        //    _scale.Add(new Vector3Keyframe(frameIndex, state.Scale, planar));
//        //}
//        private HashSet<string> _boneNotFoundCache = new HashSet<string>();
//        public void UpdateSkeleton(Skeleton skeleton)
//        {
//            IBone bone = skeleton[Name];
//            if (bone != null)
//                UpdateState(bone.FrameState, bone.BindState);
//            else if (!_boneNotFoundCache.Contains(Name))
//            {
//                _boneNotFoundCache.Add(Name);
//                Engine.Out($"Bone '{Name}' not found in skeleton '{skeleton.ToString()}'.");
//            }
//        }
//        public void UpdateState(ITransform frameState, ITransform bindState)
//        {
//            GetTransform(bindState, out Vector3 translation, out Quat rotation, out Vector3 scale);
//            frameState.SetAll(translation, rotation, scale);
//        }
//        public void UpdateState(ITransform frameState, ITransform bindState, float second)
//        {
//            GetTransform(bindState, second, out Vector3 translation, out Quat rotation, out Vector3 scale);
//            frameState.SetAll(translation, rotation, scale);
//        }

//        /// <summary>
//        /// Retrieves the parts of the transform for this bone at the current frame second.
//        /// </summary>
//        public unsafe void GetTransform(ITransform bindState, out Vector3 translation, out Quat rotation, out Vector3 scale)
//            => _tracks.GetTransform(bindState, out translation, out rotation, out scale);
//        /// <summary>
//        /// Retrieves the parts of the transform for this bone at the requested frame second.
//        /// </summary>
//        public unsafe void GetTransform(ITransform bindState, float second, out Vector3 translation, out Quat rotation, out Vector3 scale)
//            => _tracks.GetTransform(bindState, out translation, out rotation, out scale, second);
//        public void UpdateStateBlended(ITransform frameState, ITransform bindState, BoneAnimation otherBoneAnim, float otherWeight, EAnimBlendType blendType)
//            => UpdateStateBlended(frameState, bindState, otherBoneAnim, Parent.CurrentTime, otherBoneAnim.Parent.CurrentTime, otherWeight, blendType);
//        public void UpdateStateBlended(
//            ITransform frameState,
//            ITransform bindState,
//            BoneAnimation otherBoneAnim,
//            float thisSecond,
//            float otherSecond,
//            float otherWeight,
//            EAnimBlendType blendType)
//        {
//            GetTransform(bindState, thisSecond, out Vector3 t1, out Quat r1, out Vector3 s1);
//            otherBoneAnim.GetTransform(bindState, otherSecond, out Vector3 t2, out Quat r2, out Vector3 s2);

//            otherWeight = Interp.TimeModifier(otherWeight, blendType);

//            Vector3 t = Vector3.Lerp(t1, t2, otherWeight);
//            Quat r = Quat.Slerp(r1, r2, otherWeight);
//            Vector3 s = Vector3.Lerp(s1, s2, otherWeight);

//            frameState.SetAll(t, r, s);
//        }
//        public void UpdateSkeletonBlended(
//            ISkeleton skeleton,
//            BoneAnimation otherBoneAnim,
//            float otherWeight,
//            EAnimBlendType blendType)
//        {
//            IBone bone = skeleton[Name];
//            if (bone != null)
//                UpdateStateBlended(bone.FrameState, bone.BindState, otherBoneAnim, otherWeight, blendType);
//        }
//        public BoneFrame BlendedWith(float second, BoneFrame other, float otherWeight)
//            => GetFrame(second).BlendedWith(other, otherWeight);

//    }
//}
