using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct Sphere
    {
        public Vector3 Center;
        public float Radius;

        public Sphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public float Diameter
        {
            get => Radius * 2.0f;
            set => Radius = value / 2.0f;
        }

        public override string ToString()
        {
            return $"Sphere (Center: {Center}, Radius: {Radius})";
        }
    }
}
