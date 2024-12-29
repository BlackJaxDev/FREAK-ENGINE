using XREngine.Core.Files;

namespace XREngine
{
    public static partial class Engine
    {
        public static partial class Rendering
        {
            /// <summary>
            /// These options should not be enabled in production builds.
            /// </summary>
            public partial class EngineSettings : XRAsset
            {
                private bool _preview3DWorldOctree = false;
                private bool _preview2DWorldQuadtree = false;
                private bool _previewTraces = false;

                /// <summary>
                /// If true, the engine will render the octree for the 3D world.
                /// </summary>
                public bool Preview3DWorldOctree
                {
                    get => _preview3DWorldOctree;
                    set => SetField(ref _preview3DWorldOctree, value);
                }

                /// <summary>
                /// If true, the engine will render the quadtree for the 2D world.
                /// </summary>
                public bool Preview2DWorldQuadtree
                {
                    get => _preview2DWorldQuadtree;
                    set => SetField(ref _preview2DWorldQuadtree, value);
                }                 
                
                /// <summary>
                /// If true, the engine will render physics traces.
                /// </summary>
                public bool PreviewTraces
                {
                    get => _previewTraces;
                    set => SetField(ref _previewTraces, value);
                }
            }
        }
    }
}