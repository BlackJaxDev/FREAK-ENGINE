using Extensions;
using System.Runtime.InteropServices;

namespace XREngine.Data.Transforms.Vectors
{
    //[StructLayout(LayoutKind.Sequential, Pack = 0)]
    //public unsafe struct Vec3
    //{
    //    public float x;
    //    public float y;
    //    public float z;

    //    public float this[int index]
    //    {
    //        get
    //        {
    //            fixed (void* ptr = &this)
    //                return ((float*)ptr)[index];
    //        }
    //        set
    //        {
    //            fixed (void* ptr = &this)
    //                ((float*)ptr)[index] = value;
    //        }
    //    }

    //    public Vec3(float v)
    //    {
    //        x = v;
    //        y = v;
    //        z = v;
    //    }
    //    public Vec3(float x, float y, float z)
    //    {
    //        this.x = x;
    //        this.y = y;
    //        this.z = z;
    //    }

    //    /// <summary>
    //    /// Returns the length (aka magnitude) of the vector, but without taking the square root.
    //    /// </summary>
    //    public readonly float LengthSquared => this | this;
    //    /// <summary>
    //    /// Returns the length (aka magnitude) of the vector.
    //    /// </summary>
    //    public readonly float Length => (float)Math.Sqrt(LengthSquared);

    //    public static Vec3 UnitZ { get; } = new(0.0f, 0.0f, 1.0f);
    //    public static Vec3 UnitY { get; } = new(0.0f, 1.0f, 0.0f);
    //    public static Vec3 UnitX { get; } = new(1.0f, 0.0f, 0.0f);
    //    public static Vec3 Zero { get; } = new(0.0f, 0.0f, 0.0f);
    //    public static Vec3 One { get; } = new(1.0f, 1.0f, 1.0f);
    //    public static Vec3 Max { get; } = new(float.MaxValue);
    //    public static Vec3 Min { get; } = new(float.MinValue);
    //    public static Vec3 Up => UnitY;
    //    public static Vec3 Down => -UnitY;
    //    public static Vec3 Right => UnitX;
    //    public static Vec3 Left => -UnitX;
    //    public static Vec3 Forward => UnitZ;
    //    public static Vec3 Backward => -UnitZ;

    //    public float X { get => x; set => x = value; }
    //    public float Y { get => y; set => y = value; }
    //    public float Z { get => z; set => z = value; }

    //    public Matrix ToScaleMatrix() => Matrix.CreateScale(this);
    //    public Matrix ToTranslationMatrix() => Matrix.CreateTranslation(this);
    //    public Matrix ToInverseScaleMatrix() => Matrix.CreateScale(1.0f / this);
    //    public Matrix ToInverseTranslationMatrix() => Matrix.CreateTranslation(-this);

    //    /// <summary>
    //    /// Returns a normalized version of this vector.
    //    /// </summary>
    //    /// <returns></returns>
    //    public Vec3 Normalized()
    //        => this / Length;
    //    /// <summary>
    //    /// Normalizes this vector in-place.
    //    /// </summary>
    //    public void Normalize()
    //        => this /= Length;
    //    /// <summary>
    //    /// Returns a normalized version of the vector.
    //    /// </summary>
    //    /// <param name="v"></param>
    //    /// <returns></returns>
    //    public static Vec3 Normalize(Vec3 v)
    //        => v.Normalized();

    //    /// <summary>
    //    /// Returns the cross product of the two vectors.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 Cross(Vec3 a, Vec3 b)
    //        => new(
    //            a.y * b.z - a.z * b.y,
    //            a.z * b.x - a.x * b.z,
    //            a.x * b.y - a.y * b.x
    //        );

    //    /// <summary>
    //    /// Returns the dot product of the two vectors.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static float Dot(Vec3 a, Vec3 b)
    //        => a.x * b.x + a.y * b.y + a.z * b.z;

    //    public float Dot(Vec3 other)
    //        => this | other;

    //    public static Vec3 ComponentMin(Vec3 left, Vec3 right)
    //        => new Vec3(
    //            MathF.Min(left.x, right.x),
    //            MathF.Min(left.y, right.y),
    //            MathF.Min(left.z, right.z));

    //    public static Vec3 ComponentMax(Vec3 left, Vec3 right)
    //        => new Vec3(
    //            MathF.Max(left.x, right.x),
    //            MathF.Max(left.y, right.y),
    //            MathF.Max(left.z, right.z));

    //    public Vec3 Clamped(Vec3 min, Vec3 max)
    //        => new Vec3(
    //            x.Clamp(min.x, max.x),
    //            y.Clamp(min.y, max.y),
    //            z.Clamp(min.z, max.z));

    //    public float DistanceTo(Vec3 other)
    //        => (other - this).Length;
    //    public float DistanceToSquared(Vec3 other)
    //        => (other - this).LengthSquared;

    //    /// <summary>
    //    /// Subtracts each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator -(Vec3 a, Vec3 b)
    //        => new(a.x - b.x, a.y - b.y, a.z - b.z);
    //    /// <summary>
    //    /// Adds each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator +(Vec3 a, Vec3 b)
    //        => new(a.x + b.x, a.y + b.y, a.z + b.z);
    //    /// <summary>
    //    /// Divides each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator /(Vec3 a, Vec3 b)
    //        => new(a.x / b.x, a.y / b.y, a.z / b.z);
    //    /// <summary>
    //    /// Multiplies each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator *(Vec3 a, Vec3 b)
    //        => new(a.x * b.x, a.y * b.y, a.z * b.z);
    //    /// <summary>
    //    /// Returns the cross product of the two vectors.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator ^(Vec3 a, Vec3 b)
    //        => Cross(a, b);
    //    /// <summary>
    //    /// Returns the dot product of the two vectors.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static float operator |(Vec3 a, Vec3 b)
    //        => Dot(a, b);
    //    /// <summary>
    //    /// Subtracts from each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator -(Vec3 a, float b)
    //        => new(a.x - b, a.y - b, a.z - b);
    //    /// <summary>
    //    /// Sets each component to the result of 'a' subtracted by that component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator -(float a, Vec3 b)
    //        => new(a - b.x, a - b.y, a - b.z);
    //    /// <summary>
    //    /// Adds to each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator +(Vec3 a, float b)
    //        => new(a.x + b, a.y + b, a.z + b);
    //    /// <summary>
    //    /// Divides each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator /(Vec3 a, float b)
    //        => new(a.x / b, a.y / b, a.z / b);
    //    /// <summary>
    //    /// Sets each component to the quotient of 'a' divided by that component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator /(float a, Vec3 b)
    //        => new(a / b.x, a / b.y, a / b.z);
    //    /// <summary>
    //    /// Multiplies each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator *(Vec3 a, float b)
    //        => new(a.x * b, a.y * b, a.z * b);
    //    /// <summary>
    //    /// Multiplies each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator *(float a, Vec3 b)
    //        => new(a * b.x, a * b.y, a * b.z);
    //    /// <summary>
    //    /// Negates each component.
    //    /// </summary>
    //    /// <param name="v"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator -(Vec3 v)
    //        => new(-v.x, -v.y, -v.z);
    //    /// <summary>
    //    /// Normalizes the vector.
    //    /// </summary>
    //    /// <param name="v"></param>
    //    /// <returns></returns>
    //    public static Vec3 operator ~(Vec3 v)
    //        => v.Normalized();

    //    public static implicit operator System.Numerics.Vector3(Vec3 v)
    //        => new System.Numerics.Vector3(v.x, v.y, v.z);
    //    public static implicit operator Vec3(System.Numerics.Vector3 v)
    //        => new Vec3(v.X, v.Y, v.Z);
    //}
}
