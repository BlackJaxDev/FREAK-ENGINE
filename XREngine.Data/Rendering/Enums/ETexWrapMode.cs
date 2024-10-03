namespace XREngine.Data.Rendering
{
    public enum ETexWrapMode
    {
        /// <summary>
        /// Out-of-range image coordinates are remapped back into range.
        /// </summary>
        Repeat,
        /// <summary>
        ///  Out-of-range image coordinates will return the border color.
        ///  Same as Clamp.
        /// </summary>
        ClampToBorder,
        /// <summary>
        /// Out-of-range image coordinates are clamped to the extent of the image.
        /// The border color is not sampled.
        /// </summary>
        ClampToEdge,
        /// <summary>
        /// Out-of-range image coordinates are remapped back into range.
        /// Every repetition is reversed.
        /// </summary>
        MirroredRepeat
    }
}
