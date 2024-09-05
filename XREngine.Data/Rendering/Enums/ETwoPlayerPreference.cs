namespace XREngine.Data.Rendering
{
    /// <summary>
    /// Viewport layout preference for when only two people are playing.
    /// </summary>
    public enum ETwoPlayerPreference
    {
        /// <summary>
        /// 1st player is on the top of the screen, 2nd player is on bottom.
        /// </summary>
        SplitHorizontally,
        /// <summary>
        /// 1st player is on the left side of the screen, 2nd player is on the right side.
        /// </summary>
        SplitVertically,
    }
}