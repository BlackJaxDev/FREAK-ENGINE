using XREngine.Components;
using XREngine.Scene;

namespace XREngine.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class XRComponentAttribute : Attribute
    {
        /// <summary>
        /// Run logic when a component is added to a scene node.
        /// Returns false if the component should not be added.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public abstract bool VerifyComponentOnAdd(SceneNode node, XRComponent comp);
    }
}
