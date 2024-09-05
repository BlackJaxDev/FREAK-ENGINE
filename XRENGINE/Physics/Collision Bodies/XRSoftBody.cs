using System.Numerics;

namespace XREngine.Physics
{
    public abstract class XRSoftBody : XRCollisionObject
    {
        public XRSoftBody() : base() { }

        public new ISoftBodyCollidable? Owner
        {
            get => base.Owner as ISoftBodyCollidable;
            set => base.Owner = value;
        }

        public static XRSoftBody New(TSoftBodyConstructionInfo info)
            => Engine.Physics.NewSoftBody(info);

        public abstract Vector3 WindVelocity { get; set; }
        public abstract float Volume { get; }
        public abstract float TotalMass { get; set; }
        public abstract Matrix4x4 InitialWorldTransform { get; set; }
    }
}
