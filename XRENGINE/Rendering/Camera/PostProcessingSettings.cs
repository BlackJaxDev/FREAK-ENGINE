namespace XREngine.Rendering
{
    public class PostProcessingSettings
    {
        public PostProcessingSettings()
        {
            Bloom = new BloomSettings();
            DepthOfField = new DepthOfFieldSettings();
            AmbientOcclusion = new AmbientOcclusionSettings();
            MotionBlur = new MotionBlurSettings();
            ColorGrading = new ColorGradingSettings();
            Vignette = new VignetteSettings();
            LensDistortion = new LensDistortionSettings();
            ChromaticAberration = new ChromaticAberrationSettings();
            Grain = new GrainSettings();
            Dithering = new DitheringSettings();
            RayTracing = new RayTracingSettings();
            Shadows = new ShadowSettings();
        }

        public ShadowSettings Shadows { get; set; }
        public BloomSettings Bloom { get; }
        public DepthOfFieldSettings DepthOfField { get; }
        public AmbientOcclusionSettings AmbientOcclusion { get; }
        public MotionBlurSettings MotionBlur { get; }
        public ColorGradingSettings ColorGrading { get; }
        public VignetteSettings Vignette { get; }
        public LensDistortionSettings LensDistortion { get; }
        public ChromaticAberrationSettings ChromaticAberration { get; }
        public GrainSettings Grain { get; }
        public DitheringSettings Dithering { get; }
        public RayTracingSettings RayTracing { get; }

        public void SetUniforms(XRRenderProgram program)
        {

        }
    }
}