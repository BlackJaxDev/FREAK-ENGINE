using System.Numerics;

namespace XREngine.Physics
{
    public abstract class XRCollisionCompoundShape : XRCollisionShape
    {
        public static XRCollisionCompoundShape New((Matrix4x4 localTransform, XRCollisionShape shape)[] shapes)
            => Engine.Physics.NewCompoundShape(shapes);
    }
}
