namespace XREngine.Data.Rendering
{
    public enum ETexWrapMode
    {
        /// <summary>
        /// Out-of-range image coordinates will return the border color.
        /// Same as ClampToBorder.
        /// </summary>
        Clamp = 10496,
        /// <summary>
        /// Out-of-range image coordinates are remapped back into range.
        /// </summary>
        Repeat = 10497,
        /// <summary>
        ///  Out-of-range image coordinates will return the border color.
        ///  Same as Clamp.
        /// </summary>
        ClampToBorder = 33069,
        /// <summary>
        /// Out-of-range image coordinates are clamped to the extent of the image.
        /// The border color is not sampled.
        /// </summary>
        ClampToEdge = 33071,
        /// <summary>
        /// Out-of-range image coordinates are remapped back into range.
        /// Every repetition is reversed.
        /// </summary>
        MirroredRepeat = 33648
    }
}
