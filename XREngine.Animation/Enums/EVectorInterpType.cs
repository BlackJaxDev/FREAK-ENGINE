namespace XREngine.Animation
{
    public enum EVectorInterpType
    {
        /// <summary>
        /// Jumps to the next value halfway through the keyframe
        /// </summary>
        Step,
        /// <summary>
        /// Point to point interpolation
        /// </summary>
        Linear,
        /// <summary>
        /// Cubic interpolation
        /// </summary>
        Smooth
    }
}
