
using Extensions;
using XREngine.Animation;

namespace XREngine.Scene.Components.Animation
{
    public class AnimationCurve
    {
        public static AnimationCurve Linear { get; }
        public static AnimationCurve Smooth { get; }
        public static AnimationCurve Step { get; }
        public static AnimationCurve EaseOut { get; }
        public static AnimationCurve EaseIn { get; }

        static AnimationCurve()
        {
            Linear = new();
            Smooth = new();
            Step = new();
            EaseOut = new();
            EaseIn = new();

            Linear.Keys.Keyframes.Add(
                new FloatKeyframe(0.0f, 0.0f, 0.0f, EVectorInterpType.Linear),
                new FloatKeyframe(1.0f, 1.0f, 0.0f, EVectorInterpType.Linear));
            Smooth.Keys.Keyframes.Add(
                new FloatKeyframe(0.0f, 0.0f, 0.0f, EVectorInterpType.Smooth),
                new FloatKeyframe(1.0f, 1.0f, 0.0f, EVectorInterpType.Smooth));
            Step.Keys.Keyframes.Add(
                new FloatKeyframe(0.0f, 0.0f, 0.0f, EVectorInterpType.Step),
                new FloatKeyframe(1.0f, 1.0f, 0.0f, EVectorInterpType.Step));
            EaseOut.Keys.Keyframes.Add(
                new FloatKeyframe(0.0f, 0.0f, 0.0f, EVectorInterpType.Smooth),
                new FloatKeyframe(1.0f, 1.0f, 0.0f, EVectorInterpType.Smooth));
            EaseIn.Keys.Keyframes.Add(
                new FloatKeyframe(0.0f, 0.0f, 0.0f, EVectorInterpType.Smooth),
                new FloatKeyframe(1.0f, 1.0f, 0.0f, EVectorInterpType.Smooth));
        }

        public PropAnimFloat Keys { get; } = new(1.0f, false, true);

        public float Evaluate(float t)
            => Keys.GetValue(t.Clamp(0.0f, 1.0f));
    }
}