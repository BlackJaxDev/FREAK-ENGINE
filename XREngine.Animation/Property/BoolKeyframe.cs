﻿namespace XREngine.Animation
{
    public class BoolKeyframe : Keyframe, IStepKeyframe
    {
        public BoolKeyframe() { }
        public BoolKeyframe(int frameIndex, float FPS, bool value)
            : this(frameIndex / FPS, value) { }
        public BoolKeyframe(float second, bool value) : base()
        {
            Second = second;
            Value = value;
        }
        
        public bool Value { get; set; }
        public override Type ValueType => typeof(bool);

        public new BoolKeyframe? Next
        {
            get => _next as BoolKeyframe;
            set => _next = value;
        }
        public new BoolKeyframe? Prev
        {
            get => _prev as BoolKeyframe;
            set => _prev = value;
        }

        public override void ReadFromString(string str)
        {
            int spaceIndex = str.IndexOf(' ');
            Second = float.Parse(str.Substring(0, spaceIndex));
            Value = bool.Parse(str.Substring(spaceIndex + 1));
        }
        public override string WriteToString()
        {
            return string.Format("{0} {1}", Second, Value);
        }
    }
}
