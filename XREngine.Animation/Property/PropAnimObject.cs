using Extensions;

namespace XREngine.Animation
{
    public class PropAnimObject : PropAnimKeyframed<ObjectKeyframe>, IEnumerable<ObjectKeyframe>
    {
        private DelGetValue<object?> _getValue;

        private object?[]? _baked = null;
        /// <summary>
        /// The default value to return when no keyframes are set.
        /// </summary>
        public object? DefaultValue { get; set; } = null;

        public PropAnimObject() : base(0.0f, false)
        {
            _getValue = GetValueKeyframed;
        }
        public PropAnimObject(float lengthInSeconds, bool looped, bool useKeyframes)
            : base(lengthInSeconds, looped, useKeyframes)
        {
            _getValue = GetValueKeyframed;
        }
        public PropAnimObject(int frameCount, float FPS, bool looped, bool useKeyframes) 
            : base(frameCount, FPS, looped, useKeyframes)
        {
            _getValue = GetValueKeyframed;
        }

        protected override void BakedChanged()
            => _getValue = !IsBaked ? GetValueKeyframed : GetValueBaked;

        public object? GetValue(float second)
            => _getValue(second);
        protected override object? GetValueGeneric(float second)
            => _getValue(second);
        public object? GetValueBaked(float second)
            => GetValueBaked((int)Math.Floor(second * BakedFramesPerSecond));
        public object? GetValueBaked(int frameIndex)
            => _baked?.TryGet(frameIndex) ?? string.Empty;
        public object? GetValueKeyframed(float second)
        {
            ObjectKeyframe? key = Keyframes?.GetKeyBefore(second);
            if (key != null)
                return key.Value;
            return DefaultValue;
        }
        
        public override void Bake(float framesPerSecond)
        {
            _bakedFPS = framesPerSecond;
            _bakedFrameCount = (int)Math.Ceiling(LengthInSeconds * framesPerSecond);
            _baked = new string[BakedFrameCount];
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
    public class ObjectKeyframe : Keyframe, IStepKeyframe
    {
        public object? Value { get; set; }
        public override Type ValueType => typeof(object);
        public new ObjectKeyframe? Next
        {
            get => _next as ObjectKeyframe;
            set => _next = value;
        }
        public new ObjectKeyframe? Prev
        {
            get => _prev as ObjectKeyframe;
            set => _prev = value;
        }

        public override void ReadFromString(string str)
        {
            int spaceIndex = str.IndexOf(' ');
            Second = float.Parse(str[..spaceIndex]);
            Value = str[(spaceIndex + 1)..];
        }
        public override string WriteToString() => string.Format("{0} {1}", Second, Value);
    }
}
