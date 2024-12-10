//using System.Numerics;
//using XREngine.Data.Rendering;
//using XREngine.Rendering;

//namespace XREngine.Physics
//{
//    public static class ConvexDecomposition
//    {
//        public static XRCollisionCompoundShape Calculate(
//            IEnumerable<XRMesh> primitives,
//            int minClusterCount = 2,
//            int maxConcavity = 50,
//            int maxVerticesPerHull = 20,
//            double volumeWeight = 0.0,
//            double compacityWeight = 0.1,
//            bool addExtraDistPoints = false,
//            bool addNeighborsDistPoints = false,
//            bool addFacesPoints = false)
//        {
//            //TODO: finish hacd convex decomposition

//            Hacd hacd = new();
//            List<int> indices = [];
//            List<Vector3> points = [];
//            int[] meshIndices;
//            int baseIndexOffset = 0;
//            foreach (XRMesh primData in primitives)
//            {
//                Vector3[] array = [];
//                primData[ECommonBufferType.Positions]?.GetDataRaw(out array, false);
//                meshIndices = primData.GetIndices();
//                foreach (int i in meshIndices)
//                    indices.Add(i + baseIndexOffset);
//                baseIndexOffset += array.Length;
//                points.AddRange(array.Select(x => (Vector3)x));
//            }

//            //hacd.SetPoints(points);
//            hacd.SetTriangles(indices);
//            hacd.CompacityWeight = compacityWeight;
//            hacd.VolumeWeight = volumeWeight;
//            hacd.Callback = HacdUpdate;

//            // Recommended HACD parameters: 2 100 false false false
//            hacd.NClusters = minClusterCount; // minimum number of clusters
//            hacd.Concavity = maxConcavity; // maximum concavity
//            hacd.AddExtraDistPoints = addExtraDistPoints;
//            hacd.AddNeighboursDistPoints = addNeighborsDistPoints;
//            hacd.AddFacesPoints = addFacesPoints;
//            hacd.VerticesPerConvexHull = maxVerticesPerHull; // max of 100 vertices per convex-hull

//            hacd.Compute();
            
//            var shapes = new(Matrix4x4 localTransform, XRCollisionShape shape)[hacd.NClusters];
//            for (int c = 0; c < hacd.NClusters; c++)
//            {
//                //hacd.GetCH(c, double[] meshPoints, long[] triangles);

//                //Vector3 centroid = Vector3.Zero;
//                //foreach (Vector3 vertex in meshPoints)
//                //    centroid += vertex;
//                //centroid /= meshPoints.Length;
//                //for (int i = 0; i < meshPoints.Length; ++i)
//                //    meshPoints[i] -= centroid;

//                //XRCollisionConvexHull convexShape = XRCollisionConvexHull.New(
//                //    meshPoints.Select(x => (Vector3)x));

//                //shapes[c] = (Matrix4x4.CreateTranslation(centroid), convexShape);
//            }

//            return XRCollisionCompoundShape.New(shapes);
//        }
//        private static bool HacdUpdate(string msg, double progress, double globalConcativity, int n)
//        {
//            Debug.Out(msg);
//            return true;
//        }
//    }
//}
