//using XREngine.Data.Core;

//namespace XREngine.Animation
//{
//    public class SkeletalAnimationPose : XRBase
//    {
//        public Dictionary<string, BoneFrame> BoneFrames { get; set; } = [];

//        public void AddBoneFrame(BoneFrame anim)
//        {
//            if (BoneFrames.ContainsKey(anim._name))
//                BoneFrames[anim._name] = anim;
//            else
//                BoneFrames.Add(anim._name, anim);
//        }
//        public void RemoveBoneFrame(string boneName)
//        {
//            if (BoneFrames.ContainsKey(boneName))
//                BoneFrames.Remove(boneName);
//        }
//        public void UpdateSkeleton(Skeleton skeleton)
//        {
//            foreach (BoneFrame b in BoneFrames.Values)
//                b.UpdateSkeleton(skeleton);
//        }
//        /// <summary>
//        /// Returns all bone names that exist in this and the other.
//        /// </summary>
//        public IEnumerable<string> BoneNamesUnion(SkeletalAnimationPose other)
//        {
//            string[] theseNames = new string[BoneFrames.Keys.Count];
//            BoneFrames.Keys.CopyTo(theseNames, 0);
//            string[] thoseNames = new string[other.BoneFrames.Keys.Count];
//            other.BoneFrames.Keys.CopyTo(thoseNames, 0);
//            return theseNames.Intersect(thoseNames);
//        }
//        /// <summary>
//        /// Returns all bone names that exist in this and the other.
//        /// </summary>
//        public IEnumerable<string> BoneNamesUnion(SkeletalAnimation other)
//        {
//            string[] theseNames = new string[BoneFrames.Keys.Count];
//            BoneFrames.Keys.CopyTo(theseNames, 0);
//            string[] thoseNames = new string[other.BoneAnimations.Keys.Count];
//            other.BoneAnimations.Keys.CopyTo(thoseNames, 0);
//            return theseNames.Intersect(thoseNames);
//        }
//        public SkeletalAnimationPose BlendedWith(SkeletalAnimationPose other, float otherWeight)
//        {
//            SkeletalAnimationPose blendedFrame = new SkeletalAnimationPose();
//            var union = BoneNamesUnion(other);
//            foreach (string name in union)
//            {
//                if (BoneFrames.ContainsKey(name))
//                {
//                    if (other.BoneFrames.ContainsKey(name))
//                        blendedFrame.AddBoneFrame(BoneFrames[name].BlendedWith(other.BoneFrames[name], otherWeight));
//                    else
//                        blendedFrame.AddBoneFrame(BoneFrames[name].BlendedWith(null, otherWeight));
//                }
//                else
//                {
//                    if (other.BoneFrames.ContainsKey(name))
//                        blendedFrame.AddBoneFrame(other.BoneFrames[name].BlendedWith(null, 1.0f - otherWeight));
//                }
//            }
//            return blendedFrame;
//        }
//        public SkeletalAnimationPose BlendedWith(SkeletalAnimation other, float frameIndex, float otherWeight)
//        {
//            SkeletalAnimationPose blendedFrame = new SkeletalAnimationPose();
//            foreach (string name in BoneNamesUnion(other))
//            {
//                if (BoneFrames.ContainsKey(name))
//                {
//                    if (other.BoneAnimations.ContainsKey(name))
//                        blendedFrame.AddBoneFrame(BoneFrames[name].BlendedWith(other.BoneAnimations[name], frameIndex, otherWeight));
//                    else
//                        blendedFrame.AddBoneFrame(BoneFrames[name].BlendedWith(null, otherWeight));
//                }
//                else
//                {
//                    if (other.BoneAnimations.ContainsKey(name))
//                        blendedFrame.AddBoneFrame(other.BoneAnimations[name].BlendedWith(frameIndex, null, 1.0f - otherWeight));
//                }
//            }
//            return blendedFrame;
//        }

//        public void BlendWith(SkeletalAnimationPose other, float otherWeight)
//        {
//            if (other is null)
//                return;
//            foreach (string name in BoneNamesUnion(other))
//            {
//                if (BoneFrames.ContainsKey(name))
//                {
//                    if (other.BoneFrames.ContainsKey(name))
//                        BoneFrames[name].BlendWith(other.BoneFrames[name], otherWeight);
//                    else
//                        BoneFrames[name].BlendWith(null, otherWeight);
//                }
//                else
//                {
//                    if (other.BoneFrames.ContainsKey(name))
//                        AddBoneFrame(other.BoneFrames[name].BlendedWith(null, 1.0f - otherWeight));

//                    //else, neither has a bone with this name, ignore it
//                }
//            }
//        }
//        public void BlendWith(SkeletalAnimation other, float frameIndex, float otherWeight)
//        {
//            foreach (string name in BoneNamesUnion(other))
//            {
//                if (BoneFrames.ContainsKey(name))
//                {
//                    if (other.BoneAnimations.ContainsKey(name))
//                        BoneFrames[name].BlendedWith(other.BoneAnimations[name], frameIndex, otherWeight);
//                    else
//                        BoneFrames[name].BlendedWith(null, otherWeight);
//                }
//                else
//                {
//                    if (other.BoneAnimations.ContainsKey(name))
//                        other.BoneAnimations[name].BlendedWith(frameIndex, null, 1.0f - otherWeight);
//                }
//            }
//        }
//    }
//    public class FrameVector3ValueWeight
//    {
//        public FrameVector3ValueWeight() { }
//        public FrameVector3ValueWeight(Vector3 value, float weight)
//        {
//            Value = value;
//            Weight = weight;
//        }

//        public Vector3 Value { get; set; }
//        public float Weight { get; set; }
//    }
//    public class FrameQuatValueWeight
//    {
//        public FrameQuatValueWeight() { }
//        public FrameQuatValueWeight(Quat value, float weight)
//        {
//            Value = value;
//            Weight = weight;
//        }

//        public Quat Value { get; set; }
//        public float Weight { get; set; }
//    }
//    public class BoneFrame
//    {
//        public string _name;

//        public FrameVector3ValueWeight Translation { get; set; }
//        public FrameVector3ValueWeight Scale { get; set; }
//        public FrameQuatValueWeight Rotation { get; set; }

//        public Vector3 GetTranslation(Vector3 bindTranslation)
//            => Vector3.Lerp(bindTranslation, Translation.Value, Translation.Weight);
//        public Quat GetRotation(Quat bindRotation)
//            => Quat.Slerp(bindRotation, Rotation.Value, Rotation.Weight);
//        public Vector3 GetScale(Vector3 bindScale)
//            => Vector3.Lerp(bindScale, Scale.Value, Scale.Weight);

//        public Vector3 GetUnweightedTranslation()
//            => Translation.Value;
//        public Quat GetUnweightedRotation()
//            => Rotation.Value;
//        public Vector3 GetUnweightedScale()
//            => Scale.Value;

//        public BoneFrame(string name) => _name = name;
//        public BoneFrame(string name, Vector3 translation, Quat rotation, Vector3 scale) : this(name)
//        {
//            Translation = new FrameVector3ValueWeight(translation, 1.0f);
//            Rotation = new FrameQuatValueWeight(rotation, 1.0f);
//            Scale = new FrameVector3ValueWeight(scale, 1.0f);
//        }
//        public BoneFrame(string name, (Vector3 translation, Quat rotation, Vector3 scale) parts)
//            : this(name, parts.translation, parts.rotation, parts.scale) { }
//        public BoneFrame(string name, TransformKeyCollection keys) 
//            : this(name, keys.GetTransformParts()) { }
//        public BoneFrame(string name, TransformKeyCollection keys, float second) 
//            : this(name, keys.GetTransformParts(second)) { }

//        //public BoneFrame(string name, Vector3 translation, Rotator rotation, Vector3 scale, params float[] weights)
//        //{
//        //    _name = name;

//        //    _values = new FrameValueWeight[9];

//        //    _values[0].Value = translation.X;
//        //    _values[0].Weight = weights[0];
//        //    _values[1].Value = translation.Y;
//        //    _values[1].Weight = weights[1];
//        //    _values[2].Value = translation.Z;
//        //    _values[2].Weight = weights[2];

//        //    _values[3].Value = rotation.Pitch;
//        //    _values[3].Weight = weights[3];
//        //    _values[4].Value = rotation.Yaw;
//        //    _values[4].Weight = weights[4];
//        //    _values[5].Value = rotation.Roll;
//        //    _values[5].Weight = weights[5];

//        //    _values[6].Value = scale.X;
//        //    _values[6].Weight = weights[6];
//        //    _values[7].Value = scale.Y;
//        //    _values[7].Weight = weights[7];
//        //    _values[8].Value = scale.Z;
//        //    _values[8].Weight = weights[8];
//        //}

//        public void UpdateSkeleton(ISkeleton skeleton)
//        {
//            IBone bone = skeleton[_name];
//            if (bone != null)
//                UpdateState(bone.FrameState, bone.BindState);
//        }
//        public void UpdateState(ITransform frameState, ITransform bindState)
//        {
//            Vector3 t = GetTranslation(bindState.Translation.Value);
//            Quat r = GetRotation(bindState.Rotation.Value);
//            Vector3 s = GetScale(bindState.Scale.Value);
//            frameState.SetAll(t, r, s);
//        }
//        public void UpdateSkeletonBlended(ISkeleton skeleton, BoneFrame otherBoneFrame, float otherWeight)
//        {
//            IBone bone = skeleton[_name];
//            if (bone != null)
//                UpdateStateBlended(bone.FrameState, bone.BindState, otherBoneFrame, otherWeight);
//        }
//        public void UpdateStateBlended(
//            ITransform frameState,
//            ITransform bindState,
//            BoneFrame otherBoneFrame,
//            float otherWeight)
//        {
//            Vector3 t;
//            Vector3 s;
//            Quat r;

//            if (otherBoneFrame is null)
//            {
//                otherWeight = 1.0f - otherWeight;

//                Translation.Weight *= otherWeight;
//                Rotation.Weight *= otherWeight;
//                Scale.Weight *= otherWeight;

//                t = GetTranslation(bindState.Translation.Value);
//                r = GetRotation(bindState.Rotation.Value);
//                s = GetScale(bindState.Scale);

//                frameState.SetAll(t, r, s);
//            }
//            else
//            {
//                Vector3 t1 = GetTranslation(bindState.Translation.Value);
//                Vector3 t2 = otherBoneFrame.GetTranslation(bindState.Translation.Value);
//                t = Vector3.Lerp(t1, t2, otherWeight);

//                Quat r1 = GetRotation(bindState.Rotation.Value);
//                Quat r2 = otherBoneFrame.GetRotation(bindState.Rotation.Value);
//                r = Quat.Slerp(r1, r2, otherWeight);

//                Vector3 s1 = GetScale(bindState.Scale);
//                Vector3 s2 = otherBoneFrame.GetScale(bindState.Scale);
//                s = Vector3.Lerp(s1, s2, otherWeight);

//                frameState.SetAll(t, r, s);
//            }
//        }
//        public BoneFrame BlendedWith(BoneFrame otherBoneFrame, float otherWeight)
//        {
//            BoneFrame frame = new BoneFrame(_name);
//            frame.BlendWith(otherBoneFrame, otherWeight);
//            return frame;
//        }

//        public BoneFrame BlendedWith(BoneAnimation other, float frameIndex, float otherWeight)
//            => BlendedWith(other.GetFrame(frameIndex), otherWeight);

//        public void BlendWith(BoneFrame otherBoneFrame, float otherWeight)
//        {
//            Translation.Value = Interp.Lerp(Translation.Value, otherBoneFrame.Translation.Value, otherWeight);
//            Translation.Weight = Interp.Lerp(Translation.Weight, otherBoneFrame.Translation.Weight, otherWeight);

//            Rotation.Value = Quat.Slerp(Rotation.Value, otherBoneFrame.Rotation.Value, otherWeight);
//            Rotation.Weight = Interp.Lerp(Rotation.Weight, otherBoneFrame.Rotation.Weight, otherWeight);

//            Scale.Value = Interp.Lerp(Scale.Value, otherBoneFrame.Scale.Value, otherWeight);
//            Scale.Weight = Interp.Lerp(Scale.Weight, otherBoneFrame.Scale.Weight, otherWeight);
//        }
//        public void BlendWith(BoneAnimation other, float frameIndex, float otherWeight)
//        {
//            BlendWith(other.GetFrame(frameIndex), otherWeight);
//        }
//    }
//}
