﻿using XREngine.Components;

namespace XREngine.Data.Components.Rendering
{
    public class TerrainComponent : XRComponent
    {
        //private Quadtree terrainQuadtree;

        //public override void Start()
        //{
        //    // Create a quadtree with the terrain center at (0, 0), size 512, and maximum depth of 4.
        //    //terrainQuadtree = new Quadtree(0, 0, 512, 4);
        //}

        public void Generate()
        {
            //// Update the LOD of the quadtree based on the camera position and a LOD distance of 100.
            //float cameraX = 100;
            //float cameraZ = 100;
            //float lodDistance = 100;
            //terrainQuadtree.UpdateLOD(cameraX, cameraZ, lodDistance);

            //// Get the visible nodes (terrain patches) from the quadtree.
            //List<Quadtree.Node> visibleNodes = terrainQuadtree.GetVisibleNodes();

            //foreach (Quadtree.Node node in visibleNodes)
            //{
            //    RenderTerrainPatch(node.CenterX, node.CenterZ, node.Size, node.LOD);
            //}
        }
        static void RenderTerrainPatch(float centerX, float centerZ, float size, int lod)
        {
            // Implement rendering logic for terrain patches.
            Console.WriteLine($"Rendering terrain patch at ({centerX}, {centerZ}) with size {size} and LOD {lod}");
        }
    }
}
