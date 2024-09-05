using System.Numerics;
using System.Runtime.InteropServices;
using XREngine.Data.Geometry;

namespace XREngine.Data.Trees
{
    public unsafe class Quadtree2
    {
        public Node* Root;

        public Quadtree2(float centerX, float centerZ, float size, int maxDepth)
        {
            Root = (Node*)Marshal.AllocHGlobal(sizeof(Node));
            *Root = new Node(centerX, centerZ, size, 0, maxDepth);
        }

        public void UpdateLOD(float cameraX, float cameraZ, float lodDistance)
            => UpdateLOD(Root, cameraX, cameraZ, lodDistance);

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
            List<Node> visibleNodes = [];
            GetVisibleNodes(Root, frustum, visibleNodes);
            return visibleNodes;
        }

        private void GetVisibleNodes(Node* node, Frustum frustum, List<Node> visibleNodes)
        {
            if (node == null)
                return;

            AABB boundingBox = new(
                new Vector3(
                    node->CenterX - node->Size / 2,
                    0,
                    node->CenterZ - node->Size / 2),
                new Vector3(
                    node->CenterX + node->Size / 2,
                    1000,
                    node->CenterZ + node->Size / 2)
            );

            if (!frustum.Intersects(boundingBox))
                return;

            visibleNodes.Add(*node);

            for (int i = 0; i < 4; i++)
                GetVisibleNodes(node->GetChild(i), frustum, visibleNodes);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Node
        {
            public float CenterX;
            public float CenterZ;
            public float Size;
            public int LOD;
            public int Depth;
            public nint ChildrenPtr;

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
                        float offsetX = i % 2 == 0 ? -quarterSize : quarterSize;
                        float offsetZ = i < 2 ? -quarterSize : quarterSize;

                        Node* childNode = (Node*)(ChildrenPtr + sizeof(Node) * i);
                        *childNode = new Node(centerX + offsetX, centerZ + offsetZ, halfSize, depth + 1, maxDepth);
                    }
                }
                else
                {
                    ChildrenPtr = nint.Zero;
                }
            }

            public readonly Node* GetChild(int index)
                => ChildrenPtr == nint.Zero ? (Node*)null : (Node*)(ChildrenPtr + sizeof(Node) * index);
        }
    }
}
