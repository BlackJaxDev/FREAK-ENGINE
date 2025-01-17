using XREngine.Components;
using XREngine.Rendering;

namespace XREngine.Actors.Types.BSP
{
    public class BSPMeshComponent : XRComponent
    {
        
    }
    public enum EIntersectionType
    {
        Union,
        Intersection,
        Subtraction,
        Merge,
        Attach,
        Insert,
    }
}
