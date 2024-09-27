using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering;
using XREngine.Rendering.Models;
using XREngine.Rendering.Info;

namespace XREngine.Components.Scene.Mesh
{
    public class RenderableMesh : IDisposable
    {
        public RenderInfo3D RenderInfo { get; }
        private readonly RenderCommandMesh3D _rc;

        /// <summary>
        /// The transform that owns this mesh.
        /// </summary>
        public RenderableComponent Component { get; }

        public RenderableMesh(SubMesh mesh, RenderableComponent component)
        {
            Component = component;

            foreach (var lod in mesh.LODs)
                LODs.AddLast(new RenderableLOD(lod.NewRenderer(), lod.MaxVisibleDistance));

            RenderInfo = RenderInfo3D.New(component, _rc = new RenderCommandMesh3D(0));
            RenderInfo.CullingVolume = mesh.CullingVolumeOverride ?? mesh.Bounds;
            RenderInfo.PreAddRenderCommandsCallback = BeforeAdd;
        }

        private void BeforeAdd(RenderInfo info, RenderCommandCollection passes, XRCamera camera)
        {
            float distance = camera?.DistanceFromNearPlane(Component.Transform.WorldTranslation) ?? 0.0f;

            if (!passes.IsShadowPass)
                UpdateLOD(distance);

            _rc.Mesh = CurrentLOD?.Value?.Manager;
            _rc.WorldMatrix = Component.Transform.WorldMatrix;
            _rc.RenderDistance = distance;
            _rc.RenderPass = CurrentLOD?.Value?.Manager?.Material?.RenderPass ?? 0;
        }

        public record RenderableLOD(XRMeshRenderer Manager, float MaxVisibleDistance);

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
                lod.Manager.Destroy();
            LODs.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
