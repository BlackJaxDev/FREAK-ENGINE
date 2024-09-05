using System.Numerics;

namespace XREngine.Physics
{
    /// <summary>
    /// Contains parameters for constructing a new soft body.
    /// </summary>
    public class TSoftBodyConstructionInfo : ICollisionObjectConstructionInfo
    {
        public bool CollisionEnabled = true;
        public bool SimulatePhysics = true;

        public ECollisionGroup CollisionGroup = ECollisionGroup.Default;
        public ECollisionGroup CollidesWith = ECollisionGroup.All;
        
        public float WaterOffset { get; set; }
        public Vector3 WaterNormal { get; set; }
        public float WaterDensity { get; set; }
        //public SparseSdf SparseSdf { get; }
        public float MaxDisplacement { get; set; }
        public Vector3 Gravity { get; set; }
        public float AirDensity { get; set; }

        public XRCollisionShape? CollisionShape { get; set; }
        public Matrix4x4 InitialWorldTransform { get; set; }
    }
}
