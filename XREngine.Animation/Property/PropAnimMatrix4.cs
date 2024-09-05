using Extensions;
using System.ComponentModel;
using System.Numerics;

namespace XREngine.Animation
{
    public class PropAnimMatrix4 : PropAnimKeyframed<Matrix4Keyframe>, IEnumerable<Matrix4Keyframe>
    {
        private DelGetValue<Matrix4x4> _getValue;

        private Matrix4x4[]? _baked = null;
        /// <summary>
        /// The default value to return when no keyframes are set.
        /// </summary>
        public Matrix4x4 DefaultValue { get; set; } = Matrix4x4.Identity;

        public PropAnimMatrix4() : base(0.0f, false)
        {
            _getValue = GetValueKeyframed;
        }
        public PropAnimMatrix4(float lengthInSeconds, bool looped, bool useKeyframes)
            : base(lengthInSeconds, looped, useKeyframes)
        {
            _getValue = GetValueKeyframed;
        }
        public PropAnimMatrix4(int frameCount, float FPS, bool looped, bool useKeyframes) 
            : base(frameCount, FPS, looped, useKeyframes)
        {
            _getValue = GetValueKeyframed;
        }

        protected override void BakedChanged()
            => _getValue = !IsBaked ? GetValueKeyframed : GetValueBaked;

        public Matrix4x4 GetValue(float second)
            => _getValue(second);
        protected override object GetValueGeneric(float second)
            => _getValue(second);

        public Matrix4x4 GetValueBaked(float second)
            => GetValueBaked((int)Math.Floor(second * BakedFramesPerSecond));
        public Matrix4x4 GetValueBaked(int frameIndex)
            => _baked?.TryGet(frameIndex) ?? Matrix4x4.Identity;

        public Matrix4x4 GetValueKeyframed(float second)
            => Keyframes.Count == 0 ? DefaultValue : Keyframes.First?.Interpolate(second) ?? DefaultValue;
        
        public override void Bake(float framesPerSecond)
        {
            _bakedFPS = framesPerSecond;
            _bakedFrameCount = (int)Math.Ceiling(LengthInSeconds * framesPerSecond);
            _baked = new Matrix4x4[BakedFrameCount];
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
    public class Matrix4Keyframe : Keyframe, IStepKeyframe
    {
        public Matrix4Keyframe() { }
        public Matrix4Keyframe(float second, Matrix4x4 value) : base()
        {
            Second = second;
            Value = value;
        }

        protected delegate Matrix4x4 DelInterpolate(Matrix4Keyframe key1, Matrix4Keyframe key2, float time);
        
        public Matrix4x4 Value { get; set; }
        [Browsable(false)]
        public override Type ValueType => typeof(Matrix4x4);

        [Browsable(false)]
        public new Matrix4Keyframe Next
        {
            get => _next as Matrix4Keyframe;
            set => _next = value;
        }
        [Browsable(false)]
        public new Matrix4Keyframe Prev
        {
            get => _prev as Matrix4Keyframe;
            set => _prev = value;
        }

        public Matrix4x4 Interpolate(float second)
        {
            if (_prev == this || _next == this)
                return Value;

            if (second < Second && _prev.Second > Second)
                return Prev.Interpolate(second);

            if (second > _next.Second && _next.Second > Second)
                return Next.Interpolate(second);

            float time = (second - Second) / (_next.Second - second);
            return Matrix4x4.Lerp(Value, Next.Value, time);
        }

        public override void ReadFromString(string str)
        {
            int spaceIndex = str.IndexOf(' ');
            Second = float.Parse(str[..spaceIndex]);
            Value = new Matrix4x4();
            Value = ReadMatrixFromString(str[(spaceIndex + 1)..]);
        }

        public override string WriteToString()
            => string.Format("{0} {1}", Second, WriteMatrixToString(Value));

        private string WriteMatrixToString(Matrix4x4 value)
            => $"{value.M11} {value.M12} {value.M13} {value.M14} {value.M21} {value.M22} {value.M23} {value.M24} {value.M31} {value.M32} {value.M33} {value.M34} {value.M41} {value.M42} {value.M43} {value.M44}";
        private Matrix4x4 ReadMatrixFromString(string v)
        {
            string[] values = v.Split(' ');
            return new Matrix4x4(
                float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3]),
                float.Parse(values[4]), float.Parse(values[5]), float.Parse(values[6]), float.Parse(values[7]),
                float.Parse(values[8]), float.Parse(values[9]), float.Parse(values[10]), float.Parse(values[11]),
                float.Parse(values[12]), float.Parse(values[13]), float.Parse(values[14]), float.Parse(values[15])
            );
        }

    }
}
