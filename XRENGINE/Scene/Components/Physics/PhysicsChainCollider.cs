using System.Numerics;
using XREngine.Data;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Components;

public class PhysicsChainCollider : PhysicsChainColliderBase, IRenderable
{
    public float m_Radius = 0.5f;
    public float m_Height = 0;
    public float m_Radius2 = 0;

    float m_ScaledRadius;
    float m_ScaledRadius2;
    Vector3 m_C0;
    Vector3 m_C1;
    float m_C01Distance;
    int m_CollideType;

    public PhysicsChainCollider()
    {
        RenderedObjects =
        [
            RenderInfo3D.New(this, new RenderCommandMethod3D((int)EDefaultRenderPass.OpaqueForward, RenderGizmos))
        ];
    }

    public RenderInfo[] RenderedObjects { get; }

    protected internal override void OnComponentActivated()
    {
        base.OnComponentActivated();
        OnValidate();
    }

    void OnValidate()
    {
        m_Radius = MathF.Max(m_Radius, 0);
        m_Height = MathF.Max(m_Height, 0);
        m_Radius2 = MathF.Max(m_Radius2, 0);
    }

    public override void Prepare()
    {
        float scale = MathF.Abs(Transform.LossyWorldScale.X);
        float halfHeight = m_Height * 0.5f;

        if (m_Radius2 <= 0 || MathF.Abs(m_Radius - m_Radius2) < 0.01f)
        {
            m_ScaledRadius = m_Radius * scale;

            float h = halfHeight - m_Radius;
            if (h <= 0)
            {
                m_C0 = Transform.TransformPoint(m_Center);
                m_CollideType = m_Bound switch
                {
                    EBound.Outside => 0,
                    _ => 1,
                };
            }
            else
            {
                Vector3 c0 = m_Center;
                Vector3 c1 = m_Center;
                switch (m_Direction)
                {
                    case Direction.X:
                        c0.X += h;
                        c1.X -= h;
                        break;
                    case Direction.Y:
                        c0.Y += h;
                        c1.Y -= h;
                        break;
                    case Direction.Z:
                        c0.Z += h;
                        c1.Z -= h;
                        break;
                }

                m_C0 = Transform.TransformPoint(c0);
                m_C1 = Transform.TransformPoint(c1);
                m_C01Distance = (m_C1 - m_C0).Length();
                m_CollideType = m_Bound == EBound.Outside ? 2 : 3;
            }
        }
        else
        {
            float r = MathF.Max(m_Radius, m_Radius2);
            if (halfHeight - r <= 0)
            {
                m_ScaledRadius = r * scale;
                m_C0 = Transform.TransformPoint(m_Center);
                m_CollideType = m_Bound == EBound.Outside ? 0 : 1;
            }
            else
            {
                m_ScaledRadius = m_Radius * scale;
                m_ScaledRadius2 = m_Radius2 * scale;

                float h0 = halfHeight - m_Radius;
                float h1 = halfHeight - m_Radius2;
                Vector3 c0 = m_Center;
                Vector3 c1 = m_Center;

                switch (m_Direction)
                {
                    case Direction.X:
                        c0.X += h0;
                        c1.X -= h1;
                        break;
                    case Direction.Y:
                        c0.Y += h0;
                        c1.Y -= h1;
                        break;
                    case Direction.Z:
                        c0.Z += h0;
                        c1.Z -= h1;
                        break;
                }

                m_C0 = Transform.TransformPoint(c0);
                m_C1 = Transform.TransformPoint(c1);
                m_C01Distance = (m_C1 - m_C0).Length();
                m_CollideType = m_Bound == EBound.Outside ? 4 : 5;
            }
        }
    }

    public override bool Collide(ref Vector3 particlePosition, float particleRadius)
        => m_CollideType switch
        {
            0 => OutsideSphere(ref particlePosition, particleRadius, m_C0, m_ScaledRadius),
            1 => InsideSphere(ref particlePosition, particleRadius, m_C0, m_ScaledRadius),
            2 => OutsideCapsule(ref particlePosition, particleRadius, m_C0, m_C1, m_ScaledRadius, m_C01Distance),
            3 => InsideCapsule(ref particlePosition, particleRadius, m_C0, m_C1, m_ScaledRadius, m_C01Distance),
            4 => OutsideCapsule2(ref particlePosition, particleRadius, m_C0, m_C1, m_ScaledRadius, m_ScaledRadius2, m_C01Distance),
            5 => InsideCapsule2(ref particlePosition, particleRadius, m_C0, m_C1, m_ScaledRadius, m_ScaledRadius2, m_C01Distance),
            _ => false,
        };

    static bool OutsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
    {
        float r = sphereRadius + particleRadius;
        float r2 = r * r;
        Vector3 d = particlePosition - sphereCenter;
        float dlen2 = d.LengthSquared();

        // if is inside sphere, project onto sphere surface
        if (dlen2 > 0 && dlen2 < r2)
        {
            float dlen = MathF.Sqrt(dlen2);
            particlePosition = sphereCenter + d * (r / dlen);
            return true;
        }

        return false;
    }

    static bool InsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
    {
        float r = sphereRadius - particleRadius;
        float r2 = r * r;
        Vector3 d = particlePosition - sphereCenter;
        float dlen2 = d.LengthSquared();

        // if is outside sphere, project onto sphere surface
        if (dlen2 > r2)
        {
            float dlen = MathF.Sqrt(dlen2);
            particlePosition = sphereCenter + d * (r / dlen);
            return true;
        }

        return false;
    }

    static bool OutsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius, float dirlen)
    {
        float r = capsuleRadius + particleRadius;
        float r2 = r * r;
        Vector3 dir = capsuleP1 - capsuleP0;
        Vector3 d = particlePosition - capsuleP0;
        float t = Vector3.Dot(d, dir);

        if (t <= 0)
        {
            // check sphere1
            float dlen2 = d.LengthSquared();
            if (dlen2 > 0 && dlen2 < r2)
            {
                float dlen = MathF.Sqrt(dlen2);
                particlePosition = capsuleP0 + d * (r / dlen);
                return true;
            }
        }
        else
        {
            float dirlen2 = dirlen * dirlen;
            if (t >= dirlen2)
            {
                // check sphere2
                d = particlePosition - capsuleP1;
                float dlen2 = d.LengthSquared();
                if (dlen2 > 0 && dlen2 < r2)
                {
                    float dlen = MathF.Sqrt(dlen2);
                    particlePosition = capsuleP1 + d * (r / dlen);
                    return true;
                }
            }
            else
            {
                // check cylinder
                Vector3 q = d - dir * (t / dirlen2);
                float qlen2 = q.LengthSquared();
                if (qlen2 > 0 && qlen2 < r2)
                {
                    float qlen = MathF.Sqrt(qlen2);
                    particlePosition += q * ((r - qlen) / qlen);
                    return true;
                }
            }
        }
        return false;
    }

    static bool InsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius, float dirlen)
    {
        float r = capsuleRadius - particleRadius;
        float r2 = r * r;
        Vector3 dir = capsuleP1 - capsuleP0;
        Vector3 d = particlePosition - capsuleP0;
        float t = Vector3.Dot(d, dir);

        if (t <= 0)
        {
            // check sphere1
            float dlen2 = d.LengthSquared();
            if (dlen2 > r2)
            {
                float dlen = MathF.Sqrt(dlen2);
                particlePosition = capsuleP0 + d * (r / dlen);
                return true;
            }
        }
        else
        {
            float dirlen2 = dirlen * dirlen;
            if (t >= dirlen2)
            {
                // check sphere2
                d = particlePosition - capsuleP1;
                float dlen2 = d.LengthSquared();
                if (dlen2 > r2)
                {
                    float dlen = MathF.Sqrt(dlen2);
                    particlePosition = capsuleP1 + d * (r / dlen);
                    return true;
                }
            }
            else
            {
                // check cylinder
                Vector3 q = d - dir * (t / dirlen2);
                float qlen2 = q.LengthSquared();
                if (qlen2 > r2)
                {
                    float qlen = MathF.Sqrt(qlen2);
                    particlePosition += q * ((r - qlen) / qlen);
                    return true;
                }
            }
        }
        return false;
    }

    static bool OutsideCapsule2(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius0, float capsuleRadius1, float dirlen)
    {
        Vector3 dir = capsuleP1 - capsuleP0;
        Vector3 d = particlePosition - capsuleP0;
        float t = Vector3.Dot(d, dir);

        if (t <= 0)
        {
            // check sphere1
            float r = capsuleRadius0 + particleRadius;
            float r2 = r * r;
            float dlen2 = d.LengthSquared();
            if (dlen2 > 0 && dlen2 < r2)
            {
                float dlen = MathF.Sqrt(dlen2);
                particlePosition = capsuleP0 + d * (r / dlen);
                return true;
            }
        }
        else
        {
            float dirlen2 = dirlen * dirlen;
            if (t >= dirlen2)
            {
                // check sphere2
                float r = capsuleRadius1 + particleRadius;
                float r2 = r * r;
                d = particlePosition - capsuleP1;
                float dlen2 = d.LengthSquared();
                if (dlen2 > 0 && dlen2 < r2)
                {
                    float dlen = MathF.Sqrt(dlen2);
                    particlePosition = capsuleP1 + d * (r / dlen);
                    return true;
                }
            }
            else
            {
                // check cylinder
                Vector3 q = d - dir * (t / dirlen2);
                float qlen2 = q.LengthSquared();

                float klen = Vector3.Dot(d, dir / dirlen);
                float r = Interp.Lerp(capsuleRadius0, capsuleRadius1, klen / dirlen) + particleRadius;
                float r2 = r * r;

                if (qlen2 > 0 && qlen2 < r2)
                {
                    float qlen = MathF.Sqrt(qlen2);
                    particlePosition += q * ((r - qlen) / qlen);
                    return true;
                }
            }
        }
        return false;
    }

    static bool InsideCapsule2(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius0, float capsuleRadius1, float dirlen)
    {
        Vector3 dir = capsuleP1 - capsuleP0;
        Vector3 d = particlePosition - capsuleP0;
        float t = Vector3.Dot(d, dir);

        if (t <= 0)
        {
            // check sphere1
            float r = capsuleRadius0 - particleRadius;
            float r2 = r * r;
            float dlen2 = d.LengthSquared();
            if (dlen2 > r2)
            {
                float dlen = MathF.Sqrt(dlen2);
                particlePosition = capsuleP0 + d * (r / dlen);
                return true;
            }
        }
        else
        {
            float dirlen2 = dirlen * dirlen;
            if (t >= dirlen2)
            {
                // check sphere2
                float r = capsuleRadius1 - particleRadius;
                float r2 = r * r;
                d = particlePosition - capsuleP1;
                float dlen2 = d.LengthSquared();
                if (dlen2 > r2)
                {
                    float dlen = MathF.Sqrt(dlen2);
                    particlePosition = capsuleP1 + d * (r / dlen);
                    return true;
                }
            }
            else
            {
                // check cylinder
                Vector3 q = d - dir * (t / dirlen2);
                float qlen2 = q.LengthSquared();

                float klen = Vector3.Dot(d, dir / dirlen);
                float r = Interp.Lerp(capsuleRadius0, capsuleRadius1, klen / dirlen) - particleRadius;
                float r2 = r * r;

                if (qlen2 > r2)
                {
                    float qlen = MathF.Sqrt(qlen2);
                    particlePosition += q * ((r - qlen) / qlen);
                    return true;
                }
            }
        }
        return false;
    }

    private void RenderGizmos(bool shadowPass)
    {
        if (!IsActive || shadowPass)
            return;

        Prepare();

        ColorF4 color = m_Bound == EBound.Outside ? ColorF4.Yellow : ColorF4.Magenta;
        switch (m_CollideType)
        {
            case 0:
            case 1:
                Engine.Rendering.Debug.RenderSphere(m_C0, m_ScaledRadius, false, color);
                break;
            case 2:
            case 3:
                DrawCapsule(m_C0, m_C1, m_ScaledRadius, m_ScaledRadius, color);
                break;
            case 4:
            case 5:
                DrawCapsule(m_C0, m_C1, m_ScaledRadius, m_ScaledRadius2, color);
                break;
        }
    }

    static void DrawCapsule(Vector3 c0, Vector3 c1, float radius0, float radius1, ColorF4 color)
    {
        if (radius0 != radius1)
        {
            Engine.Rendering.Debug.RenderLine(c0, c1, color);
            Engine.Rendering.Debug.RenderSphere(c0, radius0, false, color);
            Engine.Rendering.Debug.RenderSphere(c1, radius1, false, color);
        }
        else
        {
            Engine.Rendering.Debug.RenderCapsule(c0, c1, radius0, false, color);
        }
    }
}
