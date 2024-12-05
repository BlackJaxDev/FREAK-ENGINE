using Extensions;
using System.ComponentModel;
using System.Numerics;
using XREngine.Data;
using YamlDotNet.Core.Tokens;

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

        public PropAnimQuaternion() : base(0.0f, false)
        {
            _getValue = !IsBaked ? GetValueKeyframed : GetValueBakedBySecond;
        }
        public PropAnimQuaternion(float lengthInSeconds, bool looped, bool useKeyframes)
            : base(lengthInSeconds, looped, useKeyframes)
        {
            _getValue = !IsBaked ? GetValueKeyframed : GetValueBakedBySecond;
        }
        public PropAnimQuaternion(int frameCount, float FPS, bool looped, bool useKeyframes) 
            : base(frameCount, FPS, looped, useKeyframes)
        {
            _getValue = !IsBaked ? GetValueKeyframed : GetValueBakedBySecond;
        }

        public event Action<PropAnimQuaternion>? ConstrainKeyframedFPSChanged;
        public event Action<PropAnimQuaternion>? LerpConstrainedFPSChanged;

        protected override void BakedChanged()
            => _getValue = !IsBaked ? GetValueKeyframed : GetValueBakedBySecond;

        protected void OnConstrainKeyframedFPSChanged()
            => ConstrainKeyframedFPSChanged?.Invoke(this);

        protected void OnLerpConstrainedFPSChanged()
            => LerpConstrainedFPSChanged?.Invoke(this);

        private bool _constrainKeyframedFPS = false;
        public bool ConstrainKeyframedFPS
        {
            get => _constrainKeyframedFPS;
            set
            {
                _constrainKeyframedFPS = value;
                OnConstrainKeyframedFPSChanged();
            }
        }
        private bool _lerpConstrainedFPS = false;
        [DisplayName("Lerp Constrained FPS")]
        /// <summary>
        /// If true and the animation is baked or ConstrainKeyframedFPS is true, 
        /// lerps between two frames if the second lies between them.
        /// This essentially fakes a higher frames per second for data at a lower resolution.
        /// </summary>
        [Description(
            "If true and the animation is baked or ConstrainKeyframedFPS is true, " +
            "lerps between two frames if the second lies between them. " +
            "This essentially fakes a higher frames per second for data at a lower resolution.")]
        public bool LerpConstrainedFPS
        {
            get => _lerpConstrainedFPS;
            set
            {
                SetField(ref _lerpConstrainedFPS, value);
                OnLerpConstrainedFPSChanged();
            }
        }

        public Quaternion GetValue(float second)
            => _getValue(second);
        protected override object GetValueGeneric(float second)
            => _getValue(second);
        public Quaternion GetValueBakedBySecond(float second)
        {
            if (_baked is null)
                throw new InvalidOperationException("Cannot get baked value when not baked.");

            float frameTime = second.RemapToRange(0, LengthInSeconds) * BakedFramesPerSecond;
            int frame = (int)frameTime;

            if (LerpConstrainedFPS)
            {
                if (frame == _baked.Length - 1)
                {
                    if (Looped && frame != 0)
                    {
                        Quaternion t1 = _baked[frame];
                        Quaternion t2 = _baked[0];

                        //TODO: interpolate values by creating tangents dynamically?

                        //Span is always 1 frame, so no need to divide to normalize
                        float lerpTime = frameTime - frame;

                        return Quaternion.Slerp(t1, t2, lerpTime);
                    }
                    return _baked[frame];
                }
                else
                {
                    Quaternion t1 = _baked[frame];
                    Quaternion t2 = _baked[frame + 1];

                    //TODO: interpolate values by creating tangents dynamically?

                    //Span is always 1 frame, so no need to divide to normalize
                    float lerpTime = frameTime - frame;

                    return Quaternion.Slerp(t1, t2, lerpTime);
                }
            }
            else if (_baked.IndexInRangeArrayT(frame))
                return _baked[frame];
            else
                return DefaultValue;
        }
        public Quaternion GetValueBakedByFrame(int frame)
        {
            if (_baked is null)
                throw new InvalidOperationException("Cannot get baked value when not baked.");

            if (!_baked.IndexInRangeArrayT(frame))
                return Quaternion.Identity;

            return _baked[frame.Clamp(0, _baked.Length - 1)];
        }
        public Quaternion GetValueKeyframed(float second)
        {
            if (Keyframes.Count == 0)
                return DefaultValue;

            if (ConstrainKeyframedFPS)
            {
                int frame = (int)(second * _bakedFPS);
                float floorSec = _bakedFPS != 0.0f ? (frame / _bakedFPS) : 0.0f;
                float ceilSec = _bakedFPS != 0.0f ? ((frame + 1) / _bakedFPS) : 0.0f;
                float time = second - floorSec;

                if (LerpConstrainedFPS)
                    return LerpKeyedValues(floorSec, ceilSec, time);

                second = floorSec;
            }

            return Keyframes.First?.Interpolate(second) ?? DefaultValue;
        }
        private Quaternion LerpKeyedValues(float floorSec, float ceilSec, float time)
        {
            QuaternionKeyframe? prevKey = null;

            Quaternion? floorValue = _keyframes.First?.Interpolate(
                floorSec,
                out prevKey,
                out _,
                out _);

            Quaternion? ceilValue = prevKey?.Interpolate(ceilSec);

            if (floorValue is null || ceilValue is null)
                return DefaultValue;

            return Quaternion.Slerp(floorValue.Value, ceilValue.Value, time);
        }
        public override void Bake(float framesPerSecond)
        {
            _bakedFPS = framesPerSecond;
            _bakedFrameCount = (int)Math.Ceiling(LengthInSeconds * framesPerSecond);
            _baked = new Quaternion[BakedFrameCount];
            for (int i = 0; i < BakedFrameCount; ++i)
                _baked[i] = GetValueKeyframed(i);
        }

        protected override object GetCurrentValueGeneric()
            => GetValue(CurrentTime);

        public event Action<PropAnimQuaternion>? CurrentValueChanged;

        private Quaternion _currentValue = Quaternion.Identity;
        /// <summary>
        /// The value at the current time.
        /// </summary>
        public Quaternion CurrentValue
        {
            get => _currentValue;
            private set
            {
                _currentValue = value;
                CurrentValueChanged?.Invoke(this);
            }
        }

        private QuaternionKeyframe? _prevKeyframe;
        protected override void OnProgressed(float delta)
        {
            //TODO: assign separate functions to be called by OnProgressed to avoid if statements and returns

            if (IsBaked)
            {
                CurrentValue = GetValueBakedBySecond(_currentTime);
                return;
            }

            _prevKeyframe ??= Keyframes.GetKeyBefore(_currentTime);

            if (Keyframes.Count == 0)
            {
                CurrentValue = DefaultValue;
                return;
            }

            float second = _currentTime;
            if (ConstrainKeyframedFPS)
            {
                int frame = (int)(second * _bakedFPS);
                float floorSec = frame / _bakedFPS;
                float ceilSec = (frame + 1) / _bakedFPS;

                //second - floorSec is the resulting delta from one frame to the next.
                //we want the delta to be between two frames with a specified number of frames in between, 
                //so we multiply by the FPS.
                float time = (second - floorSec) * _bakedFPS;

                if (LerpConstrainedFPS)
                {
                    var floorPosition = _prevKeyframe?.Interpolate(
                        floorSec,
                        out _prevKeyframe,
                        out _,
                        out _) ?? Quaternion.Identity;

                    var ceilPosition = _prevKeyframe?.Interpolate(
                        ceilSec,
                        out _,
                        out _,
                        out _) ?? Quaternion.Identity;

                    CurrentValue = Quaternion.Slerp(floorPosition, ceilPosition, time);
                    return;
                }
                second = floorSec;
            }

            CurrentValue = _prevKeyframe?.Interpolate(second,
                out _prevKeyframe,
                out _,
                out _) ?? DefaultValue;
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
            InterpolationTypeOut = type;
        }

        private delegate Quaternion DelInterpolate(QuaternionKeyframe? key1, QuaternionKeyframe? key2, float time);
        private DelInterpolate _interpolateOut = CubicBezier;
        private DelInterpolate _interpolateIn = CubicBezier;
        protected ERadialInterpType _interpolationTypeOut;
        protected ERadialInterpType _interpolationTypeIn;
        private Quaternion _inValue = Quaternion.Identity;
        private Quaternion _outValue = Quaternion.Identity;
        private Quaternion _inTangent = Quaternion.Identity;
        private Quaternion _outTangent = Quaternion.Identity;

        [Browsable(false)]
        public override Type ValueType => typeof(Quaternion);

        public Quaternion InValue
        {
            get => _inValue;
            set => SetField(ref _inValue, value);
        }
        public Quaternion OutValue
        {
            get => _outValue;
            set => SetField(ref _outValue, value);
        }
        public Quaternion InTangent
        {
            get => _inTangent;
            set => SetField(ref _inTangent, value);
        }
        public Quaternion OutTangent
        {
            get => _outTangent;
            set => SetField(ref _outTangent, value);
        }

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

        public float TrackLength => OwningTrack?.LengthInSeconds ?? 0.0f;

        public ERadialInterpType InterpolationTypeIn
        {
            get => _interpolationTypeIn;
            set => SetField(ref _interpolationTypeIn, value);
        }
        public ERadialInterpType InterpolationTypeOut
        {
            get => _interpolationTypeOut;
            set => SetField(ref _interpolationTypeOut, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(InValue):
                case nameof(OutValue):
                case nameof(InTangent):
                case nameof(OutTangent):
                    OwningTrack?.OnChanged();
                    break;
                case nameof(InterpolationTypeIn):
                    switch (_interpolationTypeIn)
                    {
                        case ERadialInterpType.Step:
                            _interpolateIn = Step;
                            break;
                        case ERadialInterpType.Linear:
                            _interpolateIn = Linear;
                            break;
                        case ERadialInterpType.CubicBezier:
                            _interpolateIn = CubicBezier;
                            break;
                    }
                    break;
                case nameof(InterpolationTypeOut):

                    switch (_interpolationTypeOut)
                    {
                        case ERadialInterpType.Step:
                            _interpolateOut = Step;
                            break;
                        case ERadialInterpType.Linear:
                            _interpolateOut = Linear;
                            break;
                        case ERadialInterpType.CubicBezier:
                            _interpolateOut = CubicBezier;
                            break;
                    }
                    break;
            }
        }

        public Quaternion Interpolate(float desiredSecond)
            => Interpolate(desiredSecond, out _, out _, out _);

        public Quaternion Interpolate(
            float desiredSecond,
            out QuaternionKeyframe? prevKey,
            out QuaternionKeyframe? nextKey,
            out float normalizedTime)
        {
            prevKey = this;
            nextKey = Next;

            float span, diff;
            QuaternionKeyframe? key1, key2;

            if (desiredSecond >= Second)
            {
                if (IsLast || Next!.Second > TrackLength)
                {
                    if (OwningTrack?.FirstKey != this)
                    {
                        QuaternionKeyframe? first = OwningTrack?.FirstKey as QuaternionKeyframe;
                        span = TrackLength - Second + (first?.Second ?? 0.0f);
                        diff = desiredSecond - Second;
                        key1 = this;
                        key2 = first;
                    }
                    else
                    {
                        normalizedTime = 0.0f;
                        return OutValue;
                    }
                }
                else if (desiredSecond < Next.Second)
                {
                    //Within two keyframes, interpolate regularly
                    span = (_next?.Second ?? 0.0f) - Second;
                    diff = desiredSecond - Second;
                    key1 = this;
                    key2 = Next;
                }
                else
                {
                    return Next.Interpolate(desiredSecond, out prevKey, out nextKey, out normalizedTime);
                }
            }
            else //desiredSecond < Second
            {
                if (!IsFirst)
                    return Prev!.Interpolate(desiredSecond, out prevKey, out nextKey, out normalizedTime);

                QuaternionKeyframe? last = OwningTrack?.GetKeyBeforeGeneric(TrackLength) as QuaternionKeyframe;

                if (last != this && last != null)
                {
                    span = TrackLength - last.Second + Second;
                    diff = TrackLength - last.Second + desiredSecond;
                    key1 = last;
                    key2 = this;
                }
                else
                {
                    normalizedTime = 0.0f;
                    return InValue;
                }
            }

            normalizedTime = diff / span;

            if (key2 is null)
                return key1.OutValue;

            if (key1.InterpolationTypeOut == key2.InterpolationTypeIn)
                return _interpolateOut(key1, key2, normalizedTime);

            var outInterp = key1._interpolateOut(key1, key2, normalizedTime);
            var inInterp = key2._interpolateIn(key1, key2, normalizedTime);
            return Quaternion.Slerp(outInterp, inInterp, normalizedTime);
        }

        public static Quaternion Step(QuaternionKeyframe? key1, QuaternionKeyframe? key2, float time)
            => time < 0.5f
            ? (key1?.OutValue ?? key2?.InValue ?? Quaternion.Identity)
            : (key2?.InValue ?? key1?.OutValue ?? Quaternion.Identity);

        public static Quaternion Linear(QuaternionKeyframe? key1, QuaternionKeyframe? key2, float time)
            => Quaternion.Slerp(
                key1?.OutValue ?? Quaternion.Identity,
                key2?.InValue ?? Quaternion.Identity,
                time);
        
        public static Quaternion CubicBezier(QuaternionKeyframe? key1, QuaternionKeyframe? key2, float time)
            => Interp.SCubic(
                key1?.OutValue ?? Quaternion.Identity,
                key1?.OutTangent ?? Quaternion.Identity,
                key2?.InTangent ?? Quaternion.Identity,
                key2?.InValue ?? Quaternion.Identity,
                time);

        public override string WriteToString()
            => string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}", Second, InValue.X, InValue.Y, InValue.Z, InValue.W, OutValue.X, OutValue.Y, OutValue.Z, OutValue.W, InterpolationTypeIn, InterpolationTypeOut);

        public override void ReadFromString(string str)
        {
            string[] parts = str.Split(' ');
            Second = float.Parse(parts[0]);
            InValue = new Quaternion(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]));
            OutValue = new Quaternion(float.Parse(parts[5]), float.Parse(parts[6]), float.Parse(parts[7]), float.Parse(parts[8]));
            InTangent = new Quaternion(float.Parse(parts[9]), float.Parse(parts[10]), float.Parse(parts[11]), float.Parse(parts[12]));
            OutTangent = new Quaternion(float.Parse(parts[13]), float.Parse(parts[14]), float.Parse(parts[15]), float.Parse(parts[16]));
            InterpolationTypeIn = parts[17].AsEnum<ERadialInterpType>();
            InterpolationTypeOut = parts[18].AsEnum<ERadialInterpType>();
        }
    }
}
