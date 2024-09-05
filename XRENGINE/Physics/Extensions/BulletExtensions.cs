//namespace Extensions
//{
//    public static class BulletExtensions
//    {
//        public static BulletSharp.Math.Matrix ToBullet(this System.Numerics.Matrix4x4 matrix)
//        {
//            return new BulletSharp.Math.Matrix(
//                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
//                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
//                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
//                matrix.M41, matrix.M42, matrix.M43, matrix.M44);
//        }

//        public static System.Numerics.Matrix4x4 ToNumerics(this BulletSharp.Math.Matrix matrix)
//        {
//            return new System.Numerics.Matrix4x4(
//                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
//                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
//                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
//                matrix.M41, matrix.M42, matrix.M43, matrix.M44);
//        }

//        public static BulletSharp.Math.Vector3 ToBullet(this System.Numerics.Vector3 vector)
//        {
//            return new BulletSharp.Math.Vector3(vector.X, vector.Y, vector.Z);
//        }

//        public static System.Numerics.Vector3 ToNumerics(this BulletSharp.Math.Vector3 vector)
//        {
//            return new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
//        }

//        public static BulletSharp.Math.Quaternion ToBullet(this System.Numerics.Quaternion quaternion)
//        {
//            return new BulletSharp.Math.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
//        }

//        public static System.Numerics.Quaternion ToNumerics(this BulletSharp.Math.Quaternion quaternion)
//        {
//            return new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
//        }

//        public static BulletSharp.Math.Vector3 ToBullet(this System.Numerics.Vector4 vector)
//        {
//            return new BulletSharp.Math.Vector3(vector.X, vector.Y, vector.Z);
//        }
//    }
//}
