using System.Numerics;

namespace XREngine
{
    public static partial class Engine
    {
public static partial class Rendering
        {
            /// <summary>
            /// Contains global rendering settings.
            /// </summary>
            public static partial class Settings
            {
                public static Vector3 DefaultLuminance = new(0.299f, 0.587f, 0.114f);
                /// <summary>
                /// Shader pipelines allow for dynamic combination of shaders at runtime, such as mixing and matching vertex and fragment shaders.
                /// When this is off, a new shader must be compiled for each unique combination of shaders.
                /// </summary>
                public static bool AllowShaderPipelines { get; set; } = true;
                /// <summary>
                /// When true, the engine will use integers in shaders instead of floats when needed.
                /// </summary>
                public static bool UseIntegerUniformsInShaders { get; set; } = true;
                /// <summary>
                /// When items in the same group also have the same order value, this will dictate whether they are ticked in parallel or sequentially.
                /// Depending on how many items are in a singular tick order, this could be faster or slower.
                /// </summary>
                public static bool TickGroupedItemsInParallel { get; set; } = false;
                public static uint LightProbeDefaultColorResolution { get; set; } = 512u;
                public static bool ShouldLightProbesCaptureDepth { get; set; } = false;
                public static uint LightProbeDefaultDepthResolution { get; set; } = 256u;
            }
        }
    }
}