using XREngine.Scene;

namespace XREngine.Rendering
{
    public interface IRigidBodyActor
    {
        AbstractRigidBody? RigidBody { get; set; }
    }
}