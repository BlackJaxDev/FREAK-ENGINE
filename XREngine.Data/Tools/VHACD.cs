using System.Numerics;
using System.Runtime.InteropServices;

namespace XREngine.Data.Tools
{
    public static class VHACD
    {
        // Define the V-HACD interface functions
        [DllImport("VHACD.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateVHACD();

        [DllImport("VHACD.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ReleaseVHACD(IntPtr vhacd);

        [DllImport("VHACD.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ComputeVHACD(
            IntPtr vhacd,
            [In] float[] points, int numPoints,
            [In] int[] triangles, int numTriangles,
            [In] ref VHACDParameters parameters);

        [DllImport("VHACD.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetConvexHullCount(IntPtr vhacd);

        [DllImport("VHACD.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetConvexHull(IntPtr vhacd, int index, [Out] float[]? points, ref int numPoints);

        // Define the VHACD parameters structure
        [StructLayout(LayoutKind.Sequential)]
        private struct VHACDParameters
        {
            public int m_maxConvexHulls;
            public double m_resolution;
            public double m_concavity;
            public int m_depth;
            public int m_maxVerticesPerHull;
            public bool m_shrinkWrap;
        }

        public static async Task<Vector3[][]?> CalculateAsync(
            Vector3[] positions,
            int[] triangleIndices,
            int maxHulls = 10,
            double resolution = 100000,
            double concavity = 0.0025,
            int depth = 20,
            int maxVerticesPerHull = 64,
            bool shrinkWrap = true)
            => await Task.Run(() => Calculate(positions, triangleIndices, maxHulls, resolution, concavity, depth, maxVerticesPerHull, shrinkWrap));

        public static Vector3[][]? Calculate(
            Vector3[] positions,
            int[] triangleIndices,
            int maxHulls = 10,
            double resolution = 100000,
            double concavity = 0.0025,
            int depth = 20,
            int maxVerticesPerHull = 64,
            bool shrinkWrap = true)
        {
            IntPtr vhacd = CreateVHACD();
            if (vhacd == IntPtr.Zero)
                return null;
            
            VHACDParameters parameters = new()
            {
                m_maxConvexHulls = maxHulls,
                m_resolution = resolution,
                m_concavity = concavity,
                m_depth = depth,
                m_maxVerticesPerHull = maxVerticesPerHull,
                m_shrinkWrap = shrinkWrap
            };

            bool result = ComputeVHACD(
                vhacd,
                positions.SelectMany(x => new float[] { x.X, x.Y, x.Z }).ToArray(),
                positions.Length,
                triangleIndices,
                triangleIndices.Length / 3,
                ref parameters);

            if (!result)
            {
                ReleaseVHACD(vhacd);
                return null;
            }

            int convexHullCount = GetConvexHullCount(vhacd);
            Vector3[][] convexHulls = new Vector3[convexHullCount][];

            for (int i = 0; i < convexHullCount; i++)
            {
                int numPoints = 0;
                GetConvexHull(vhacd, i, null, ref numPoints);

                float[] hullPoints = new float[numPoints * 3];
                GetConvexHull(vhacd, i, hullPoints, ref numPoints);

                Vector3[] hull = new Vector3[numPoints];
                for (int j = 0; j < numPoints; j++)
                    hull[j] = new Vector3(hullPoints[j * 3], hullPoints[j * 3 + 1], hullPoints[j * 3 + 2]);
                convexHulls[i] = hull;
            }

            ReleaseVHACD(vhacd);
            return convexHulls;
        }
    }
}
