using System.Numerics;
using XREngine.Core.Files;

namespace XREngine
{
    public static partial class Engine
    {
        public static partial class Rendering
        {
            public static event Action? SettingsChanged;

            private static EngineSettings _settings = new();
            /// <summary>
            /// The global rendering settings for the engine.
            /// </summary>
            public static EngineSettings Settings
            {
                get => _settings;
                set
                {
                    if (_settings == value)
                        return;

                    _settings = value;
                    SettingsChanged?.Invoke();
                }
            }

            /// <summary>
            /// Contains global rendering settings.
            /// </summary>
            public partial class EngineSettings : XRAsset
            {
                private Vector3 _defaultLuminance = new(0.299f, 0.587f, 0.114f);
                private bool _allowShaderPipelines = true;
                private bool _useIntegerUniformsInShaders = true;
                private bool _optimizeTo4Weights = false;
                private bool _optimizeWeightsIfPossible = true;
                private bool _tickGroupedItemsInParallel = false;
                private uint lightProbeColorResolution = 512u;
                private bool _lightProbesCaptureDepth = false;
                private uint _lightProbeDepthResolution = 256u;
                private bool _allowBinaryProgramCaching = true;
                private bool _renderMeshBounds = false;
                private bool _calculateBlendshapesInComputeShader = false;
                private bool _calculateSkinningInComputeShader = false;
                private string _defaultFontFolder = "Roboto";
                private string _defaultFontFileName = "Roboto-Regular.ttf";

                /// <summary>
                /// The default luminance used for calculation of exposure, etc.
                /// </summary>
                public Vector3 DefaultLuminance
                {
                    get => _defaultLuminance;
                    set => SetField(ref _defaultLuminance, value);
                }

                /// <summary>
                /// Shader pipelines allow for dynamic combination of shaders at runtime, such as mixing and matching vertex and fragment shaders.
                /// When this is off, a new shader program must be compiled for each unique combination of shaders.
                /// </summary>
                public bool AllowShaderPipelines
                {
                    get => _allowShaderPipelines;
                    set => SetField(ref _allowShaderPipelines, value);
                }
                /// <summary>
                /// When true, the engine will use integers in shaders instead of floats when needed.
                /// </summary>
                public bool UseIntegerUniformsInShaders
                {
                    get => _useIntegerUniformsInShaders;
                    set => SetField(ref _useIntegerUniformsInShaders, value);
                }
                /// <summary>
                /// When true, the engine will optimize the number of bone weights used per vertex if any vertex uses more than 4 weights.
                /// Will reduce shader calculations at the expense of skinning quality.
                /// </summary>
                public bool OptimizeSkinningTo4Weights
                {
                    get => _optimizeTo4Weights;
                    set => SetField(ref _optimizeTo4Weights, value);
                }
                /// <summary>
                /// This will pass vertex weights and indices to the shader as elements of a vec4 instead of using SSBO remaps for more straightforward calculation.
                /// Will not result in any quality loss and should be enabled if possible.
                /// </summary>
                public bool OptimizeSkinningWeightsIfPossible
                {
                    get => _optimizeWeightsIfPossible;
                    set => SetField(ref _optimizeWeightsIfPossible, value);
                }
                /// <summary>
                /// When items in the same group also have the same order value, this will dictate whether they are ticked in parallel or sequentially.
                /// Depending on how many items are in a singular tick order, this could be faster or slower.
                /// </summary>
                public bool TickGroupedItemsInParallel
                {
                    get => _tickGroupedItemsInParallel;
                    set => SetField(ref _tickGroupedItemsInParallel, value);
                }
                /// <summary>
                /// The default resolution of the light probe color texture.
                /// </summary>
                public uint LightProbeColorResolution
                {
                    get => lightProbeColorResolution;
                    set => SetField(ref lightProbeColorResolution, value);
                }
                /// <summary>
                /// If true, the light probes will also capture depth information.
                /// </summary>
                public bool LightProbesCaptureDepth
                {
                    get => _lightProbesCaptureDepth;
                    set => SetField(ref _lightProbesCaptureDepth, value);
                }
                /// <summary>
                /// The default resolution of the light probe depth texture.
                /// </summary>
                public uint LightProbeDepthResolution
                {
                    get => _lightProbeDepthResolution;
                    set => SetField(ref _lightProbeDepthResolution, value);
                }
                /// <summary>
                /// If true, the engine will cache compiled binary programs for faster loading times on next startups until the GPU driver is updated.
                /// </summary>
                public bool AllowBinaryProgramCaching 
                {
                    get => _allowBinaryProgramCaching;
                    set => SetField(ref _allowBinaryProgramCaching, value);
                }
                /// <summary>
                /// If true, the engine will render the bounds of each mesh.
                /// Useful for debugging, but should be disabled in production builds.
                /// </summary>
                public bool RenderMeshBounds 
                {
                    get => _renderMeshBounds;
                    set => SetField(ref _renderMeshBounds, value);
                }
                /// <summary>
                /// If true, the engine will calculate blendshapes in a compute shader rather than the vertex shader.
                /// Performance gain or loss may vary depending on the GPU.
                /// </summary>
                public bool CalculateBlendshapesInComputeShader
                {
                    get => _calculateBlendshapesInComputeShader;
                    set => SetField(ref _calculateBlendshapesInComputeShader, value);
                }
                /// <summary>
                /// If true, the engine will calculate skinning in a compute shader rather than the vertex shader.
                /// Performance gain or loss may vary depending on the GPU.
                /// </summary>
                public bool CalculateSkinningInComputeShader
                {
                    get => _calculateSkinningInComputeShader;
                    set => SetField(ref _calculateSkinningInComputeShader, value);
                }
                /// <summary>
                /// The name of the default font's folder within the engine's font directory.
                /// </summary>
                public string DefaultFontFolder 
                {
                    get => _defaultFontFolder;
                    set => SetField(ref _defaultFontFolder, value);
                }
                /// <summary>
                /// The name of the font file within the DefaultFontFolder directory.
                /// TTF or OTF files are supported, and the extension should be included in the string.
                /// </summary>
                public string DefaultFontFileName 
                {
                    get => _defaultFontFileName;
                    set => SetField(ref _defaultFontFileName, value);
                }
            }
        }
    }
}