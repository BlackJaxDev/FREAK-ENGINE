using System.Numerics;

namespace XREngine
{
    public static class Globals
    {
        public static Vector3 Up = Vector3.UnitY;
        public static Vector3 Down = -Vector3.UnitY;
        public static Vector3 Left = -Vector3.UnitX;
        public static Vector3 Right = Vector3.UnitX;
        public static Vector3 Forward = -Vector3.UnitZ;
        public static Vector3 Backward = Vector3.UnitZ;

        public static class Min
        {
            public static Vector2 Vector2 = new Vector2(float.MinValue);
            public static Vector3 Vector3 = new Vector3(float.MinValue);
            public static Vector4 Vector4 = new Vector4(float.MinValue);
        }
        public static class Max
        {
            public static Vector2 Vector2 = new Vector2(float.MaxValue);
            public static Vector3 Vector3 = new Vector3(float.MaxValue);
            public static Vector4 Vector4 = new Vector4(float.MaxValue);
        }
    }
}
