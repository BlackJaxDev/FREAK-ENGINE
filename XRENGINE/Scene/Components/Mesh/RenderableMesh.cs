using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Scene.Mesh
{
    public class RenderableMesh : XRBase, IDisposable
    {
        public RenderInfo3D RenderInfo { get; }

        private readonly RenderCommandMesh3D _rc;

        private TransformBase? _rootBone;
        private bool _renderBounds = false;

        public XRMeshRenderer? CurrentLODRenderer => CurrentLOD?.Value?.Renderer;
        public XRMesh? CurrentLODMesh => CurrentLOD?.Value?.Renderer?.Mesh;

        public LinkedListNode<RenderableLOD>? CurrentLOD { get; private set; } = null;
        public XRWorldInstance? World => Component.SceneNode.World;
        public LinkedList<RenderableLOD> LODs { get; private set; } = new();
        public bool RenderBounds
        {
            get => _renderBounds;
            set => SetField(ref _renderBounds, value);
        }

        public TransformBase? RootBone
        {
            get => _rootBone;
            set => SetField(ref _rootBone, value);
        }

        /// <summary>
        /// The transform that owns this mesh.
        /// </summary>
        public RenderableComponent Component { get; }

        private readonly RenderCommandMethod3D _renderBoundsCommand;

        public RenderableMesh(SubMesh mesh, RenderableComponent component)
        {
            Component = component;
            RootBone = mesh.RootBone;

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

            _renderBoundsCommand = new RenderCommandMethod3D((int)EDefaultRenderPass.OpaqueForward, DoRenderBounds);
            RenderInfo = RenderInfo3D.New(component, _rc = new RenderCommandMesh3D(0));
            if (RenderBounds)
                RenderInfo.RenderCommands.Add(_renderBoundsCommand);
            RenderInfo.LocalCullingVolume = mesh.CullingBounds ?? mesh.Bounds;
            RenderInfo.PreAddRenderCommandsCallback = BeforeAdd;
        }

        private void DoRenderBounds(bool shadowPass)
        {
            if (shadowPass)
                return;

            var box = RenderInfo.LocalCullingVolume;
            if (box is not null)
                Engine.Rendering.Debug.RenderBox(box.Value.HalfExtents, box.Value.Center, Matrix4x4.Identity, false, ColorF4.White, true);

            if (RootBone is not null)
                Engine.Rendering.Debug.RenderPoint(RootBone.WorldTranslation, ColorF4.Red, false);
        }

        private void SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            vertexProgram.Uniform(EEngineUniform.RootInvModelMatrix.ToString(), /*RootTransform?.InverseWorldMatrix ?? */Matrix4x4.Identity);
        }

        private void BeforeAdd(RenderInfo info, RenderCommandCollection passes, XRCamera? camera)
        {
            var rend = CurrentLODRenderer;
            bool skinned = (rend?.Mesh?.HasSkinning ?? false);
            TransformBase tfm = skinned ? RootBone ?? Component.Transform : Component.Transform;
            float distance = camera?.DistanceFromNearPlane(tfm.WorldTranslation) ?? 0.0f;

            if (!passes.IsShadowPass)
                UpdateLOD(distance);

            _rc.Mesh = rend;
            _rc.WorldMatrix = skinned ? Matrix4x4.Identity : Component.Transform.WorldMatrix;
            _rc.RenderDistance = distance;

            var mat = rend?.Material;
            if (mat is not null)
                _rc.RenderPass = mat.RenderPass;
        }

        public record RenderableLOD(XRMeshRenderer Renderer, float MaxVisibleDistance);

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

        public Segment GetLocalSegment(Segment worldSegment, bool skinnedMesh)
        {
            Segment localSegment;
            if (skinnedMesh)
            {
                if (RootBone is not null)
                    localSegment = worldSegment.TransformedBy(RootBone.InverseWorldMatrix);
                else
                    localSegment = worldSegment;
            }
            else
            {
                localSegment = worldSegment.TransformedBy(Component.Transform.InverseWorldMatrix);
            }

            return localSegment;
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(RootBone):
                        if (RootBone is not null)
                            RootBone.WorldMatrixChanged -= RootBone_WorldMatrixChanged;
                        break;

                    case nameof(Component):
                        if (Component is not null)
                            Component.Transform.WorldMatrixChanged -= Component_WorldMatrixChanged;
                        break;
                }
            }
            return change;
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(RootBone):
                    if (RootBone is not null)
                        RootBone.WorldMatrixChanged += RootBone_WorldMatrixChanged;
                    break;
                case nameof(Component):
                    if (Component is not null)
                        Component.Transform.WorldMatrixChanged += RootBone_WorldMatrixChanged;
                    break;
                case nameof(RenderBounds):
                    if (RenderBounds)
                    {
                        if (!RenderInfo.RenderCommands.Contains(_renderBoundsCommand))
                            RenderInfo.RenderCommands.Add(_renderBoundsCommand);
                    }
                    else
                        RenderInfo.RenderCommands.Remove(_renderBoundsCommand);
                    break;
            }
        }

        private void RootBone_WorldMatrixChanged(TransformBase rootBone)
        {
            bool hasSkinning = CurrentLOD?.Value?.Renderer?.Mesh?.HasSkinning ?? false;
            if (hasSkinning)
                RenderInfo.CullingMatrix = rootBone.WorldMatrix;
        }
        private void Component_WorldMatrixChanged(TransformBase component)
        {
            bool hasSkinning = CurrentLOD?.Value?.Renderer?.Mesh?.HasSkinning ?? false;
            if (!hasSkinning)
                RenderInfo.CullingMatrix = component.WorldMatrix;
        }
    }
}
