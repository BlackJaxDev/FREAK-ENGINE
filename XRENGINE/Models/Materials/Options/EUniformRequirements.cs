namespace XREngine.Rendering.Models.Materials
{
    [Flags]
    public enum EUniformRequirements
    {
        None = 0b00000,
        /// <summary>
        /// InverseViewMatrix, ProjMatrix, CameraPosition, CameraForward, CameraUp, CameraNearZ, CameraFarZ, ScreenWidth, ScreenHeight, and ScreenOrigin will be provided.
        /// If the camera is perspective, the CameraFovY, CameraFovX, and CameraAspect will also be provided.
        /// Additional custom uniforms can also be provided by whatever parameters class the camera is using.
        /// </summary>
        Camera = 0b00001,
        /// <summary>
        /// Arrays of point lights, spot lights, and directional lights will be provided.
        /// </summary>
        Lights = 0b00010,
        /// <summary>
        /// However many seconds the shader program has been running will be provided.
        /// </summary>
        RenderTime = 0b00100,
        /// <summary>
        /// ScreenWidth, ScreenHeight, and ScreenOrigin will be provided.
        /// </summary>
        ViewportDimensions = 0b01000,
        /// <summary>
        /// The current mouse position relative to the viewport will be provided.
        /// Origin is bottom left.
        /// </summary>
        MousePosition = 0b10000,

        //LightsAndCamera = Lights | Camera,
    }
}
