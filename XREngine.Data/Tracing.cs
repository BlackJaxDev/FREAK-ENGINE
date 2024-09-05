using System.Numerics;

namespace XREngine
{
    public delegate void OnTraceHit(HitInfo hit);
    public class HitInfo
    {
        public Vector3 _hitNormal;
        public Vector3 _location;
        //public ActorComponent? _hitActor;
        //public SkeletalModel? _hitModel;
    }
}
