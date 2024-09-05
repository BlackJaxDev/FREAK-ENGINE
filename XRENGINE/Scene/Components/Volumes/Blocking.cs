using System.Numerics;
using XREngine.Components.Scene.Shapes;
using XREngine.Data.Transforms.Rotations;
using XREngine.Physics;

namespace XREngine.Components.Scene.Volumes
{
    public class BlockingVolumeComponent : BoxComponent
    {
        public BlockingVolumeComponent()
            : this(new Vector3(0.5f), Vector3.Zero, Rotator.GetZero(), 0, 0) { }
        public BlockingVolumeComponent(Vector3 halfExtents, Vector3 translation, Rotator rotation, ushort collisionGroup, ushort collidesWith)
            : base(halfExtents, new RigidBodyConstructionInfo()
            {
                Mass = 0.0f,
                CollisionEnabled = true,
                SimulatePhysics = false,
                CollisionGroup = collisionGroup,
                CollidesWith = collidesWith,
            })
        {
            RenderInfo.CastsShadows = false;
            RenderInfo.ReceivesShadows = false;
            RenderCommand.RenderPass = 0;
            //RenderParams.DepthTest.Enabled = false;

            //Transform.Translation.Value = translation;
            //Transform.Rotation.Value = rotation.ToQuaternion();
        }
    }
}
