using System.Numerics;
using XREngine.Physics;

namespace XREngine.Components.Scene.Volumes
{
    public class GravityVolumeComponent : TriggerVolumeComponent
    {
        private Vector3 _gravity = new(0.0f, -9.81f, 0.0f);
        public Vector3 Gravity
        {
            get => _gravity;
            set => SetField(ref _gravity, value);
        }

        protected override void OnEntered(XRCollisionObject obj)
        {
            if (obj is XRRigidBody rb)
                rb.Gravity = Gravity;

            base.OnEntered(obj);
        }
        protected override void OnLeft(XRCollisionObject obj)
        {
            if (obj is XRRigidBody rb && World?.PhysicsScene != null)
                rb.Gravity = World.PhysicsScene.Gravity;

            base.OnLeft(obj);
        }
    }
}