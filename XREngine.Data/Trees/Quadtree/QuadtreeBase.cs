using System.Numerics;

namespace XREngine.Data.Trees
{
    /// <summary>
    /// A 3D space partitioning tree that recursively divides aabbs into 4 smaller axis-aligned rectangles depending on the items they contain.
    /// </summary>
    public class QuadtreeBase
    {
        public const float MinimumUnit = 1.0f;
        public const int MaxChildNodeCount = 4;

        /// <summary>
        /// The maximum length of an array that could store every possible octree node.
        /// </summary>
        public static int ArrayLength(Vector2 halfExtents)
        {
            float minExtent = Math.Min(halfExtents.X, halfExtents.Y);
            int divisions = 0;
            while (minExtent >= MinimumUnit)
            {
                minExtent *= 0.5f;
                ++divisions;
            }
            return (int)Math.Pow(MaxChildNodeCount, divisions);
        }
    }
}
