using System.Numerics;

namespace XREngine.Physics
{
    public interface ICollidable
    {
        Matrix4x4 CollidableWorldMatrix { get; set; }
    }
}
