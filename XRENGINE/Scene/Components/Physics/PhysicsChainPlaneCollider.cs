using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Components;

public class PhysicsChainPlaneCollider : PhysicsChainColliderBase, IRenderable
{
    public Plane _plane;

    public RenderInfo[] RenderedObjects { get; }

    public PhysicsChainPlaneCollider()
    {
        RenderedObjects =
        [
            RenderInfo3D.New(this, new RenderCommandMethod3D((int)EDefaultRenderPass.OpaqueForward, OnDrawGizmosSelected))
        ];
    }

    public override void Prepare()
    {
        Vector3 normal = Globals.Up;
        switch (m_Direction)
        {
            case Direction.X:
                normal = Transform.WorldRight;
                break;
            case Direction.Y:
                normal = Transform.WorldUp;
                break;
            case Direction.Z:
                normal = Transform.WorldForward;
                break;
        }

        Vector3 p = Transform.TransformPoint(m_Center);
        _plane = XRMath.CreatePlaneFromPointAndNormal(p, normal);
    }

    public override bool Collide(ref Vector3 particlePosition, float particleRadius)
    {
        float d = GeoUtil.DistancePlanePoint(_plane, particlePosition);

        if (m_Bound == EBound.Outside)
        {
            if (d < 0)
            {
                particlePosition -= _plane.Normal * d;
                return true;
            }
        }
        else
        {
            if (d > 0)
            {
                particlePosition -= _plane.Normal * d;
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected(bool shadowPass)
    {
        if (!IsActive)
            return;

        Prepare();

        ColorF4 color = m_Bound == EBound.Outside ? ColorF4.Yellow : ColorF4.Magenta;
        Vector3 p = Transform.TransformPoint(m_Center);
        Engine.Rendering.Debug.RenderLine(p, p + _plane.Normal, color);
    }
}
