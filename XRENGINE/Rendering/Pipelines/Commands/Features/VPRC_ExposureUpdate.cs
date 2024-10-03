
namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_ExposureUpdate(ViewportRenderCommandContainer pipeline) : ViewportRenderCommand(pipeline)
    {
        /// <summary>
        /// This is the texture that exposure will be calculated from.
        /// </summary>
        public string HDRSceneTextureName { get; set; } = "HDRSceneTexture";

        /// <summary>
        /// If true, the command will generate mipmaps for the HDR texture.
        /// Set to false if you've already generated mipmaps before this command.
        /// </summary>
        public bool GenerateMipmapsHere { get; set; } = true;

        protected override void Execute()
        {
            ColorGradingSettings? cgs = Pipeline.State.SceneCamera?.PostProcessing?.ColorGrading;
            if (cgs != null && cgs.AutoExposure)
                cgs.Exposure = Engine.Rendering.State.CalculateDotLuminance(Pipeline.GetTexture<XRTexture2D>(HDRSceneTextureName)!, GenerateMipmapsHere);
        }

        public void SetOptions(string hdrSceneTextureName, bool generateMipmapsHere)
        {
            HDRSceneTextureName = hdrSceneTextureName;
            GenerateMipmapsHere = generateMipmapsHere;
        }
    }
}
