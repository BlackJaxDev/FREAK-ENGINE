using XREngine.Components;
using XREngine.Scene;

namespace XREngine.Core.Attributes
{
    public class OneComponentAllowedAttribute : XRComponentAttribute
    {
        public override bool VerifyComponentOnAdd(SceneNode node, XRComponent comp)
        {
            foreach (var c in node.Components)
            {
                if (c.GetType() == comp.GetType())
                {
                    Debug.LogWarning($"Cannot add component {comp.GetType().Name} to node {node.Name} because only one component of this type is allowed.");
                    return false;
                }
            }
            return true;
        }
    }
}