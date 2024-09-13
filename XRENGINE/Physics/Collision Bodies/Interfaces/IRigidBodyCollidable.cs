namespace XREngine.Physics
{
    public interface IRigidBodyCollidable : ICollidable
    {
        XRRigidBody RigidBodyCollision { get; }
    }
}
