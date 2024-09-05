
using XREngine.Data.Core;

namespace XREngine.Rendering
{
    public class BloomSettings : XRBase
    {
        private float _intensity;
        private float _threshold;
        private float _softKnee;
        private float _radius;

        public BloomSettings()
        {

        }

        public float Intensity
        {
            get => _intensity;
            set => SetField(ref _intensity, value);
        }
        public float Threshold
        {
            get => _threshold;
            set => SetField(ref _threshold, value);
        }
        public float SoftKnee
        {
            get => _softKnee;
            set => SetField(ref _softKnee, value);
        }
        public float Radius
        {
            get => _radius;
            set => SetField(ref _radius, value);
        }

        internal void SetUniforms(XRRenderProgram program)
        {

        }
    }
}