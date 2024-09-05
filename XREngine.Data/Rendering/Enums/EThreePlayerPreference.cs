namespace XREngine.Data.Rendering
{
    /// <summary>
    /// Viewport layout preference for when only three people are playing.
    /// </summary>
    public enum EThreePlayerPreference
    {
        /// <summary>
        /// Top left, top right, and bottom left quadrants of the screen are used for viewports.
        /// The bottom right is blank (can be drawn in using global hud; for example, a world map)
        /// </summary>
        BlankBottomRight,
        /// <summary>
        /// First player has a wide screen on top (two quadrants), and the remaining two players have smaller screens in the bottom two quadrants.
        /// </summary>
        PreferFirstPlayer,
        /// <summary>
        /// Second player has a wide screen on top (two quadrants), and the remaining two players have smaller screens in the bottom two quadrants.
        /// </summary>
        PreferSecondPlayer,
        /// <summary>
        /// Third player has a wide screen on top (two quadrants), and the remaining two players have smaller screens in the bottom two quadrants.
        /// </summary>
        PreferThirdPlayer,
    }
}