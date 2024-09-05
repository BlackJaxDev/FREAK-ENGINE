using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        public Vector3 A = a;
        public Vector3 B = b;
        public Vector3 C = c;

        public readonly System.Numerics.Plane GetPlane()
            => System.Numerics.Plane.CreateFromVertices(A, B, C);

        public readonly Vector3 GetNormal()
            => Vector3.Normalize(Vector3.Cross(B - A, C - A));

        public void Flip()
            => (C, A) = (A, C);

        public readonly Vector3 ClosestPoint(Vector3 point)
        {
            Vector3 ab = B - A;
            Vector3 ac = C - A;
            Vector3 ap = point - A;

            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);

            if (d1 <= 0.0f && d2 <= 0.0f)
                return A;

            Vector3 bp = point - B;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);

            if (d3 >= 0.0f && d4 <= d3)
                return B;

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float x = d1 / (d1 - d3);
                return A + x * ab;
            }

            Vector3 cp = point - C;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);

            if (d6 >= 0.0f && d5 <= d6)
                return C;

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float x = d2 / (d2 - d6);
                return A + x * ac;
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float x = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return B + x * (C - B);
            }

            float denom = 1.0f / (va + vb + vc);
            float v = vb * denom;
            float w = vc * denom;
            return A + ab * v + ac * w;
        }
    }
}