namespace XREngine.Data.Animation
{
    /// <summary>
    /// Dictates the current state of an animation.
    /// </summary>
    public enum EAnimationState
    {
        /// <summary>
        /// Stopped means that the animation is not playing and is set to its initial start position.
        /// </summary>
        Stopped,
        /// <summary>
        /// Paused means that the animation is not currently playing
        /// but is stopped at some arbitrary point in the animation, ready to start from that point again.
        /// </summary>
        Paused,
        /// <summary>
        /// Playing means that the animation is currently progressing forward.
        /// </summary>
        Playing,
    }
}
