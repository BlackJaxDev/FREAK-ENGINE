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
        public RenderCommandMesh3D(
            int renderPass,
            XRMeshRenderer manager,
            Matrix4x4 worldMatrix,
            float renderDistance,
            XRMaterial? materialOverride = null) : base(renderPass, renderDistance)
        {
            Mesh = manager;
            WorldMatrix = worldMatrix;
            MaterialOverride = materialOverride;
        }

        public override void Render(bool shadowPass)
        {
            //Don't render points or lines in shadow pass
            if (shadowPass)
                return;

            Mesh?.Render(WorldMatrix, MaterialOverride, Instances);
        }
    }
}
