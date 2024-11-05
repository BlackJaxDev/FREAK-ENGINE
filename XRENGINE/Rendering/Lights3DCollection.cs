using MIConvexHull;
using System.Numerics;
using XREngine.Components.Lights;
using XREngine.Data;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Trees;
using XREngine.Data.Vectors;
using XREngine.Rendering;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;

namespace XREngine.Scene
{
    public class Lights3DCollection(VisualScene visualScene) : XRBase
    {
        public bool IBLCaptured { get; private set; } = false;

        private bool _capturing = false;

        private ITriangulation<LightProbeComponent, LightProbeCell>? _cells;

        public Octree<LightProbeCell> LightProbeTree { get; } = new(new AABB());
        
        public VisualScene Scene { get; } = visualScene;
        public EventList<SpotLightComponent> SpotLights { get; } = [];
        public EventList<PointLightComponent> PointLights { get; } = [];
        public EventList<DirectionalLightComponent> DirectionalLights { get; } = [];
        public EventList<LightProbeComponent> LightProbes { get; } = [];

        public bool RenderingShadowMaps { get; private set; } = false;

        internal void SetForwardLightingUniforms(XRRenderProgram program)
        {
            program.Uniform("DirLightCount", DirectionalLights.Count);
            program.Uniform("PointLightCount", PointLights.Count);
            program.Uniform("SpotLightCount", SpotLights.Count);

            for (int i = 0; i < DirectionalLights.Count; ++i)
                DirectionalLights[i].SetUniforms(program, $"DirLightData[{i}]");

            for (int i = 0; i < SpotLights.Count; ++i)
                SpotLights[i].SetUniforms(program, $"SpotLightData[{i}]");

            for (int i = 0; i < PointLights.Count; ++i)
                PointLights[i].SetUniforms(program, $"PointLightData[{i}]");
        }

        public void CollectVisibleItems()
        {
            foreach (DirectionalLightComponent l in DirectionalLights)
                l.CollectVisibleItems(Scene);

            foreach (SpotLightComponent l in SpotLights)
                l.CollectVisibleItems(Scene);

            foreach (PointLightComponent l in PointLights)
                l.CollectVisibleItems(Scene);
        }

        public void SwapBuffers()
        {
            foreach (DirectionalLightComponent l in DirectionalLights)
                l.SwapBuffers();

            foreach (SpotLightComponent l in SpotLights)
                l.SwapBuffers();

            foreach (PointLightComponent l in PointLights)
                l.SwapBuffers();
        }

        public void RenderShadowMaps(bool collectVisibleNow)
        {
            RenderingShadowMaps = true;

            foreach (DirectionalLightComponent l in DirectionalLights)
                l.RenderShadowMap(Scene, collectVisibleNow);

            foreach (SpotLightComponent l in SpotLights)
                l.RenderShadowMap(Scene, collectVisibleNow);

            foreach (PointLightComponent l in PointLights)
                l.RenderShadowMap(Scene, collectVisibleNow);

            RenderingShadowMaps = false;
        }

        public void Clear()
        {
            SpotLights.Clear();
            PointLights.Clear();
            DirectionalLights.Clear();
        }

        /// <summary>
        /// Renders the scene from each light probe's perspective.
        /// </summary>
        public void CaptureLightProbes()
        {
            Engine.EnqueueMainThreadTask(() => CaptureLightProbes(
                Engine.Rendering.Settings.LightProbeDefaultColorResolution, 
                Engine.Rendering.Settings.ShouldLightProbesCaptureDepth,
                Engine.Rendering.Settings.LightProbeDefaultDepthResolution));
        }

        /// <summary>
        /// Renders the scene from each light probe's perspective.
        /// </summary>
        /// <param name="colorResolution"></param>
        /// <param name="captureDepth"></param>
        /// <param name="depthResolution"></param>
        /// <param name="force"></param>
        public void CaptureLightProbes(uint colorResolution, bool captureDepth, uint depthResolution, bool force = false)
        {
            if (_capturing || (!force && IBLCaptured))
                return;

            IBLCaptured = true;
            Debug.Out(EOutputVerbosity.Verbose, true, true, true, true, 0, 10, "Capturing scene IBL...");
            _capturing = true;

            try
            {
                IReadOnlyList<LightProbeComponent> list = LightProbes;
                for (int i = 0; i < list.Count; i++)
                {
                    Debug.Out(EOutputVerbosity.Verbose, true, true, true, true, 0, 10, $"Capturing light probe {i + 1} of {list.Count}.");
                    list[i].FullCapture(colorResolution, captureDepth, depthResolution);
                }
            }
            catch (Exception e)
            {
                Debug.Out(EOutputVerbosity.Verbose, true, true, true, true, 0, 10, e.Message);
            }
            finally
            {
                _capturing = false;
            }
        }

        private XRMeshRenderer? _instancedCellRenderer;

        /// <summary>
        /// Triangulates the light probes to form a Delaunay triangulation and adds the tetrahedron cells to the render tree.
        /// </summary>
        /// <param name="scene"></param>
        public void GenerateDelauanyTriangulation(VisualScene scene)
        {
            _cells = Triangulation.CreateDelaunay<LightProbeComponent, LightProbeCell>(LightProbes);
            //_instancedCellRenderer = new XRMeshRenderer(GenerateInstancedCellMesh(), new XRMaterial(XRShader.EngineShader("Common/DelaunayCell.frag", EShaderType.Fragment)));
            scene.RenderablesTree.AddRange(_cells.Cells.Select(x => x.RenderInfo));
        }

        public void RenderCells(ICollection<LightProbeCell> probes)
        {
            int count = probes.Count;
            if (count <= 0)
                return;

            //_instancedCellRenderer!.Mesh.GetBuffer(0, probes.SelectMany(x => x.Vertices.Select(y => y.Transform.WorldTranslation)).ToArray());
            _instancedCellRenderer!.Render(Matrix4x4.Identity, null, (uint)count);
        }

        //public static XRMesh GenerateInstancedCellMesh()
        //{
        //    //Create zero-verts for a tetrahedron that will be filled in with instanced positions on the gpu
        //    VertexTriangle[] triangles =
        //    [
        //        new(new Vertex(Vector3.Zero), new Vertex(Vector3.Zero), new Vertex(Vector3.Zero)),
        //            new(new Vertex(Vector3.Zero), new Vertex(Vector3.Zero), new Vertex(Vector3.Zero)),
        //            new(new Vertex(Vector3.Zero), new Vertex(Vector3.Zero), new Vertex(Vector3.Zero)),
        //            new(new Vertex(Vector3.Zero), new Vertex(Vector3.Zero), new Vertex(Vector3.Zero))
        //    ];
        //    XRMesh mesh = new(XRMeshDescriptor.Positions(), triangles);
        //    mesh.AddBuffer()
        //}

        public static void GenerateLightProbeGrid(SceneNode parent, AABB bounds, Vector3 probesPerMeter)
        {
            Vector3 size = bounds.Size;

            IVector3 probeCount = new(
                (int)(size.X * probesPerMeter.X),
                (int)(size.Y * probesPerMeter.Y),
                (int)(size.Z * probesPerMeter.Z));

            Vector3 localMin = bounds.Min;

            Vector3 probeInc = new(
                size.X / probeCount.X,
                size.Y / probeCount.Y,
                size.Z / probeCount.Z);

            Vector3 baseInc = probeInc * 0.5f;

            for (int x = 0; x < probeCount.X; ++x)
                for (int y = 0; y < probeCount.Y; ++y)
                    for (int z = 0; z < probeCount.Z; ++z)
                        new SceneNode(parent, $"Probe[{x},{y},{z}]", new Transform(localMin + baseInc + new Vector3(x, y, z) * probeInc)).AddComponent<LightProbeComponent>();
        }

        /// <summary>
        /// Represents a tetrehedron consisting of 4 light probes, searchable within the octree
        /// </summary>
        public class LightProbeCell : TriangulationCell<LightProbeComponent, LightProbeCell>, IOctreeItem, IRenderable, IVolume
        {
            public LightProbeCell()
            {
                _rc = new(0)
                {

                };
                RenderInfo = RenderInfo3D.New(this);
                RenderInfo.CullingVolume = this;
                RenderedObjects = [RenderInfo];
            }

            private RenderCommandMesh3D _rc;
            public RenderInfo3D RenderInfo { get; }
            public RenderInfo[] RenderedObjects { get; }
            public IVolume? CullingVolume => this;
            public OctreeNodeBase? OctreeNode { get; set; }
            public bool ShouldRender { get; } = true;

            public bool Intersects(IVolume cullingVolume, bool containsOnly)
            {
                throw new NotImplementedException();
            }

            public EContainment Contains(AABB box)
            {
                throw new NotImplementedException();
            }

            public EContainment Contains(Sphere sphere)
            {
                throw new NotImplementedException();
            }

            public EContainment Contains(Cone cone)
            {
                throw new NotImplementedException();
            }

            public EContainment Contains(Capsule shape)
            {
                throw new NotImplementedException();
            }

            public Vector3 ClosestPoint(Vector3 point, bool clampToEdge)
            {
                throw new NotImplementedException();
            }

            public bool Contains(Vector3 point)
            {
                throw new NotImplementedException();
            }

            public AABB GetAABB()
            {
                throw new NotImplementedException();
            }

            public bool Intersects(Segment segment, out Vector3[] points)
            {
                throw new NotImplementedException();
            }

            public bool Intersects(Segment segment)
            {
                throw new NotImplementedException();
            }
        }

        public LightProbeComponent[] GetNearestProbes(Vector3 position)
        {
            if (_cells is null)
                return [];

            //Find a tetrahedron cell that contains the point.
            //We'll use this group of probes to light whatever mesh is using the provided position as reference.
            LightProbeCell? cell = LightProbeTree.FindFirst(
                item => item.CullingVolume?.Contains(position) ?? false,
                bounds => bounds.Contains(position));

            if (cell is null)
                return [];

            return cell.Vertices;
        }
    }
}