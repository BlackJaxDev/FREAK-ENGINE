using System.ComponentModel;
using System.Numerics;
using XREngine.Core.Files;
using XREngine.Data.Geometry;

namespace XREngine.Scene
{
    [Serializable]
    public class WorldSettings : XRAsset
    {
        private Vector3 _gravity = new(0.0f, -9.81f, 0.0f);
        public Vector3 Gravity
        {
            get => _gravity;
            set => SetField(ref _gravity, value);
        }

        private GameMode? _defaultGameMode;
        /// <summary>
        /// Overrides the default game mode specified by the game.
        /// </summary>
        public GameMode? DefaultGameMode
        {
            get => _defaultGameMode;
            set => SetField(ref _defaultGameMode, value);
        }

        private float _timeDilation = 1.0f;
        /// <summary>
        /// How fast the game moves. 
        /// A value of 2 will make the game 2x faster,
        /// while a value of 0.5 will make it 2x slower.
        /// </summary>
        [Description(
            "How fast the game moves. " +
            "A value of 2 will make the game 2x faster, " +
            "while a value of 0.5 will make it 2x slower.")]
        public float TimeDilation
        {
            get => _timeDilation;
            set => SetField(ref _timeDilation, value);
        }

        private AABB _bounds = AABB.FromSize(new(5000.0f));
        public AABB Bounds
        {
            get => _bounds;
            set => SetField(ref _bounds, value);
        }

        private bool _previewWorldBounds = true;
        public bool PreviewWorldBounds
        {
            get => _previewWorldBounds;
            set => SetField(ref _previewWorldBounds, value);
        }

        private bool _previewOctrees = false;
        public bool PreviewOctrees
        {
            get => _previewOctrees;
            set => SetField(ref _previewOctrees, value);
        }

        private bool _previewQuadtrees = false;
        public bool PreviewQuadtrees
        {
            get => _previewQuadtrees;
            set => SetField(ref _previewQuadtrees, value);
        }

        private bool _previewPhysics = false;
        public bool PreviewPhysics
        {
            get => _previewPhysics;
            set => SetField(ref _previewPhysics, value);
        }
    }
}
