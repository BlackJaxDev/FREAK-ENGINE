﻿using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using XREngine.Data;
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

        public XRMeshRenderer? CurrentLODRenderer => CurrentLOD?.Value?.Renderer;
        public XRMesh? CurrentLODMesh => CurrentLOD?.Value?.Renderer?.Mesh;

        private LinkedListNode<RenderableLOD>? _currentLOD = null;
        public LinkedListNode<RenderableLOD>? CurrentLOD
        {
            get => _currentLOD;
            private set => SetField(ref _currentLOD, value);
        }
        public XRWorldInstance? World => Component.SceneNode.World;
        public LinkedList<RenderableLOD> LODs { get; private set; } = new();

        private bool _renderBounds = Engine.Rendering.Settings.RenderMesh3DBounds;
        public bool RenderBounds
        {
            get => _renderBounds;
            set => SetField(ref _renderBounds, value);
        }

        private TransformBase? _rootBone;
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

        void ComponentPropertyChanged(object? s, IXRPropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RenderableComponent.Transform) && !Component.SceneNode.IsTransformNull)
                Component.Transform.WorldMatrixChanged += Component_WorldMatrixChanged;
        }
        void ComponentPropertyChanging(object? s, IXRPropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(RenderableComponent.Transform) && !Component.SceneNode.IsTransformNull)
                Component.Transform.WorldMatrixChanged -= Component_WorldMatrixChanged;
        }

        public RenderableMesh(SubMesh mesh, RenderableComponent component)
        {
            Component = component;
            Component.Transform.WorldMatrixChanged += Component_WorldMatrixChanged;
            Component.PropertyChanged += ComponentPropertyChanged;
            Component.PropertyChanging += ComponentPropertyChanging;
            RootBone = mesh.RootBone;

            foreach (var lod in mesh.LODs)
            {
                var renderer = lod.NewRenderer();
                renderer.SettingUniforms += SettingUniforms;
                void UpdateReferences(object? s, IXRPropertyChangedEventArgs e)
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

            if (LODs.Count > 0)
                CurrentLOD = LODs.First;
        }

        private void DoRenderBounds(bool shadowPass)
        {
            if (shadowPass)
                return;

            var box = (RenderInfo as IOctreeItem)?.WorldCullingVolume;
            if (box is not null)
                Engine.Rendering.Debug.RenderBox(box.Value.LocalHalfExtents, box.Value.LocalCenter, box.Value.Transform, false, ColorF4.White, true);

            if (RootBone is not null)
                Engine.Rendering.Debug.RenderPoint(RootBone.WorldTranslation, ColorF4.Red, false);
        }

        private void SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            vertexProgram.Uniform(EEngineUniform.RootInvModelMatrix.ToString(), /*RootTransform?.InverseWorldMatrix ?? */Matrix4x4.Identity);
        }

        private bool BeforeAdd(RenderInfo info, RenderCommandCollection passes, XRCamera? camera)
        {
            var rend = CurrentLODRenderer;
            bool skinned = (rend?.Mesh?.HasSkinning ?? false);
            TransformBase tfm = skinned ? RootBone ?? Component.Transform : Component.Transform;
            float distance = camera?.DistanceFromNearPlane(tfm.WorldTranslation) ?? 0.0f;

            if (!passes.IsShadowPass)
                UpdateLOD(distance);

            _rc.Mesh = rend;
            //RenderInfo.CullingOffsetMatrix = _rc.WorldMatrix = skinned ? Matrix4x4.Identity : Component.Transform.WorldMatrix;
            _rc.RenderDistance = distance;

            var mat = rend?.Material;
            if (mat is not null)
                _rc.RenderPass = mat.RenderPass;

            return true;
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
                    {
                        RootBone.WorldMatrixChanged += RootBone_WorldMatrixChanged;
                        RootBone_WorldMatrixChanged(RootBone);
                    }
                    break;
                case nameof(Component):
                    if (Component is not null)
                    {
                        Component.Transform.WorldMatrixChanged += RootBone_WorldMatrixChanged;
                        Component_WorldMatrixChanged(Component.Transform);
                    }
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
                case nameof(CurrentLOD):
                    if (CurrentLOD is not null)
                    {
                        var rend = CurrentLODRenderer;
                        bool skinned = (rend?.Mesh?.HasSkinning ?? false);
                        _rc.WorldMatrix = skinned ? Matrix4x4.Identity : Component.Transform.WorldMatrix;
                    }
                    break;
            }
        }

        /// <summary>
        /// Updates the culling offset matrix for skinned meshes.
        /// </summary>
        /// <param name="rootBone"></param>
        private void RootBone_WorldMatrixChanged(TransformBase rootBone)
        {
            bool hasSkinning = CurrentLOD?.Value?.Renderer?.Mesh?.HasSkinning ?? false;
            if (!hasSkinning)
                return;
            
            RenderInfo.CullingOffsetMatrix = rootBone.WorldMatrix;
        }

        /// <summary>
        /// Updates the culling offset matrix for non-skinned meshes.
        /// </summary>
        /// <param name="component"></param>
        private void Component_WorldMatrixChanged(TransformBase component)
        {
            bool hasSkinning = CurrentLOD?.Value?.Renderer?.Mesh?.HasSkinning ?? false;
            if (hasSkinning)
                return;

            if (component is null)
                return;
            
            var mtx = component.WorldMatrix;

            if (_rc is not null)
                _rc.WorldMatrix = mtx;

            if (RenderInfo is not null/* && !hasSkinning*/)
                RenderInfo.CullingOffsetMatrix = mtx;
        }
    }
}
