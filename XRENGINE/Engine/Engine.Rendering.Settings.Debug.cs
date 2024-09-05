namespace XREngine
{
    public static partial class Engine
    {
        public static partial class Rendering
        {
            public static partial class Settings
            {
                /// <summary>
                /// These options should not be enabled in production builds.
                /// </summary>
                public static class Debug
                {
                    /// <summary>
                    /// If true, the engine will render the octree for the 3D world.
                    /// </summary>
                    public static bool Preview3DWorldOctree { get; set; } = false;
                    /// <summary>
                    /// If true, the engine will render the quadtree for the 2D world.
                    /// </summary>
                    public static bool Preview2DWorldQuadtree { get; set; } = false;
                    /// <summary>
                    /// If true, the engine will render physics traces.
                    /// </summary>
                    public static bool PreviewTraces { get; set; } = false;
                }
            }
        }
    }
}