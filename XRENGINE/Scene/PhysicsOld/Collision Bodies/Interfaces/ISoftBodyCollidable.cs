namespace XREngine.Physics
{
    public interface ISoftBodyCollidable : ICollidable
    {
        XRSoftBody SoftBodyCollision { get; }
    }
}
