namespace XREngine.Physics
{
    public interface IGenericCollidable : ICollidable
    {
        XRCollisionObject CollisionObject { get; }
    }
}
