namespace XREngine.Physics.ContactTesting
{
    /// <summary>
    /// Returns all intersected objects that specify collision with this ray.
    /// </summary>
    public class ContactTestMulti : ContactTest
    {
        public override bool HasContact => Results.Count != 0;
        
        public List<ContactTestResult> Results { get; } = [];
        internal override void Reset() => Results.Clear();
        
        public ContactTestMulti(XRCollisionObject obj, ushort collisionGroupFlags, ushort collidesWithFlags, params XRCollisionObject[] ignored) 
            : base(obj, collisionGroupFlags, collidesWithFlags, ignored) { }

        protected internal override void AddResult(XRContactInfo contact, XRCollisionObject otherObject, bool isOtherB)
        {
            if (!CanAddCommon(otherObject))
                return;
            
            Results.Add(new ContactTestResult(contact, otherObject, isOtherB));
        }
    }
}
