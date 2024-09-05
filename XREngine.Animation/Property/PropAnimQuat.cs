using Extensions;
using System.ComponentModel;
using System.Numerics;
using XREngine.Data;

namespace XREngine.Animation
{
    public class PropAnimQuaternion : PropAnimKeyframed<QuaternionKeyframe>, IEnumerable<QuaternionKeyframe>
    {
        private DelGetValue<Quaternion> _getValue;

        private Quaternion[]? _baked = null;
        /// <summary>
        /// The default value to return when no keyframes are set.
        /// </summary>
        public Quaternion DefaultValue { get; set; } = Quaternion.Identity;

        public PropAnimQuaternion() : base(0.0f, false) { }
        public PropAnimQuaternion(float lengthInSeconds, bool looped, bool useKeyframes)
            : base(lengthInSeconds, looped, useKeyframes) { }
        public PropAnimQuaternion(int frameCount, float FPS, bool looped, bool useKeyframes) 
            : base(frameCount, FPS, looped, useKeyframes) { }

        protected override void BakedChanged()
            => _getValue = !IsBaked ? GetValueKeyframed : GetValueBaked;

        public Quaternion GetValue(float second)
            => _getValue(second);
        protected override object GetValueGeneric(float second)
            => _getValue(second);
        public Quaternion GetValueBaked(float second)
            => _baked[(int)Math.Floor(second * BakedFramesPerSecond)];
        public Quaternion GetValueBaked(int frameIndex)
            => _baked[frameIndex];
        public Quaternion GetValueKeyframed(float second)
            => Keyframes.Count == 0 ? DefaultValue : Keyframes.First.Interpolate(second);
        
        public override void Bake(float framesPerSecond)
        {
            _bakedFPS = framesPerSecond;
            _bakedFrameCount = (int)Math.Ceiling(LengthInSeconds * framesPerSecond);
            _baked = new Quaternion[BakedFrameCount];
            for (int i = 0; i < BakedFrameCount; ++i)
                _baked[i] = GetValueKeyframed(i);
        }
        
        protected override object GetCurrentValueGeneric()
        {
            throw new NotImplementedException();
        }

        protected override void OnProgressed(float delta)
        {
            throw new NotImplementedException();
        }
    }
    public class QuaternionKeyframe : Keyframe, IRadialKeyframe
    {
        public QuaternionKeyframe() { }
        public QuaternionKeyframe(int frameIndex, float FPS, Quaternion inValue, Quaternion outValue, Quaternion inTangent, Quaternion outTangent, ERadialInterpType type)
            : this(frameIndex / FPS, inValue, outValue, inTangent, outTangent, type) { }
        public QuaternionKeyframe(int frameIndex, float FPS, Quaternion inoutValue, Quaternion inTangent, Quaternion outTangent, ERadialInterpType type)
            : this(frameIndex / FPS, inoutValue, inoutValue, inTangent, outTangent, type) { }
        public QuaternionKeyframe(float second, Quaternion inoutValue, Quaternion inOutTangent, ERadialInterpType type)
            : this(second, inoutValue, inoutValue, inOutTangent, inOutTangent, type) { }
        public QuaternionKeyframe(float second, Quaternion inoutValue, Quaternion inTangent, Quaternion outTangent, ERadialInterpType type)
            : this(second, inoutValue, inoutValue, inTangent, outTangent, type) { }
        public QuaternionKeyframe(float second, Quaternion inValue, Quaternion outValue, Quaternion inTangent, Quaternion outTangent, ERadialInterpType type) : base()
        {
            Second = second;
            InValue = inValue;
            OutValue = outValue;
            InTangent = inTangent;
            OutTangent = outTangent;
            InterpolationType = type;
        }

        private delegate Quaternion DelInterpolate(QuaternionKeyframe key1, QuaternionKeyframe key2, float time);
        private DelInterpolate _interpolate = CubicBezier;
        protected ERadialInterpType _interpolationType;

        [Browsable(false)]
        public override Type ValueType => typeof(Quaternion);

        public Quaternion InValue { get; set; }
        public Quaternion OutValue { get; set; }
        public Quaternion InTangent { get; set; }
        public Quaternion OutTangent { get; set; }

        public new QuaternionKeyframe? Next
        {
            get => _next as QuaternionKeyframe;
            set => _next = value;
        }
        public new QuaternionKeyframe? Prev
        {
            get => _prev as QuaternionKeyframe;
            set => _prev = value;
        }

        public ERadialInterpType InterpolationType
        {
            get => _interpolationType;
            set
            {
                _interpolationType = value;
                switch (_interpolationType)
                {
                    case ERadialInterpType.Step:
                        _interpolate = Step;
                        break;
                    case ERadialInterpType.Linear:
                        _interpolate = Linear;
                        break;
                    case ERadialInterpType.CubicBezier:
                        _interpolate = CubicBezier;
                        break;
                }
            }
        }
        public Quaternion Interpolate(float desiredSecond)
        {
            if (desiredSecond < Second)
            {
                if (_prev == this)
                    return InValue;

                return Prev.Interpolate(desiredSecond);
            }

            if (desiredSecond > _next.Second)
            {
                if (_next == this)
                    return OutValue;

                return Next.Interpolate(desiredSecond);
            }

            float span = _next.Second - Second;
            float diff = desiredSecond - Second;
            float time = diff / span;
            return _interpolate(this, Next, time);
        }
        public static Quaternion Step(QuaternionKeyframe key1, QuaternionKeyframe key2, float time)
            => time < 1.0f ? key1.OutValue : key2.OutValue;
        public static Quaternion Linear(QuaternionKeyframe key1, QuaternionKeyframe key2, float time)
            => Quaternion.Slerp(key1.OutValue, key2.InValue, time);
        public static Quaternion CubicBezier(QuaternionKeyframe key1, QuaternionKeyframe key2, float time)
            => Interp.SCubic(key1.OutValue, key1.OutTangent, key2.InTangent, key2.InValue, time);

        public override string WriteToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", Second, InValue.X, InValue.Y, InValue.Z, InValue.W, OutValue.X, OutValue.Y, OutValue.Z, OutValue.W, InterpolationType);
            //return string.Format("{0} {1} {2} {3} {4} {5}", Second, InValue.WriteToString(), OutValue.WriteToString(), InTangent.WriteToString(), OutTangent.WriteToString(), InterpolationType);
        }

        public override void ReadFromString(string str)
        {
            string[] parts = str.Split(' ');
            Second = float.Parse(parts[0]);
            InValue = new Quaternion(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]));
            OutValue = new Quaternion(float.Parse(parts[5]), float.Parse(parts[6]), float.Parse(parts[7]), float.Parse(parts[8]));
            InTangent = new Quaternion(float.Parse(parts[9]), float.Parse(parts[10]), float.Parse(parts[11]), float.Parse(parts[12]));
            OutTangent = new Quaternion(float.Parse(parts[13]), float.Parse(parts[14]), float.Parse(parts[15]), float.Parse(parts[16]));
            InterpolationType = parts[17].AsEnum<ERadialInterpType>();
        }
    }
}
