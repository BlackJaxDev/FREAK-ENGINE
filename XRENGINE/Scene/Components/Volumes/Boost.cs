using System.Numerics;

namespace XREngine.Components.Scene.Volumes
{
    public class BoostVolumeComponent : TriggerVolumeComponent
    {
        public Vector3 Force { get; set; }

        //public BoostVolumeComponent()
        //    : base() { }
        //public BoostVolumeComponent(Vector3 halfExtents)
        //    : base(halfExtents) { }
    }
}
