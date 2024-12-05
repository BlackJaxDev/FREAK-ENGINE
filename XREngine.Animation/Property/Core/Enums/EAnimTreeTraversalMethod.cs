namespace XREngine.Animation
{
    public enum EAnimTreeTraversalMethod
    {
        /// <summary>
        /// All members are animated at the same time.
        /// </summary>
        Parallel,
        /// <summary>
        /// Members are animated sequentially in order of appearance, parent-down.
        /// Root-Children-Grandchildren-Etc
        /// </summary>
        BreadthFirst,
        /// <summary>
        /// Left-Root-Right
        /// </summary>
        DepthFirstInOrder,
        /// <summary>
        /// Left-Right-Root
        /// </summary>
        DepthFirstPreOrder,
        /// <summary>
        /// Root-Left-Right
        /// </summary>
        DepthFirstPostOrder
    }
}
