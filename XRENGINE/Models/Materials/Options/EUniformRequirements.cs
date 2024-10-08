namespace XREngine.Rendering.Models.Materials
{
    [Flags]
    public enum EUniformRequirements
    {
        None = 0b00000,
        Camera = 0b00001,
        Lights = 0b00010,
        RenderTime = 0b00100,
        ViewportDimensions = 0b01000,
        MousePosition = 0b10000,

        //LightsAndCamera = Lights | Camera,
    }
}
