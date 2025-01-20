using System.Numerics;

namespace XREngine.Components.Scene.Volumes
{
    public class BoostVolumeComponent : TriggerVolumeComponent
    {
        private Vector3 _force;
        public Vector3 Force
        {
            get => _force;
            set => SetField(ref _force, value);
        }
    }
}
