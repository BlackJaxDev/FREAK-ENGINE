using System;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Transforms.Rotations
{
    //public struct Quat
    //{
    //    public float x;
    //    public float y;
    //    public float z;
    //    public float w;

    //    public Quat() : this(0, 0, 0, 1) { }
    //    public Quat(float x, float y, float z, float w)
    //    {
    //        this.x = x;
    //        this.y = y;
    //        this.z = z;
    //        this.w = w;
    //    }
    //    public Quat(ulong compressedQuaternion)
    //    {
    //        byte largestComponentIndex = (byte)(compressedQuaternion & 0x3);
    //        float[] components = new float[4];

    //        byte currentIndex = 8;
    //        for (byte i = 0; i < 4; i++)
    //            if (i != largestComponentIndex)
    //            {
    //                ushort compressedComponent = (ushort)(compressedQuaternion >> currentIndex & 0xFFFF);
    //                components[i] = compressedComponent / (float)ushort.MaxValue * 2.0f - 1.0f;
    //                currentIndex += 16;
    //            }

    //        float sum = 1.0f - (components[0] * components[0] + components[1] * components[1] + components[2] * components[2]);
    //        components[largestComponentIndex] = sum > 0.0f ? (float)Math.Sqrt(sum) : 0.0f;

    //        x = components[0];
    //        y = components[1];
    //        z = components[2];
    //        w = components[3];
    //    }

    //    public static Quat Identity => new(0, 0, 0, 1);

    //    public float MagnitudeSquared()
    //        => x * x + y * y + z * z + w * w;
    //    public float Magnitude()
    //        => (float)Math.Sqrt(MagnitudeSquared());

    //    public void Normalize()
    //    {
    //        float magnitude = Magnitude();
    //        x /= magnitude;
    //        y /= magnitude;
    //        z /= magnitude;
    //        w /= magnitude;
    //    }
    //    public Quat Normalized()
    //    {
    //        float magnitude = Magnitude();
    //        return new Quat(x / magnitude, y / magnitude, z / magnitude, w / magnitude);
    //    }
    //    public static Quat Slerp(Quat a, Quat b, float t)
    //    {
    //        float dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
    //        if (dot < 0)
    //        {
    //            dot = -dot;
    //            t = -t;
    //        }

    //        if (1.0f - dot > 0.0001f)
    //        {
    //            float angle = (float)Math.Acos(dot);
    //            float sin_angle = (float)Math.Sin(angle);
    //            float inv_sin_angle = 1.0f / sin_angle;
    //            float angle_a = (float)Math.Sin((1.0f - t) * angle) * inv_sin_angle;
    //            float angle_b = (float)Math.Sin(t * angle) * inv_sin_angle;

    //            return new Quat(
    //                a.x * angle_a + b.x * angle_b,
    //                a.y * angle_a + b.y * angle_b,
    //                a.z * angle_a + b.z * angle_b,
    //                a.w * angle_a + b.w * angle_b
    //            );
    //        }

    //        return new Quat(
    //            a.x + t * (b.x - a.x),
    //            a.y + t * (b.y - a.y),
    //            a.z + t * (b.z - a.z),
    //            a.w + t * (b.w - a.w)
    //        );
    //    }
    //    public static Quat Slerp2(Quat a, Quat b, float t)
    //    {
    //        float cosHalfTheta = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
    //        if (cosHalfTheta < 0)
    //        {
    //            b = new Quat(-b.x, -b.y, -b.z, -b.w);
    //            cosHalfTheta = -cosHalfTheta;
    //        }

    //        if (Math.Abs(cosHalfTheta) >= 1.0)
    //        {
    //            return a;
    //        }

    //        float sinHalfTheta = (float)Math.Sqrt(1.0 - cosHalfTheta * cosHalfTheta);
    //        float halfTheta = (float)Math.Acos(cosHalfTheta);

    //        float ratioA = (float)Math.Sin((1 - t) * halfTheta) / sinHalfTheta;
    //        float ratioB = (float)Math.Sin(t * halfTheta) / sinHalfTheta;

    //        return new Quat(
    //            a.x * ratioA + b.x * ratioB,
    //            a.y * ratioA + b.y * ratioB,
    //            a.z * ratioA + b.z * ratioB,
    //            a.w * ratioA + b.w * ratioB
    //        );
    //    }
    //    public static Quat LookAt(Vec3 forward, Vec3 up)
    //    {
    //        Vec3 zAxis = forward.Normalized();
    //        Vec3 xAxis = Vec3.Cross(up, zAxis).Normalized();
    //        Vec3 yAxis = Vec3.Cross(zAxis, xAxis);

    //        float m00 = xAxis.x, m01 = xAxis.y, m02 = xAxis.z;
    //        float m10 = yAxis.x, m11 = yAxis.y, m12 = yAxis.z;
    //        float m20 = zAxis.x, m21 = zAxis.y, m22 = zAxis.z;

    //        float num8 = (float)(m00 + m11 + m22);
    //        Quat result = new();

    //        if (num8 > 0.0f)
    //        {
    //            float num = (float)Math.Sqrt(num8 + 1.0f);
    //            result.w = num * 0.5f;
    //            num = 0.5f / num;
    //            result.x = (m12 - m21) * num;
    //            result.y = (m20 - m02) * num;
    //            result.z = (m01 - m10) * num;
    //            return result;
    //        }

    //        if (m00 >= m11 && m00 >= m22)
    //        {
    //            float num7 = (float)Math.Sqrt(1.0f + m00 - m11 - m22);
    //            float num4 = 0.5f / num7;
    //            result.x = 0.5f * num7;
    //            result.y = (m01 + m10) * num4;
    //            result.z = (m02 + m20) * num4;
    //            result.w = (m12 - m21) * num4;
    //            return result;
    //        }

    //        if (m11 > m22)
    //        {
    //            float num6 = (float)Math.Sqrt(1.0f + m11 - m00 - m22);
    //            float num3 = 0.5f / num6;
    //            result.x = (m10 + m01) * num3;
    //            result.y = 0.5f * num6;
    //            result.z = (m21 + m12) * num3;
    //            result.w = (m20 - m02) * num3;
    //            return result;
    //        }

    //        float num5 = (float)Math.Sqrt(1.0f + m22 - m00 - m11);
    //        float num2 = 0.5f / num5;
    //        result.x = (m20 + m02) * num2;
    //        result.y = (m21 + m12) * num2;
    //        result.z = 0.5f * num5;
    //        result.w = (m01 - m10) * num2;
    //        return result;
    //    }
    //    public Vec3 Rotate(Vec3 v)
    //    {
    //        Quat q = new(v.x, v.y, v.z, 0);
    //        Quat qInverse = new(-x, -y, -z, w);
    //        Quat result = this * q * qInverse;
    //        return new(result.x, result.y, result.z);
    //    }
    //    public enum RotationOrder
    //    {
    //        YXZ,
    //        ZYX,
    //        XYZ,
    //        XZY,
    //        YZX,
    //        ZXY
    //    }
    //    public Vec3 ToEulerAngles(RotationOrder order)
    //    {
    //        double tx, ty, tz;

    //        switch (order)
    //        {
    //            case RotationOrder.YXZ:
    //                ty = Math.Asin(2 * (w * y - z * x));
    //                if (Math.Abs(y) < Math.PI / 2)
    //                {
    //                    tx = Math.Atan2(2 * (w * x + y * z), 1 - 2 * (x * x + y * y));
    //                    tz = Math.Atan2(2 * (w * z + x * y), 1 - 2 * (y * y + z * z));
    //                }
    //                else
    //                {
    //                    tx = 0;
    //                    tz = Math.Atan2(-2 * (x * y - w * z), 1 - 2 * (y * y + z * z));
    //                }
    //                break;
    //            case RotationOrder.ZYX:
    //                tz = Math.Asin(2 * (w * z - x * y));
    //                if (Math.Abs(z) < Math.PI / 2)
    //                {
    //                    tx = Math.Atan2(2 * (w * x + y * z), 1 - 2 * (x * x + z * z));
    //                    ty = Math.Atan2(2 * (w * y + x * z), 1 - 2 * (y * y + z * z));
    //                }
    //                else
    //                {
    //                    tx = 0;
    //                    ty = Math.Atan2(-2 * (y * z - w * x), 1 - 2 * (x * x + z * z));
    //                }
    //                break;
    //            case RotationOrder.XYZ:
    //                tx = Math.Asin(2 * (w * x - y * z));
    //                if (Math.Abs(x) < Math.PI / 2)
    //                {
    //                    ty = Math.Atan2(2 * (w * y + x * z), 1 - 2 * (x * x + y * y));
    //                    tz = Math.Atan2(2 * (w * z + x * y), 1 - 2 * (x * x + z * z));
    //                }
    //                else
    //                {
    //                    ty = 0;
    //                    tz = Math.Atan2(-2 * (y * x - w * z), 1 - 2 * (x * x + y * y));
    //                }
    //                break;
    //            case RotationOrder.YZX:
    //                ty = Math.Asin(2 * (w * y + z * x));
    //                if (Math.Abs(y) < Math.PI / 2)
    //                {
    //                    tx = Math.Atan2(-2 * (z * w - x * y), 1 - 2 * (y * y + z * z));
    //                    tz = Math.Atan2(-2 * (x * w - y * z), 1 - 2 * (x * x + y * y));
    //                }
    //                else
    //                {
    //                    tx = 0;
    //                    tz = Math.Atan2(2 * (x * z + w * y), 1 - 2 * (x * x + z * z));
    //                }
    //                break;
    //            case RotationOrder.XZY:
    //                tx = Math.Asin(2 * (w * x + y * z));
    //                if (Math.Abs(x) < Math.PI / 2)
    //                {
    //                    ty = Math.Atan2(-2 * (z * w - x * y), 1 - 2 * (x * x + z * z));
    //                    tz = Math.Atan2(-2 * (y * w - x * z), 1 - 2 * (x * x + y * y));
    //                }
    //                else
    //                {
    //                    ty = 0;
    //                    tz = Math.Atan2(2 * (y * z + w * x), 1 - 2 * (x * x + z * z));
    //                }
    //                break;
    //            case RotationOrder.ZXY:
    //                tz = Math.Asin(2 * (w * z + x * y));
    //                if (Math.Abs(z) < Math.PI / 2)
    //                {
    //                    tx = Math.Atan2(-2 * (y * w - x * z), 1 - 2 * (y * y + z * z));
    //                    ty = Math.Atan2(-2 * (x * w - y * z), 1 - 2 * (x * x + z * z));
    //                }
    //                else
    //                {
    //                    tx = 0;
    //                    ty = Math.Atan2(2 * (x * y + w * z), 1 - 2 * (y * y + z * z));
    //                }
    //                break;
    //            default:
    //                throw new ArgumentException("Invalid rotation order.");
    //        }

    //        return new Vec3((float)tx, (float)ty, (float)tz);
    //    }

    //    public static Quat FromEulerAngles(Vec3 eulerAngles, RotationOrder order)
    //    {
    //        float x = eulerAngles.x;
    //        float y = eulerAngles.y;
    //        float z = eulerAngles.z;

    //        float c1 = (float)Math.Cos(y / 2);
    //        float s1 = (float)Math.Sin(y / 2);
    //        float c2 = (float)Math.Cos(z / 2);
    //        float s2 = (float)Math.Sin(z / 2);
    //        float c3 = (float)Math.Cos(x / 2);
    //        float s3 = (float)Math.Sin(x / 2);

    //        return order switch
    //        {
    //            RotationOrder.YXZ => new Quat(
    //                c1 * c2 * c3 - s1 * s2 * s3,
    //                s1 * c2 * c3 + c1 * s2 * s3,
    //                c1 * s2 * c3 - s1 * c2 * s3,
    //                c1 * c2 * s3 + s1 * s2 * c3),
    //            RotationOrder.ZYX => new Quat(
    //                c1 * c2 * c3 + s1 * s2 * s3,
    //                s1 * c2 * c3 - c1 * s2 * s3,
    //                c1 * s2 * c3 + s1 * c2 * s3,
    //                c1 * c2 * s3 - s1 * s2 * c3),
    //            RotationOrder.XYZ => new Quat(
    //                c1 * c2 * c3 - s1 * s2 * s3,
    //                s1 * c2 * c3 + c1 * s2 * s3,
    //                c1 * s2 * c3 + s1 * c2 * s3,
    //                c1 * c2 * s3 - s1 * s2 * c3),
    //            RotationOrder.YZX => new Quat(
    //                c1 * c2 * c3 - s1 * s2 * s3,
    //                s1 * c2 * c3 + c1 * s2 * s3,
    //                c1 * s2 * c3 + s1 * c2 * s3,
    //                c1 * c2 * s3 - s1 * s2 * c3),
    //            RotationOrder.XZY => new Quat(
    //                c1 * c2 * c3 - s1 * s2 * s3,
    //                s1 * c2 * c3 + c1 * s2 * s3,
    //                c1 * s2 * c3 + s1 * c2 * s3,
    //                c1 * c2 * s3 - s1 * s2 * c3),
    //            RotationOrder.ZXY => new Quat(
    //                c1 * c2 * c3 + s1 * s2 * s3,
    //                s1 * c2 * c3 - c1 * s2 * s3,
    //                c1 * s2 * c3 - s1 * c2 * s3,
    //                c1 * c2 * s3 + s1 * s2 * c3),
    //            _ => throw new ArgumentException("Invalid rotation order."),
    //        };
    //    }

    //    public Matrix ToMatrix() => Matrix.CreateRotation(this);

    //    public ulong Compress()
    //    {
    //        Quat normalizedQuaternion = Normalized();
    //        float[] components = { normalizedQuaternion.x, normalizedQuaternion.y, normalizedQuaternion.z, normalizedQuaternion.w };

    //        byte largestComponentIndex = 0;
    //        for (byte i = 1; i < 4; i++)
    //            if (Math.Abs(components[i]) > Math.Abs(components[largestComponentIndex]))
    //                largestComponentIndex = i;

    //        ulong encodedQuaternion = largestComponentIndex;
    //        byte currentIndex = 8;
    //        for (byte i = 0; i < 4; i++)
    //        {
    //            if (i != largestComponentIndex)
    //            {
    //                ushort compressedComponent = (ushort)((components[i] + 1.0f) * 0.5f * ushort.MaxValue);
    //                encodedQuaternion |= (ulong)compressedComponent << currentIndex;
    //                currentIndex += 16;
    //            }
    //        }

    //        return encodedQuaternion;
    //    }

    //    public static Quat operator *(Quat a, Quat b)
    //    {
    //        return new Quat(
    //            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
    //            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
    //            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
    //            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
    //    }

    //    public override string ToString() => string.Format("Quaternion({0}, {1}, {2}, {3})", x, y, z, w);
    //}
}
