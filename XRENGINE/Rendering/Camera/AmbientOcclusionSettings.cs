
namespace XREngine.Rendering
{
    public class AmbientOcclusionSettings
    {
        public enum EType
        {
            ScreenSpace,
            ScalableAmbientObscurance,
            HorizonBased,
            HorizonBasedPlus,
        }

        public bool Enabled { get; set; }
        public EType Type { get; set; }

        /// <summary>
        /// The resolution scale of the ambient occlusion.
        /// </summary>
        public float ResolutionScale { get; set; }
        /// <summary>
        /// The samples that are taken per pixel to compute the ambient occlusion.
        /// </summary>
        public float SamplesPerPixel { get; set; }
        /// <summary>
        /// Controls the radius/size of the ambient occlusion in world units.
        /// </summary>
        public float Distance { get; set; }
        /// <summary>
        /// Controls how fast the ambient occlusion fades away with distance in world units.
        /// </summary>
        public float DistanceIntensity { get; set; }
        /// <summary>
        /// A purely artistic control for the intensity of the AO - runs the ao through the function pow(ao, intensity), which has the effect of darkening areas with more ambient occlusion.
        /// </summary>
        public float Intensity { get; set; }
        /// <summary>
        /// The color of the ambient occlusion.
        /// </summary>
        public float Color { get; set; }
        /// <summary>
        /// The bias that is used for the effect in world units.
        /// </summary>
        public float Bias { get; set; }
        /// <summary>
        /// The thickness if the ambient occlusion effect.
        /// </summary>
        public float Thickness { get; set; }
        /// <summary>
        /// The number of iterations of the denoising pass.
        /// </summary>
        public int Iterations { get; set; }
        /// <summary>
        /// The radius of the poisson disk.
        /// </summary>
        public float Radius { get; set; }
        /// <summary>
        /// The rings of the poisson disk.
        /// </summary>
        public float Rings { get; set; }
        /// <summary>
        /// Allows to adjust the influence of the luma difference in the denoising pass.
        /// </summary>
        public float LumaPhi { get; set; }
        /// <summary>
        /// Allows to adjust the influence of the depth difference in the denoising pass.
        /// </summary>
        public float DepthPhi { get; set; }
        /// <summary>
        /// Allows to adjust the influence of the normal difference in the denoising pass.
        /// </summary>
        public float NormalPhi { get; set; }
        /// <summary>
        /// The samples that are used in the poisson disk.
        /// </summary>
        public int Samples { get; set; }

        public AmbientOcclusionSettings()
        {
        }

        internal void SetUniforms(XRRenderProgram program)
        {
            throw new NotImplementedException();
        }
    }
}