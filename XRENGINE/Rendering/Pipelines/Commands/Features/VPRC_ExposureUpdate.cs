﻿
namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_ExposureUpdate : ViewportRenderCommand
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
            => Pipeline.State.SceneCamera?.PostProcessing?.ColorGrading?.UpdateExposure(Pipeline.GetTexture<XRTexture2D>(HDRSceneTextureName)!, GenerateMipmapsHere);

        public void SetOptions(string hdrSceneTextureName, bool generateMipmapsHere)
        {
            HDRSceneTextureName = hdrSceneTextureName;
            GenerateMipmapsHere = generateMipmapsHere;
        }
    }
}
