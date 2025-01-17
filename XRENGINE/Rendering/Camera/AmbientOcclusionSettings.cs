using XREngine.Data;
using XREngine.Data.Core;

namespace XREngine.Rendering
{
    public class AmbientOcclusionSettings : XRBase
    {
        private bool _enabled = true;
        private EType _type = EType.ScreenSpace;
        private float _resolutionScale;
        private float _samplesPerPixel;
        private float _distance;
        private float _distanceIntensity;
        private float _intensity = 1.0f;
        private float _color;
        private float _bias = 0.05f;
        private float _thickness;
        private int _iterations;
        private float _radius = 0.9f;
        private float _power = 1.4f;
        private float _rings;
        private float _lumaPhi;
        private float _depthPhi;
        private float _normalPhi;
        private int _samples;

        public enum EType
        {
            ScreenSpace,
            ScalableAmbientObscurance,
            MultiScaleVolumetricObscurance,
            HorizonBased,
            HorizonBasedPlus,
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        public EType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        /// <summary>
        /// The resolution scale of the ambient occlusion.
        /// </summary>
        public float ResolutionScale
        {
            get => _resolutionScale;
            set => SetField(ref _resolutionScale, value);
        }
        /// <summary>
        /// The samples that are taken per pixel to compute the ambient occlusion.
        /// </summary>
        public float SamplesPerPixel
        {
            get => _samplesPerPixel;
            set => SetField(ref _samplesPerPixel, value);
        }
        /// <summary>
        /// Controls the radius/size of the ambient occlusion in world units.
        /// </summary>
        public float Distance
        {
            get => _distance;
            set => SetField(ref _distance, value);
        }
        /// <summary>
        /// Controls how fast the ambient occlusion fades away with distance in world units.
        /// </summary>
        public float DistanceIntensity
        {
            get => _distanceIntensity;
            set => SetField(ref _distanceIntensity, value);
        }
        /// <summary>
        /// A purely artistic control for the intensity of the AO - runs the ao through the function pow(ao, intensity), which has the effect of darkening areas with more ambient occlusion.
        /// </summary>
        public float Intensity
        {
            get => _intensity;
            set => SetField(ref _intensity, value);
        }
        /// <summary>
        /// The color of the ambient occlusion.
        /// </summary>
        public float Color
        {
            get => _color;
            set => SetField(ref _color, value);
        }
        /// <summary>
        /// The bias that is used for the effect in world units.
        /// </summary>
        public float Bias
        {
            get => _bias;
            set => SetField(ref _bias, value);
        }
        /// <summary>
        /// The thickness if the ambient occlusion effect.
        /// </summary>
        public float Thickness
        {
            get => _thickness;
            set => SetField(ref _thickness, value);
        }
        /// <summary>
        /// The number of iterations of the denoising pass.
        /// </summary>
        public int Iterations
        {
            get => _iterations;
            set => SetField(ref _iterations, value);
        }
        /// <summary>
        /// The radius of the poisson disk.
        /// </summary>
        public float Radius
        {
            get => _radius;
            set => SetField(ref _radius, value);
        }
        public float Power
        {
            get => _power;
            set => SetField(ref _power, value);
        }
        /// <summary>
        /// The rings of the poisson disk.
        /// </summary>
        public float Rings
        {
            get => _rings;
            set => SetField(ref _rings, value);
        }
        /// <summary>
        /// Allows to adjust the influence of the luma difference in the denoising pass.
        /// </summary>
        public float LumaPhi
        {
            get => _lumaPhi;
            set => SetField(ref _lumaPhi, value);
        }
        /// <summary>
        /// Allows to adjust the influence of the depth difference in the denoising pass.
        /// </summary>
        public float DepthPhi
        {
            get => _depthPhi;
            set => SetField(ref _depthPhi, value);
        }
        /// <summary>
        /// Allows to adjust the influence of the normal difference in the denoising pass.
        /// </summary>
        public float NormalPhi
        {
            get => _normalPhi;
            set => SetField(ref _normalPhi, value);
        }
        /// <summary>
        /// The samples that are used in the poisson disk.
        /// </summary>
        public int Samples
        {
            get => _samples;
            set => SetField(ref _samples, value);
        }

        public void Lerp(AmbientOcclusionSettings from, AmbientOcclusionSettings to, float time)
        {
            Radius = Interp.Lerp(from.Radius, to.Radius, time);
            Power = Interp.Lerp(from.Power, to.Power, time);
        }

        public void SetUniforms(XRRenderProgram program)
        {
            switch (Type)
            {
                case EType.ScreenSpace:
                    program.Uniform("Radius", Radius);
                    program.Uniform("Power", Power);
                    break;
                case EType.MultiScaleVolumetricObscurance:
                    program.Uniform("Bias", Bias);
                    program.Uniform("Intensity", Intensity);
                    break;
            }
        }
    }
}