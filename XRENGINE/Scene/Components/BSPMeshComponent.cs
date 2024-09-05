using XREngine.Components;

namespace XREngine.Actors.Types.BSP
{
    public class BSPMeshComponent : XRComponent
    {
        //private XRMeshRenderer _manager;
        //public XRMeshRenderer Merge(XRMeshRenderer right, EIntersectionType intersection)
        //{
        //    XRMeshRenderer m = new();
        //    switch (intersection)
        //    {
        //        case EIntersectionType.Union:

        //            break;
        //        case EIntersectionType.Intersection:

        //            break;
        //        case EIntersectionType.Subtraction:

        //            break;
        //        case EIntersectionType.Merge:

        //            break;
        //        case EIntersectionType.Attach:

        //            break;
        //        case EIntersectionType.Insert:

        //            break;
        //    }
        //    return m;
        //}
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
