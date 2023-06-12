using XREngine.Data.Geometry;

namespace XREngine.Data.BSP
{
    public class BSPNode
    {
        public Plane? Plane;
        public BSPNode? Front;
        public BSPNode? Back;
        public List<Triangle> Triangles;

        public BSPNode()
            => Triangles = new List<Triangle>();

        public BSPNode(List<Triangle> triangles)
            => Triangles = triangles;

        public void Build(List<Triangle> triangles)
        {
            if (triangles.Count == 0)
                return;

            Plane ??= triangles[0].GetPlane();

            List<Triangle> front = new();
            List<Triangle> back = new();

            foreach (Triangle triangle in triangles)
                Plane.Value.SplitTriangle(triangle, Triangles, front, back);
            
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

        public void Invert()
        {
            Plane = Plane?.Flipped();
            Triangles.ForEach(t => t.Flip());

            Front?.Invert();
            Back?.Invert();

            (Back, Front) = (Front, Back);
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

            List<Triangle> front = new();
            List<Triangle> back = new();

            foreach (Triangle triangle in triangles)
                Plane.Value.SplitTriangle(triangle, null, front, back);
            
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
                cloneNode.Plane = new Plane(Plane.Value.Normal, Plane.Value.Distance);
            
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