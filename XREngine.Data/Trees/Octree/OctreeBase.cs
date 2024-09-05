using System.Numerics;
using XREngine.Data.Core;

namespace XREngine.Data.Trees
{
    public class OctreeBase : XRBase
    {
        public const float MinimumUnit = 10.0f;
        public const int MaxChildNodeCount = 8;

        /// <summary>
        /// The maximum length of an array that could store every possible octree node.
        /// </summary>
        public static int ArrayLength(Vector3 halfExtents)
        {
            float minExtent = XRMath.Min(halfExtents.X, halfExtents.Y, halfExtents.Z);
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
