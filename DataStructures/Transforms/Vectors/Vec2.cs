namespace XREngine.Data.Transforms.Vectors
{
    //public struct Vec2
    //{
    //    public float x;
    //    public float y;

    //    public Vec2(float v)
    //    {
    //        x = v;
    //        y = v;
    //    }
    //    public Vec2(float x, float y)
    //    {
    //        this.x = x;
    //        this.y = y;
    //    }

    //    /// <summary>
    //    /// Returns the length (aka magnitude) of the vector, but without taking the square root.
    //    /// </summary>
    //    public float LengthSquared => this | this;
    //    /// <summary>
    //    /// Returns the length (aka magnitude) of the vector.
    //    /// </summary>
    //    public float Length => (float)Math.Sqrt(LengthSquared);

    //    public static Vec2 UnitY { get; } = new(0.0f, 1.0f);
    //    public static Vec2 UnitX { get; } = new(1.0f, 0.0f);
    //    public static Vec2 Zero { get; } = new(0.0f, 0.0f);
    //    public static Vec2 One { get; } = new(1.0f, 1.0f);
    //    public static Vec2 Max { get; } = new(float.MaxValue);
    //    public static Vec2 Min { get; } = new(float.MinValue);

    //    public float X { get => x; set => x = value; }
    //    public float Y { get => y; set => y = value; }

    //    /// <summary>
    //    /// Returns a normalized version of this vector.
    //    /// </summary>
    //    /// <returns></returns>
    //    public Vec2 Normalized()
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
    //    public static Vec2 Normalize(Vec2 v)
    //        => v.Normalized();

    //    /// <summary>
    //    /// Returns the dot product of the two vectors.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static float Dot(Vec2 a, Vec2 b)
    //        => a.x * b.x + a.y * b.y;

    //    /// <summary>
    //    /// Subtracts each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator -(Vec2 a, Vec2 b)
    //        => new(a.x - b.x, a.y - b.y);
    //    /// <summary>
    //    /// Adds each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator +(Vec2 a, Vec2 b)
    //        => new(a.x + b.x, a.y + b.y);
    //    /// <summary>
    //    /// Divides each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator /(Vec2 a, Vec2 b)
    //        => new(a.x / b.x, a.y / b.y);
    //    /// <summary>
    //    /// Multiplies each component individually.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator *(Vec2 a, Vec2 b)
    //        => new(a.x * b.x, a.y * b.y);
    //    /// <summary>
    //    /// Returns the dot product of the two vectors.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static float operator |(Vec2 a, Vec2 b)
    //        => Dot(a, b);
    //    /// <summary>
    //    /// Subtracts from each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator -(Vec2 a, float b)
    //        => new(a.x - b, a.y - b);
    //    /// <summary>
    //    /// Adds to each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator +(Vec2 a, float b)
    //        => new(a.x + b, a.y + b);
    //    /// <summary>
    //    /// Divides each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator /(Vec2 a, float b)
    //        => new(a.x / b, a.y / b);
    //    /// <summary>
    //    /// Multiplies each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator *(Vec2 a, float b)
    //        => new(a.x * b, a.y * b);
    //    /// <summary>
    //    /// Multiplies each component.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator *(float a, Vec2 b)
    //        => new(b.x * a, b.y * a);
    //    /// <summary>
    //    /// Normalizes the vector.
    //    /// </summary>
    //    /// <param name="v"></param>
    //    /// <returns></returns>
    //    public static Vec2 operator ~(Vec2 v)
    //        => v.Normalized();

    //    public static implicit operator System.Numerics.Vector2(Vec2 v)
    //        => new System.Numerics.Vector2(v.x, v.y);
    //    public static implicit operator Vec2(System.Numerics.Vector2 v)
    //        => new Vec2(v.X, v.Y);
    //}
}
