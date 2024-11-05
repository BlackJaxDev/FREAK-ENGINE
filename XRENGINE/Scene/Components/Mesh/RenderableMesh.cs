using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Scene.Mesh
{
    public class RenderableMesh : IDisposable
    {
        public RenderInfo3D RenderInfo { get; }

        private readonly RenderCommandMesh3D _rc;
        
        public TransformBase? RootTransform { get; set; }

        /// <summary>
        /// The transform that owns this mesh.
        /// </summary>
        public RenderableComponent Component { get; }

        public RenderableMesh(SubMesh mesh, RenderableComponent component)
        {
            Component = component;

            foreach (var lod in mesh.LODs)
            {
                var renderer = lod.NewRenderer();
                renderer.SettingUniforms += SettingUniforms;
                void UpdateReferences(object? s, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(SubMeshLOD.Mesh))
                        renderer.Mesh = lod.Mesh;
                    else if (e.PropertyName == nameof(SubMeshLOD.Material))
                        renderer.Material = lod.Material;
                }
                lod.PropertyChanged += UpdateReferences;
                LODs.AddLast(new RenderableLOD(renderer, lod.MaxVisibleDistance));
            }

            RenderInfo = RenderInfo3D.New(component, _rc = new RenderCommandMesh3D(0));
            RenderInfo.CullingVolume = mesh.CullingVolumeOverride ?? mesh.Bounds;
            RenderInfo.PreAddRenderCommandsCallback = BeforeAdd;
        }

        private void SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            vertexProgram.Uniform(EEngineUniform.RootInvModelMatrix.ToString(), /*RootTransform?.InverseWorldMatrix ?? */Matrix4x4.Identity);
        }

        private void BeforeAdd(RenderInfo info, RenderCommandCollection passes, XRCamera? camera)
        {
            float distance = camera?.DistanceFromNearPlane(Component.Transform.WorldTranslation) ?? 0.0f;

            if (!passes.IsShadowPass)
                UpdateLOD(distance);

            var rend = CurrentLOD?.Value?.Renderer;
            _rc.Mesh = rend;
            _rc.WorldMatrix = (rend?.Mesh?.HasSkinning ?? false) ? Matrix4x4.Identity : Component.Transform.WorldMatrix;
            _rc.RenderDistance = distance;
            _rc.RenderPass = rend?.Material?.RenderPass ?? 0;
        }

        public record RenderableLOD(XRMeshRenderer Renderer, float MaxVisibleDistance);

        public LinkedListNode<RenderableLOD>? CurrentLOD { get; private set; } = null;
        public XRWorldInstance? World => Component.SceneNode.World;
        public LinkedList<RenderableLOD> LODs { get; private set; } = new();

        public void UpdateLOD(XRCamera camera)
            => UpdateLOD(camera.DistanceFromNearPlane(Component.Transform.WorldTranslation));
        public void UpdateLOD(float distanceToCamera)
        {
            if (LODs.Count == 0)
                return;

            if (CurrentLOD is null)
            {
                CurrentLOD = LODs.First;
                return;
            }

            while (CurrentLOD.Next is not null && distanceToCamera > CurrentLOD.Value.MaxVisibleDistance)
                CurrentLOD = CurrentLOD.Next;

            if (CurrentLOD.Previous is not null && distanceToCamera < CurrentLOD.Previous.Value.MaxVisibleDistance)
                CurrentLOD = CurrentLOD.Previous;
        }

        public void Dispose()
        {
            foreach (var lod in LODs)
                lod.Renderer.Destroy();
            LODs.Clear();
            GC.SuppressFinalize(this);
        }

        [RequiresDynamicCode("")]
        public float? Intersect(Segment localSpaceSegment, out Triangle? triangle)
        {
            triangle = null;
            return CurrentLOD?.Value?.Renderer?.Mesh?.Intersect(localSpaceSegment, out triangle);
        }
    }
}
