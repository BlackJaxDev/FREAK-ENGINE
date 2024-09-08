﻿namespace XREngine.Animation
{
    /// <summary>
    /// Retrieves an animated value by executing a method with the current animation time.
    /// </summary>
    /// <typeparam name="T">The type of value to animate.</typeparam>
    public class PropAnimMethod<T> : BasePropAnimBakeable
    {
        public delegate T DelGetValue(float second);

        private DelGetValue _tickMethod = null;
        public DelGetValue TickMethod
        {
            get => _tickMethod;
            set
            {
                _tickMethod = value;
                if (!IsBaked)
                    GetValue = _tickMethod;
            }
        }
        public DelGetValue GetValue { get; private set; }
        
        private T[]? _baked = null;
        /// <summary>
        /// The default value to return when the tick method is not set.
        /// </summary>
        public T DefaultValue { get; set; }
        
        public PropAnimMethod() 
            : base(0.0f, false) { }
        public PropAnimMethod(float lengthInSeconds, bool looped)
            : base(lengthInSeconds, looped) { }
        public PropAnimMethod(int frameCount, float FPS, bool looped)
            : base(frameCount, FPS, looped) { }

        public PropAnimMethod(DelGetValue method) 
            : base(0.0f, false) => TickMethod = method;
        public PropAnimMethod(float lengthInSeconds, bool looped, DelGetValue method)
            : base(lengthInSeconds, looped) => TickMethod = method;
        public PropAnimMethod(int frameCount, float FPS, bool looped, DelGetValue method)
            : base(frameCount, FPS, looped) => TickMethod = method;

        public T GetValueMethod(float second)
            => TickMethod != null ? TickMethod(second) : DefaultValue;
        protected override object GetValueGeneric(float second)
            => GetValue(second);
        public T GetValueBaked(float second)
            => _baked[(int)Math.Floor(second * BakedFramesPerSecond)];
        public T GetValueBaked(int frameIndex)
            => _baked[frameIndex];

        protected override void BakedChanged()
            => GetValue = !IsBaked ? (DelGetValue)GetValueMethod : GetValueBaked;

        public override void Bake(float framesPerSecond)
        {
            _bakedFPS = framesPerSecond;
            _bakedFrameCount = (int)Math.Ceiling(LengthInSeconds * framesPerSecond);
            _baked = new T[BakedFrameCount];
            for (int i = 0; i < BakedFrameCount; ++i)
                _baked[i] = TickMethod(i);
        }

        protected override object GetCurrentValueGeneric() => GetValue(CurrentTime);
        protected override void OnProgressed(float delta) { }
    }
}