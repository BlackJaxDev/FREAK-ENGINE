﻿namespace XREngine.Data.Transforms.Vectors
{
    //[StructLayout(LayoutKind.Sequential, Pack = 0)]
    //public unsafe struct Vector4
    //{
    //    public float x;
    //    public float w;
    //    public float z;
    //    public float y;

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

    //    public Vector4(float v)
    //    {
    //        x = v;
    //        y = v;
    //        z = v;
    //        w = v;
    //    }
    //    public Vector4(float x, float y, float z, float w)
    //    {
    //        this.x = x;
    //        this.y = y;
    //        this.z = z;
    //        this.w = w;
    //    }

    //    /// <summary>
    //    /// Returns the length (aka magnitude) of the vector, but without taking the square root.
    //    /// </summary>
    //    public float LengthSquared => this | this;
    //    /// <summary>
    //    /// Returns the length (aka magnitude) of the vector.
    //    /// </summary>
    //    public float Length => (float)Math.Sqrt(LengthSquared);

    //    public static Vector4 UnitW { get; } = new(0.0f, 0.0f, 0.0f, 1.0f);
    //    public static Vector4 UnitZ { get; } = new(0.0f, 0.0f, 1.0f, 0.0f);
    //    public static Vector4 UnitY { get; } = new(0.0f, 1.0f, 0.0f, 0.0f);
    //    public static Vector4 UnitX { get; } = new(1.0f, 0.0f, 0.0f, 0.0f);
    //    public static Vector4 Zero { get; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    //    public static Vector4 One { get; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    //    public static Vector4 Max { get; } = new(float.MaxValue);
    //    public static Vector4 Min { get; } = new(float.MinValue);

    //    public float W { get => w; set => w = value; }
    //    public float Z { get => z; set => z = value; }
    //    public float Y { get => y; set => y = value; }
    //    public float X { get => x; set => x = value; }

    //    /// <summary>
    //    /// Returns a normalized version of this vector.
    //    /// </summary>
    //    /// <returns></returns>
    //    public Vector4 Normalized()
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
    //    public static Vector4 Normalize(Vector4 v)
    //        => v.Normalized();

    //    /// <summary>
    //    /// Returns the dot product of the two vectors.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static float Dot(Vector4 a, Vector4 b)
    //        => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

    //    /// <summary>
    //    /// Subtracts each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator -(Vector4 a, Vector4 b)
    //        => new(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    //    /// <summary>
    //    /// Adds each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator +(Vector4 a, Vector4 b)
    //        => new(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    //    /// <summary>
    //    /// Divides each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator /(Vector4 a, Vector4 b)
    //        => new(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);
    //    /// <summary>
    //    /// Multiplies each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator *(Vector4 a, Vector4 b)
    //        => new(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
    //    /// <summary>
    //    /// Returns the dot product of the two vectors.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static float operator |(Vector4 a, Vector4 b)
    //        => Dot(a, b);
    //    /// <summary>
    //    /// Subtracts from each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator -(Vector4 a, float b)
    //        => new(a.x - b, a.y - b, a.z - b, a.w - b);
    //    /// <summary>
    //    /// Sets each component to the result of 'a' subtracted by that component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator -(float a, Vector4 b)
    //        => new(a - b.x, a - b.y, a - b.z, a - b.w);
    //    /// <summary>
    //    /// Adds to each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator +(Vector4 a, float b)
    //        => new(a.x + b, a.y + b, a.z + b, a.w + b);
    //    /// <summary>
    //    /// Divides each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator /(Vector4 a, float b)
    //        => new(a.x / b, a.y / b, a.z / b, a.w / b);
    //    /// <summary>
    //    /// Sets each component to the quotient of 'a' divided by that component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator /(float a, Vector4 b)
    //        => new(a / b.x, a / b.y, a / b.z, a / b.w);
    //    /// <summary>/// Multiplies each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator *(Vector4 a, float b)
    //        => new(a.x * b, a.y * b, a.z * b, a.w * b);
    //    /// <summary>
    //    /// Multiplies each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator *(float a, Vector4 b)
    //        => new(a * b.x, a * b.y, a * b.z, a * b.w);
    //    /// <summary>
    //    /// Negates each component.
    //    /// </summary>
    //    /// <param name="v"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator -(Vector4 v)
    //        => new(-v.x, -v.y, -v.z, -v.w);
    //    /// <summary>
    //    /// Normalizes the vector.
    //    /// </summary>
    //    /// <param name="v"></param>
    //    /// <returns></returns>
    //    public static Vector4 operator ~(Vector4 v)
    //        => v.Normalized();

    //    public static implicit operator System.Numerics.Vector4(Vector4 v)
    //        => new System.Numerics.Vector4(v.x, v.y, v.z, v.w);
    //    public static implicit operator Vector4(System.Numerics.Vector4 v)
    //        => new Vector4(v.X, v.Y, v.Z, v.W);
    //}
}
