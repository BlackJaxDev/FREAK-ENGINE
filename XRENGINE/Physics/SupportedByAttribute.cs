namespace XREngine.Physics
{
    public class PhysicsSupportAttribute : Attribute
    {
        public EPhysicsLibrary[] PhysicsLibraries { get; }

        public PhysicsSupportAttribute(params EPhysicsLibrary[] libraries)
        {
            PhysicsLibraries = libraries;
        }
    }
}