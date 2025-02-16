﻿using System.Numerics;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using YamlDotNet.Serialization;

namespace XREngine.Data.Rendering
{
    public class RenderCommandMesh3D : RenderCommand3D
    {
        private XRMeshRenderer? _mesh;
        private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
        private XRMaterial? _materialOverride;
        private uint _instances = 1;
        private bool _worldMatrixIsModelMatrix = true;

        private XRMeshRenderer? _renderMesh;
        private Matrix4x4 _renderWorldMatrix;
        private XRMaterial? _renderMaterialOverride;
        private uint _renderInstances;
        private bool _renderWorldMatrixIsModelMatrix;

        [YamlIgnore]
        public XRMeshRenderer? Mesh
        {
            get => _mesh;
            set => SetField(ref _mesh, value);
        }
        public Matrix4x4 WorldMatrix
        {
            get => _worldMatrix;
            set => SetField(ref _worldMatrix, value);
        }
        public bool WorldMatrixIsModelMatrix
        {
            get => _worldMatrixIsModelMatrix;
            set => SetField(ref _worldMatrixIsModelMatrix, value);
        }
        public XRMaterial? MaterialOverride
        {
            get => _materialOverride;
            set => SetField(ref _materialOverride, value);
        }
        public uint Instances
        {
            get => _instances;
            set => SetField(ref _instances, value);
        }

        public RenderCommandMesh3D() : base() { }
        public RenderCommandMesh3D(int renderPass) : base(renderPass) { }
        public RenderCommandMesh3D(EDefaultRenderPass renderPass) : base((int)renderPass) { }
        public RenderCommandMesh3D(
            int renderPass,
            XRMeshRenderer manager,
            Matrix4x4 worldMatrix,
            XRMaterial? materialOverride = null) : base(renderPass)
        {
            Mesh = manager;
            WorldMatrix = worldMatrix;
            MaterialOverride = materialOverride;
        }

        public override void Render(bool shadowPass)
            => _renderMesh?.Render(_renderWorldMatrixIsModelMatrix ? _renderWorldMatrix : Matrix4x4.Identity, _renderMaterialOverride, _renderInstances);

        public override void CollectedForRender(XRCamera? camera, bool shadowPass)
        {
            base.CollectedForRender(camera, shadowPass);
            // Update render distance for proper sorting.
            // This is done in the collect visible thread - doesn't need to be thread safe.
            if (camera != null)
                UpdateRenderDistance(_renderWorldMatrix.Translation, camera);
        }

        public override void SwapBuffers(bool shadowPass)
        {
            base.SwapBuffers(shadowPass);
            _renderMesh = Mesh;
            _renderWorldMatrix = WorldMatrix;
            _renderMaterialOverride = MaterialOverride;
            _renderInstances = Instances;
            _renderWorldMatrixIsModelMatrix = WorldMatrixIsModelMatrix;
        }
    }
}
