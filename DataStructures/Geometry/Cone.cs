using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct Cone
    {
        public Vector3 Center;
        public Vector3 Up;
        public float Height;
        public float Radius;

        public Cone(Vector3 center, Vector3 up, float height, float radius)
        {
            Center = center;
            Up = up;
            Height = height;
            Radius = radius;
        }

        public float Diameter
        {
            get => Radius * 2.0f;
            set => Radius = value / 2.0f;
        }

        public override string ToString()
        {
            return $"Cone (Center: {Center}, Up: {Up}, Height: {Height}, Radius: {Radius})";
        }
    }
}
