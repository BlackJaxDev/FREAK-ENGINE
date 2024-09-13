using System.Numerics;

namespace XREngine.Physics
{
    public interface ICollisionObjectConstructionInfo
    {
        XRCollisionShape? CollisionShape { get; set; }
        Matrix4x4 InitialWorldTransform { get; set; }
    }
}
