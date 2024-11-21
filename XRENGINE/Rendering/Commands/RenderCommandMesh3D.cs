using System.Numerics;
using XREngine.Rendering;
using XREngine.Rendering.Commands;

namespace XREngine.Data.Rendering
{
    public class RenderCommandMesh3D : RenderCommand3D
    {
        private XRMeshRenderer? _mesh;
        private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
        private XRMaterial? _materialOverride;
        private uint _instances = 1;
        private bool _worldMatrixIsModelMatrix = true;

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
            => Mesh?.Render(WorldMatrixIsModelMatrix ? WorldMatrix : Matrix4x4.Identity, MaterialOverride, Instances);

        public override void PreRender(XRCamera? camera, bool shadowPass)
        {
            base.PreRender(camera, shadowPass);
            if (camera != null)
                UpdateRenderDistance(WorldMatrix.Translation, camera);
        }
    }
}
