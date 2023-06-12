using System.Runtime.InteropServices;
using XREngine.Data.Transforms.Rotations;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Transforms
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Matrix
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;

        public Vec4 Column0
        {
            get => new(m00, m01, m02, m03);
            set
            {
                m00 = value.x;
                m01 = value.y;
                m02 = value.z;
                m03 = value.w;
            }
        }

        public Vec4 Column1
        {
            get => new(m10, m11, m12, m13);
            set
            {
                m10 = value.x;
                m11 = value.y;
                m12 = value.z;
                m13 = value.w;
            }
        }

        public Vec4 Column2
        {
            get => new(m20, m21, m22, m23);
            set
            {
                m20 = value.x;
                m21 = value.y;
                m22 = value.z;
                m23 = value.w;
            }
        }

        public Vec4 Column3
        {
            get => new(m30, m31, m32, m33);
            set
            {
                m30 = value.x;
                m31 = value.y;
                m32 = value.z;
                m33 = value.w;
            }
        }

        public Vec3 Right
        {
            get => new(m00, m01, m02);
            set
            {
                m00 = value.x;
                m01 = value.y;
                m02 = value.z;
            }
        }

        public Vec3 Up
        {
            get => new(m10, m11, m12);
            set
            {
                m10 = value.x;
                m11 = value.y;
                m12 = value.z;
            }
        }

        public Vec3 Forward
        {
            get => new(m20, m21, m22);
            set
            {
                m20 = value.x;
                m21 = value.y;
                m22 = value.z;
            }
        }

        public Vec3 Translation
        {
            get => new(m03, m13, m23);
            set
            {
                m03 = value.x;
                m13 = value.y;
                m23 = value.z;
            }
        }

        public Vec3 Scale
        {
            get => new(m00, m11, m22);
            set
            {
                m00 = value.x;
                m11 = value.y;
                m22 = value.z;
            }
        }

        public Matrix(
            float m00, float m01, float m02, float m03,
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;
            this.m03 = m03;

            this.m10 = m10;
            this.m11 = m11;
            this.m12 = m12;
            this.m13 = m13;

            this.m20 = m20;
            this.m21 = m21;
            this.m22 = m22;
            this.m23 = m23;

            this.m30 = m30;
            this.m31 = m31;
            this.m32 = m32;
            this.m33 = m33;
        }

        public static Matrix Zero => new(
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0);

        public static Matrix Identity => new(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);

        public unsafe float* Address
        {
            get
            {
                fixed (void* ptr = &this) { return (float*)ptr; }
            }
        }

        public unsafe float this[int index]
        {
            get => Address[index];
            set => Address[index] = value;
        }

        public static Matrix operator *(Matrix lhs, Matrix rhs)
        {
            Matrix result = new();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    result[i, j] =
                        lhs[i, 0] * rhs[0, j] +
                        lhs[i, 1] * rhs[1, j] +
                        lhs[i, 2] * rhs[2, j] +
                        lhs[i, 3] * rhs[3, j];

            return result;
        }

        public static Matrix CreateTranslation(Vec3 translation) => new()
        {
            m00 = 1.0f,
            m01 = 0.0f,
            m02 = 0.0f,
            m03 = translation.x,
            m10 = 0.0f,
            m11 = 1.0f,
            m12 = 0.0f,
            m13 = translation.y,
            m20 = 0.0f,
            m21 = 0.0f,
            m22 = 1.0f,
            m23 = translation.z,
            m30 = 0.0f,
            m31 = 0.0f,
            m32 = 0.0f,
            m33 = 1.0f
        };

        public static Matrix CreateRotation(Quat rotation)
        {
            float xx = rotation.x * rotation.x;
            float yy = rotation.y * rotation.y;
            float zz = rotation.z * rotation.z;
            float xy = rotation.x * rotation.y;
            float xz = rotation.x * rotation.z;
            float yz = rotation.y * rotation.z;
            float wx = rotation.w * rotation.x;
            float wy = rotation.w * rotation.y;
            float wz = rotation.w * rotation.z;

            return new Matrix
            (
                1 - 2 * (yy + zz), 2 * (xy - wz), 2 * (xz + wy), 0,
                2 * (xy + wz), 1 - 2 * (xx + zz), 2 * (yz - wx), 0,
                2 * (xz - wy), 2 * (yz + wx), 1 - 2 * (xx + yy), 0,
                0, 0, 0, 1
            );
        }

        public static Matrix CreateScale(Vec3 scale) => new()
        {
            m00 = scale.x,
            m01 = 0.0f,
            m02 = 0.0f,
            m03 = 0.0f,
            m10 = 0.0f,
            m11 = scale.y,
            m12 = 0.0f,
            m13 = 0.0f,
            m20 = 0.0f,
            m21 = 0.0f,
            m22 = scale.z,
            m23 = 0.0f,
            m30 = 0.0f,
            m31 = 0.0f,
            m32 = 0.0f,
            m33 = 1.0f
        };

        public Matrix Inverted()
        {
            // Calculate the inverse using Cramer's rule and adjugate matrix
            // This implementation assumes the matrix is invertible
            // If the determinant is zero, the inverse does not exist
            float determinant = Determinant();
            if (Math.Abs(determinant) < 1e-6)
                throw new InvalidOperationException("Matrix is not invertible.");

            return Adjugate() / determinant;
        }

        public bool Inverted(out Matrix inverted)
        {
            // Calculate the inverse using Cramer's rule and adjugate matrix
            // This implementation assumes the matrix is invertible
            // If the determinant is zero, the inverse does not exist
            float determinant = Determinant();
            if (Math.Abs(determinant) < 1e-6)
            {
                inverted = Identity;
                return false;
            }
            inverted = Adjugate() / determinant;
            return true;
        }

        public static Matrix operator *(Matrix matrix, float scalar)
        {
            Matrix result = new();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    result[i, j] = matrix[i, j] * scalar;
            return result;
        }

        public static Matrix operator /(Matrix matrix, float scalar)
        {
            return matrix * (1.0f / scalar);
            //Matrix result = new();
            //for (int i = 0; i < 4; i++)
            //    for (int j = 0; j < 4; j++)
            //        result[i, j] = matrix[i, j] / scalar;
            //return result;
        }

        public static Matrix LookAt(Vec3 eye, Vec3 target, Vec3 up)
        {
            Vec3 zAxis = ~(eye - target);
            Vec3 xAxis = ~(up ^ zAxis);
            Vec3 yAxis = zAxis ^ xAxis;

            return new Matrix
            {
                m00 = xAxis.x,
                m01 = yAxis.x,
                m02 = zAxis.x,
                m03 = -(xAxis | eye),
                m10 = xAxis.y,
                m11 = yAxis.y,
                m12 = zAxis.y,
                m13 = -(yAxis | eye),
                m20 = xAxis.z,
                m21 = yAxis.z,
                m22 = zAxis.z,
                m23 = -(zAxis | eye),
                m30 = 0.0f,
                m31 = 0.0f,
                m32 = 0.0f,
                m33 = 1.0f
            };
        }

        public float Determinant()
        {
            float det;
            det = m00 * (m11 * m22 * m33 + m12 * m23 * m21 + m13 * m20 * m23 - m13 * m22 * m21 - m12 * m20 * m33 - m11 * m23 * m32);
            det -= m01 * (m10 * m22 * m33 + m12 * m23 * m20 + m13 * m21 * m23 - m13 * m22 * m20 - m12 * m21 * m33 - m10 * m23 * m32);
            det += m02 * (m10 * m21 * m33 + m11 * m23 * m20 + m13 * m21 * m30 - m13 * m20 * m31 - m11 * m21 * m33 - m10 * m23 * m31);
            det -= m03 * (m10 * m21 * m32 + m11 * m22 * m30 + m12 * m20 * m31 - m12 * m21 * m30 - m11 * m20 * m32 - m10 * m22 * m31);
            return det;
        }

        public Matrix Adjugate() => new()
        {
            m00 = m11 * m22 * m33 + m12 * m23 * m31 + m13 * m21 * m32 - m13 * m22 * m31 - m12 * m21 * m33 - m11 * m23 * m32,
            m01 = m01 * m22 * m33 + m02 * m23 * m31 + m03 * m21 * m32 - m03 * m22 * m31 - m02 * m21 * m33 - m01 * m23 * m32,
            m02 = m01 * m12 * m33 + m02 * m13 * m31 + m03 * m11 * m32 - m03 * m12 * m31 - m02 * m11 * m33 - m01 * m13 * m32,
            m03 = m01 * m12 * m23 + m02 * m13 * m21 + m03 * m11 * m22 - m03 * m12 * m21 - m02 * m11 * m23 - m01 * m13 * m22,

            m10 = m10 * m22 * m33 + m12 * m23 * m30 + m13 * m20 * m32 - m13 * m22 * m30 - m12 * m20 * m33 - m10 * m23 * m32,
            m11 = m00 * m22 * m33 + m02 * m23 * m30 + m03 * m20 * m32 - m03 * m22 * m30 - m02 * m20 * m33 - m00 * m23 * m32,
            m12 = m00 * m12 * m33 + m02 * m13 * m30 + m03 * m10 * m32 - m03 * m12 * m30 - m02 * m10 * m33 - m00 * m13 * m32,
            m13 = m00 * m12 * m23 + m02 * m13 * m20 + m03 * m10 * m22 - m03 * m12 * m20 - m02 * m10 * m23 - m00 * m13 * m22,

            m20 = m10 * m21 * m33 + m11 * m23 * m30 + m13 * m20 * m31 - m13 * m21 * m30 - m11 * m20 * m33 - m10 * m23 * m31,
            m21 = m00 * m21 * m33 + m01 * m23 * m30 + m03 * m20 * m31 - m03 * m21 * m30 - m01 * m20 * m33 - m00 * m23 * m31,
            m22 = m00 * m11 * m33 + m01 * m13 * m30 + m03 * m10 * m31 - m03 * m11 * m30 - m01 * m10 * m33 - m00 * m13 * m31,
            m23 = m00 * m11 * m23 + m01 * m13 * m20 + m03 * m10 * m22 - m03 * m11 * m20 - m01 * m10 * m23 - m00 * m13 * m22,

            m30 = m10 * m21 * m32 + m11 * m22 * m30 + m12 * m20 * m31 - m12 * m21 * m30 - m11 * m20 * m32 - m10 * m22 * m31,
            m31 = m00 * m21 * m32 + m01 * m22 * m30 + m02 * m20 * m31 - m02 * m21 * m30 - m01 * m20 * m32 - m00 * m22 * m31,
            m32 = m00 * m11 * m32 + m01 * m12 * m30 + m02 * m10 * m31 - m02 * m11 * m30 - m01 * m10 * m32 - m00 * m12 * m31,
            m33 = m00 * m11 * m22 + m01 * m12 * m20 + m02 * m10 * m21 - m02 * m11 * m20 - m01 * m10 * m22 - m00 * m12 * m21
        };

        public Matrix Transpose() => new()
        {
            m00 = m00,
            m01 = m10,
            m02 = m20,
            m03 = m30,
            m10 = m01,
            m11 = m11,
            m12 = m21,
            m13 = m31,
            m20 = m02,
            m21 = m12,
            m22 = m22,
            m23 = m32,
            m30 = m03,
            m31 = m13,
            m32 = m23,
            m33 = m33
        };

        public static Matrix YUpToZUp()
            => new(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, -1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );

        public static Matrix ZUpToYUp()
            => new(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );

        public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
        {
            if (fieldOfView <= 0.0f || fieldOfView >= Math.PI)
                throw new ArgumentOutOfRangeException(nameof(fieldOfView), "fieldOfView must be in the range (0, PI).");

            if (nearPlane <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(nearPlane), "nearPlane must be greater than 0.");

            if (farPlane <= nearPlane)
                throw new ArgumentOutOfRangeException(nameof(farPlane), "farPlane must be greater than nearPlane.");

            float f = 1.0f / (float)Math.Tan(fieldOfView / 2.0f);
            float range = nearPlane - farPlane;

            return new(

                f / aspectRatio,
                0.0f,
                0.0f,
                0.0f,

                0.0f,
                f,
                0.0f,
                0.0f,

                0.0f,
                0.0f,
                (nearPlane + farPlane) / range,
                2.0f * nearPlane * farPlane / range,

                0.0f,
                0.0f,
                -1.0f,
                0.0f
            );
        }

        public static Matrix CreateOrthographic(float width, float height, float nearPlane, float farPlane)
        {
            if (nearPlane == farPlane)
                throw new ArgumentException("nearPlane and farPlane cannot be equal.");

            return new(

                2.0f / width,
                0.0f,
                0.0f,
                0.0f,

                0.0f,
                2.0f / height,
                0.0f,
                0.0f,

                0.0f,
                0.0f,
                1.0f / (nearPlane - farPlane),
                nearPlane / (nearPlane - farPlane),

                0.0f,
                0.0f,
                0.0f,
                1.0f
            );
        }

        public static Matrix CreateLookAt(Vec3 cameraPosition, Vec3 cameraTarget, Vec3 cameraUpVector)
        {
            Vec3 forward = ~(cameraPosition - cameraTarget);
            Vec3 right = ~(cameraUpVector ^ forward);
            Vec3 up = forward ^ right;

            Matrix rotation = new(
                right.x, up.x, forward.x, 0,
                right.y, up.y, forward.y, 0,
                right.z, up.z, forward.z, 0,
                0, 0, 0, 1
            );

            Matrix translation = new(
                1, 0, 0, -cameraPosition.x,
                0, 1, 0, -cameraPosition.y,
                0, 0, 1, -cameraPosition.z,
                0, 0, 0, 1
            );

            return rotation * translation;
        }

        public unsafe float this[int row, int column]
        {
            get
            {
                //if (row < 0 || row > 3 || column < 0 || column > 3)
                //    throw new ArgumentOutOfRangeException();

                fixed (float* ptr = &m00)
                {
                    return ptr[row * 4 + column];
                }
            }
            set
            {
                //if (row < 0 || row > 3 || column < 0 || column > 3)
                //    throw new ArgumentOutOfRangeException();

                fixed (float* ptr = &m00)
                {
                    ptr[row * 4 + column] = value;
                }
            }
        }
    }
}
