using XREngine.Data.Core;

namespace XREngine.Rendering.Physics.Physx
{
    public abstract class AbstractPhysicsMaterial : XRBase
    {
        public AbstractPhysicsMaterial() { }

        public abstract float StaticFriction { get; set; }
        public abstract float DynamicFriction { get; set; }
        public abstract float Restitution { get; set; }
        public abstract float Damping { get; set; }

        public abstract ECombineMode FrictionCombineMode { get; set; }
        public abstract ECombineMode RestitutionCombineMode { get; set; }

        public abstract bool DisableFriction { get; set; }
        public abstract bool DisableStrongFriction { get; set; }
        public abstract bool ImprovedPatchFriction { get; set; }
        public abstract bool CompliantContact { get; set; }
    }
}