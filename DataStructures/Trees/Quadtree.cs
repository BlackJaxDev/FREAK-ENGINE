using System.Runtime.InteropServices;
using XREngine.Data.Geometry;

namespace XREngine.Data.Trees
{
    public unsafe class Quadtree
    {
        public Node* Root;

        public Quadtree(float centerX, float centerZ, float size, int maxDepth)
        {
            Root = (Node*)Marshal.AllocHGlobal(sizeof(Node));
            *Root = new Node(centerX, centerZ, size, 0, maxDepth);
        }

        public void UpdateLOD(float cameraX, float cameraZ, float lodDistance)
        {
            UpdateLOD(Root, cameraX, cameraZ, lodDistance);
        }

        private void UpdateLOD(Node* node, float cameraX, float cameraZ, float lodDistance)
        {
            if (node == null)
                return;

            float xDiff = node->CenterX - cameraX;
            float zDiff = node->CenterZ - cameraZ;
            float distance = MathF.Sqrt(xDiff * xDiff + zDiff * zDiff);
            node->LOD = (int)(distance / lodDistance);

            for (int i = 0; i < 4; i++)
                UpdateLOD(node->GetChild(i), cameraX, cameraZ, lodDistance);
        }

        public List<Node> GetVisibleNodes(Frustum frustum)
        {
            List<Node> visibleNodes = new List<Node>();
            GetVisibleNodes(Root, frustum, visibleNodes);
            return visibleNodes;
        }

        private void GetVisibleNodes(Node* node, Frustum frustum, List<Node> visibleNodes)
        {
            if (node == null)
                return;

            BoundingBox boundingBox = new BoundingBox(
                node->CenterX - node->Size / 2, 0, node->CenterZ - node->Size / 2,
                node->CenterX + node->Size / 2, 1000, node->CenterZ + node->Size / 2
            );

            if (frustum.Intersects(boundingBox))
            {
                visibleNodes.Add(*node);

                for (int i = 0; i < 4; i++)
                    GetVisibleNodes(node->GetChild(i), frustum, visibleNodes);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Node
        {
            public float CenterX;
            public float CenterZ;
            public float Size;
            public int LOD;
            public int Depth;
            public IntPtr ChildrenPtr;

            public Node(float centerX, float centerZ, float size, int depth, int maxDepth)
            {
                CenterX = centerX;
                CenterZ = centerZ;
                Size = size;
                LOD = 0;
                Depth = depth;

                if (depth < maxDepth)
                {
                    ChildrenPtr = Marshal.AllocHGlobal(sizeof(Node) * 4);

                    float halfSize = size / 2;
                    float quarterSize = size / 4;

                    for (int i = 0; i < 4; i++)
                    {
                        float offsetX = (i % 2) == 0 ? -quarterSize : quarterSize;
                        float offsetZ = (i < 2) ? -quarterSize : quarterSize;

                        Node* childNode = (Node*)(ChildrenPtr + sizeof(Node) * i);
                        *childNode = new Node(centerX + offsetX, centerZ + offsetZ, halfSize, depth + 1, maxDepth);
                    }
                }
                else
                {
                    ChildrenPtr = IntPtr.Zero;
                }
            }
            public Node* GetChild(int index)
            {
                if (ChildrenPtr == IntPtr.Zero)
                    return null;

                return (Node*)(ChildrenPtr + sizeof(Node) * index);
            }
        }
    }
}
