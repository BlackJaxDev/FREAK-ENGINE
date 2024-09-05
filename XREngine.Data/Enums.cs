namespace System
{
    public enum EPlaneIntersection
    {
        Back,
        Front,
        Intersecting,
    }
    [Flags]
    public enum EContainmentFlags
    {
        None = 0,
        /// <summary>
        /// Not intersecting the other shape.
        /// </summary>
        Disjoint = 1,
        /// <summary>
        /// Fully contains the other shape.
        /// </summary>
        Contains = 2,
        /// <summary>
        /// Shapes are intersecting, but not fully contained within each other whatsoever
        /// </summary>
        Intersects = 4,
        /// <summary>
        /// The other shape contains this.
        /// </summary>
        ContainedWithin = 8,
    }
}
