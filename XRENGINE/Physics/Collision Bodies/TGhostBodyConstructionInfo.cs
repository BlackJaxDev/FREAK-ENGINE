//using System.ComponentModel;
//using XREngine.Core.Files;

//namespace XREngine.Physics
//{
//    /// <summary>
//    /// Contains parameters for constructing a new ghost body.
//    /// </summary>
//    public class TGhostBodyConstructionInfo : XRAsset, ICollisionObjectConstructionInfo
//    {
//        public TGhostBodyConstructionInfo() { }
//        public TGhostBodyConstructionInfo(
//            XRCollisionShape shape,
//            float mass,
//            Vector3? localIntertia,
//            bool useMotionState,
//            ushort collisionGroup,
//            ushort collidesWith,
//            bool collisionEnabled,
//            bool simulatePhysics,
//            bool sleepingEnabled,
//            float deactivationTime)
//        {
//            CollisionShape = shape;
//            Mass = mass;
//            LocalInertia = localIntertia ?? shape?.CalculateLocalInertia(mass) ?? Vector3.Zero;
//            UseMotionState = useMotionState;
//            CollisionGroup = collisionGroup;
//            CollidesWith = collidesWith;
//            CollisionEnabled = collisionEnabled;
//            SimulatePhysics = simulatePhysics;
//            SleepingEnabled = sleepingEnabled;
//            DeactivationTime = deactivationTime;
//        }

//        [TSerialize]
//        public bool SleepingEnabled { get; set; } = true;
//        [TSerialize]
//        public bool CollisionEnabled { get; set; } = true;
//        [TSerialize]
//        public bool SimulatePhysics { get; set; } = true;

//        /// <summary>
//        /// Use <see cref="ECollisionGroup"/> or your own enum if you want. Note that the enum must be flags.
//        /// </summary>
//        [TSerialize]
//        public ushort CollisionGroup { get; set; } = 1;
//        /// <summary>
//        /// Use <see cref="ECollisionGroup"/> or your own enum if you want. Note that the enum must be flags.
//        /// </summary>
//        [TSerialize]
//        public ushort CollidesWith { get; set; } = 0xFFFF;

//        [TSerialize]
//        public bool UseMotionState { get; set; } = true;
//        [TSerialize]
//        public Matrix4 CenterOfMassOffset { get; set; } = Matrix4.Identity;
//        [TSerialize]
//        public Matrix4 InitialWorldTransform { get; set; } = Matrix4.Identity;

//        [TSerialize]
//        public float Friction { get; set; } = 0.5f;
//        [TSerialize]
//        public float Restitution { get; set; } = 0.0f;
//        [TSerialize]
//        public float RollingFriction { get; set; } = 0.0f;
//        [TSerialize]
//        public bool IsKinematic { get; set; } = false;
//        [TSerialize]
//        public bool CustomMaterialCallback { get; set; } = true;
//        [TSerialize]
//        public float CcdMotionThreshold { get; set; } = 0.0f;
//        [TSerialize]
//        public float DeactivationTime { get; set; } = 0.0f;
//        [TSerialize]
//        public float CcdSweptSphereRadius { get; set; } = 0.0f;
//        [TSerialize]
//        public float ContactProcessingThreshold { get; set; } = 0.0f;

//        /// <summary>
//        /// Inertia vector relative to the rigid body's local frame space. 
//        /// Auto-calculated when you set CollisionShape or Mass (using both), 
//        /// so set after setting those if you want to override.
//        /// </summary>
//        [Description("Inertia vector relative to the rigid body's local frame space. " +
//            "Auto-calculated when you set CollisionShape or Mass (requires both to calculate), " +
//            "so set after setting those if you want to override.")]
//        [TSerialize]
//        public Vector3 LocalInertia { get; set; } = Vector3.Zero;

//        /// <summary>
//        /// The shape this rigid body will use to collide.
//        /// Auto-calculates LocalInertia for you using Mass and the given shape.
//        /// </summary>
//        public XRCollisionShape CollisionShape
//        {
//            get => _collisionShape;
//            set
//            {
//                _collisionShape = value;
//                if (CollisionShape != null)
//                    LocalInertia = CollisionShape.CalculateLocalInertia(Mass);
//            }
//        }
//        /// <summary>
//        /// The mass of this rigid body.
//        /// Auto-calculates LocalIntertia for you using CollisionShape and the given mass.
//        /// </summary>
//        public float Mass
//        {
//            get => _mass;
//            set
//            {
//                _mass = value;
//                if (CollisionShape != null)
//                    LocalInertia = CollisionShape.CalculateLocalInertia(Mass);
//            }
//        }

//        [TSerialize(nameof(Mass))]
//        private float _mass = 1.0f;
//        [TSerialize(nameof(CollisionShape))]
//        private XRCollisionShape _collisionShape = null;
//    }
//}
