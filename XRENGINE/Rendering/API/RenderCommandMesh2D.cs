using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Rendering.Commands;

namespace XREngine.Rendering
{
    public class RenderCommandMesh2D : RenderCommand2D
    {
        private XRMeshRenderer? _mesh;
        public XRMeshRenderer? Mesh
        {
            get => _mesh;
            set
            {
                _mesh?.Destroy();
                _mesh = value;
            }
        }
        public Matrix4x4 WorldMatrix { get; set; } = Matrix4x4.Identity;
        public XRMaterial? MaterialOverride { get; set; }
        public uint Instances { get; set; } = 1;
        public BoundingRectangle? CropRegion { get; set; } = null;

        public RenderCommandMesh2D() : base() { }
        public RenderCommandMesh2D(int renderPass) : base(renderPass) { }
        public RenderCommandMesh2D(
            int renderPass,
            XRMeshRenderer mesh,
            Matrix4x4 worldMatrix,
            int zIndex,
            XRMaterial? materialOverride = null) : base(renderPass, zIndex)
        {
            RenderPass = renderPass;
            Mesh = mesh;
            WorldMatrix = worldMatrix;
            MaterialOverride = materialOverride;
        }

        public override void Render(bool shadowPass)
        {
            if (Mesh is null)
                return;

            var rend = AbstractRenderer.Current;
            if (rend is not null)
            {
                if (CropRegion is null)
                    rend.SetCroppingEnabled(false);
                else
                {
                    rend.SetCroppingEnabled(true);
                    rend.CropRenderArea(CropRegion.Value);
                }
            }
            Mesh.Render(WorldMatrix, MaterialOverride, Instances);
            rend?.SetCroppingEnabled(false);
        }
    }
}
