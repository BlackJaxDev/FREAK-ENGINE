using System.Numerics;

namespace Extensions
{
    public static class MatrixExtension
    {
        public static Matrix4x4 Inverted(this Matrix4x4 matrix)
            => Matrix4x4.Invert(matrix, out Matrix4x4 result) ? result : Matrix4x4.Identity;

        public static Matrix4x4 Transposed(this Matrix4x4 matrix)
            => Matrix4x4.Transpose(matrix);

        public static Matrix4x4 Translate(this Matrix4x4 matrix, Vector3 translation)
        {
            matrix.M41 += translation.X;
            matrix.M42 += translation.Y;
            matrix.M43 += translation.Z;
            return matrix;
        }

        public static Quaternion FromMatrix(this Matrix4x4 matrix)
        {
            float trace = matrix.M11 + matrix.M22 + matrix.M33;
            float w, x, y, z;
            if (trace > 0)
            {
                float s = 0.5f / (float)Math.Sqrt(trace + 1.0f);
                w = 0.25f / s;
                x = (matrix.M32 - matrix.M23) * s;
                y = (matrix.M13 - matrix.M31) * s;
                z = (matrix.M21 - matrix.M12) * s;
            }
            else if (matrix.M11 > matrix.M22 && matrix.M11 > matrix.M33)
            {
                float s = 2.0f * (float)Math.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33);
                w = (matrix.M32 - matrix.M23) / s;
                x = 0.25f * s;
                y = (matrix.M12 + matrix.M21) / s;
                z = (matrix.M13 + matrix.M31) / s;
            }
            else if (matrix.M22 > matrix.M33)
            {
                float s = 2.0f * (float)Math.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33);
                w = (matrix.M13 - matrix.M31) / s;
                x = (matrix.M12 + matrix.M21) / s;
                y = 0.25f * s;
                z = (matrix.M23 + matrix.M32) / s;
            }
            else
            {
                float s = 2.0f * (float)Math.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22);
                w = (matrix.M21 - matrix.M12) / s;
                x = (matrix.M13 + matrix.M31) / s;
                y = (matrix.M23 + matrix.M32) / s;
                z = 0.25f * s;
            }
            return new Quaternion(x, y, z, w);
        }
    }
}
