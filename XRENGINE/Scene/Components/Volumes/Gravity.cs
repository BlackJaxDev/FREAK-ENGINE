using System.Numerics;
using XREngine.Physics;

namespace XREngine.Components.Scene.Volumes
{
    public class GravityVolumeComponent : TriggerVolumeComponent
    {
        public Vector3 Gravity { get; set; } = new Vector3(0.0f, -9.81f, 0.0f);

        //public GravityVolumeComponent() : this(new Vector3(0.5f)) { }
        //public GravityVolumeComponent(Vector3 halfExtents) : base(halfExtents) { }

        protected override void OnEntered(XRCollisionObject obj)
        {
            if (obj is XRRigidBody rb)
                rb.Gravity = Gravity;

            base.OnEntered(obj);
        }
        protected override void OnLeft(XRCollisionObject obj)
        {
            //if (obj is XRRigidBody rb)
            //    rb.Gravity = World.Gravity;

            base.OnLeft(obj);
        }
    }
}