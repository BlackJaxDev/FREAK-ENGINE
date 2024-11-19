using System.Numerics;
using XREngine.Components;

public class PhysicsChainColliderBase : XRComponent
{
    public enum Direction
    {
        X, Y, Z
    }

    public Direction m_Direction = Direction.Y;
    public Vector3 m_Center = Vector3.Zero;

    public enum EBound
    {
        Outside,
        Inside
    }

    public EBound m_Bound = EBound.Outside;

    public int PrepareFrame { set; get; }

    public virtual void Start()
    {

    }

    public virtual void Prepare()
    {

    }

    public virtual bool Collide(ref Vector3 particlePosition, float particleRadius)
    {
        return false;
    }
}
