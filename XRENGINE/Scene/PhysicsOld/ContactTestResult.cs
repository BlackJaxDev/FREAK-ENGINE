namespace XREngine.Physics.ContactTesting
{
    /// <summary>
    /// Provides information about a ray intersection.
    /// </summary>
    public class ContactTestResult
    {
        public XRCollisionObject CollisionObject { get; internal set; }
        public bool IsObjectB { get; internal set; }
        public XRContactInfo Contact { get; internal set; }
        
        internal ContactTestResult() { }
        internal ContactTestResult(XRContactInfo contact, XRCollisionObject obj, bool isB)
        {
            CollisionObject = obj;
            Contact = contact;
            IsObjectB = isB;
        }
    }
}
