using System.Numerics;
using XREngine.Data.Geometry;

namespace XREngine.Data.BSP
{
    public class BSPNode
    {
        public System.Numerics.Plane? Plane;
        public BSPNode? Front;
        public BSPNode? Back;
        public List<Triangle> Triangles;

        public BSPNode()
            => Triangles = [];

        public BSPNode(List<Triangle> triangles)
            => Triangles = triangles;

        public void Build(List<Triangle> triangles)
        {
            if (triangles.Count == 0)
                return;

            Plane ??= triangles[0].GetPlane();

            List<Triangle> front = [];
            List<Triangle> back = [];

            foreach (Triangle triangle in triangles)
                SplitTriangle(Plane.Value, triangle, Triangles, front, back);
            
            if (front.Count > 0)
            {
                Front ??= new BSPNode();
                Front.Build(front);
            }

            if (back.Count > 0)
            {
                Back ??= new BSPNode();
                Back.Build(back);
            }
        }

        private void SplitTriangle(Plane value, Triangle triangle, List<Triangle>? triangles, List<Triangle>? front, List<Triangle>? back)
        {
            int[] sides = GetSides(triangle, value);

            if (sides[0] == 0 && sides[1] == 0 && sides[2] == 0)
            {
                triangles?.Add(triangle);
                return;
            }

            if (sides[0] == 1 && sides[1] == 1 && sides[2] == 1)
            {
                front?.Add(triangle);
                return;
            }

            if (sides[0] == -1 && sides[1] == -1 && sides[2] == -1)
            {
                back?.Add(triangle);
                return;
            }

            Triangle[] split = Split(triangle, value);

            if (sides[0] == 0)
                triangles?.Add(split[0]);
            else if (sides[0] == 1)
                front?.Add(split[0]);
            else
                back?.Add(split[0]);

            if (sides[1] == 0)
                triangles?.Add(split[1]);
            else if (sides[1] == 1)
                front?.Add(split[1]);
            else
                back?.Add(split[1]);

            if (sides[2] == 0)
                triangles?.Add(split[2]);
            else if (sides[2] == 1)
                front?.Add(split[2]);
            else
                back?.Add(split[2]);
        }

        private static int[] GetSides(Triangle triangle, System.Numerics.Plane value)
        {
            int[] sides = new int[3];

            for (int i = 0; i < 3; i++)
                sides[i] = GetSide(value, GetPoints(triangle)[i]);
            
            return sides;
        }

        private static int GetSide(System.Numerics.Plane value, Vector3 point)
        {
            float distance = GetDistance(value, point);

            if (distance < -0.01f)
                return -1;
            if (distance > 0.01f)
                return 1;
            
            return 0;
        }

        private static float GetDistance(System.Numerics.Plane value, Vector3 point)
            => Vector3.Dot(value.Normal, point) + value.D;

        private static Vector3[] GetPoints(Triangle triangle)
            => [triangle.A, triangle.B, triangle.C];

        private Triangle[] Split(Triangle triangle, System.Numerics.Plane value)
        {
            Vector3[] points = GetPoints(triangle);
            Vector3[] intersections = new Vector3[3];
            int[] sides = GetSides(triangle, value);

            for (int i = 0; i < 3; i++)
            {
                if (sides[i] == 0)
                {
                    intersections[i] = points[i];
                    continue;
                }

                int next = (i + 1) % 3;
                int prev = (i + 2) % 3;

                intersections[i] = GetIntersection(value, points[i], points[next]);
                Triangle newTriangle = new(points[next], intersections[i], points[prev]);

                if (sides[i] == 1)
                    Front?.SplitTriangle(value, newTriangle, Triangles, Front.Triangles, Front.Triangles);
                else
                    Back?.SplitTriangle(value, newTriangle, Triangles, Back.Triangles, Back.Triangles);
            }

            return
            [
                new Triangle(points[0], intersections[0], intersections[2]),
                new Triangle(points[1], intersections[1], intersections[0]),
                new Triangle(points[2], intersections[2], intersections[1])
            ];
        }

        private static Vector3 GetIntersection(System.Numerics.Plane value, Vector3 vector31, Vector3 vector32)
        {
            Vector3 direction = vector32 - vector31;
            float distance = GetDistance(value, vector31);
            float dot = Vector3.Dot(value.Normal, direction);

            if (dot == 0)
                return vector31;
            
            float t = -distance / dot;
            return vector31 + direction * t;
        }

        public void Invert()
        {
            Plane = Flip(Plane);
            Triangles.ForEach(t => t.Flip());

            Front?.Invert();
            Back?.Invert();

            (Back, Front) = (Front, Back);
        }

        private static System.Numerics.Plane? Flip(System.Numerics.Plane? plane)
        {
            if (plane is null)
                return null;

            return new System.Numerics.Plane(-plane.Value.Normal, -plane.Value.D);
        }

        public void ClipTo(BSPNode node)
        {
            if (Triangles.Count == 0)
                return;

            Triangles = node.ClipTriangles(Triangles);
            Front?.ClipTo(node);
            Back?.ClipTo(node);
        }

        public List<Triangle> ClipTriangles(List<Triangle> triangles)
        {
            if (Plane == null)
                return new List<Triangle>(triangles);

            List<Triangle> front = [];
            List<Triangle> back = [];

            foreach (Triangle triangle in triangles)
                SplitTriangle(Plane.Value, triangle, null, front, back);
            
            if (Front != null)
                front = Front.ClipTriangles(front);
            if (Back != null)
                back = Back.ClipTriangles(back);

            front.AddRange(back);
            return front;
        }

        public void GetAllTriangles(List<Triangle> triangles)
        {
            triangles.AddRange(Triangles);
            Front?.GetAllTriangles(triangles);
            Back?.GetAllTriangles(triangles);
        }

        public BSPNode Clone()
        {
            BSPNode cloneNode = new();

            if (Plane != null)
                cloneNode.Plane = new System.Numerics.Plane(Plane.Value.Normal, Plane.Value.D);
            
            if (Front != null)
                cloneNode.Front = Front.Clone();
            
            if (Back != null)
                cloneNode.Back = Back.Clone();
            
            foreach (Triangle triangle in Triangles)
                cloneNode.Triangles.Add(new Triangle(triangle.A, triangle.B, triangle.C));
            
            return cloneNode;
        }
    }
}