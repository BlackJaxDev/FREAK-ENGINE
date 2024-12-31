using Extensions;
using System.Drawing;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    [RequiresTransform(typeof(UIBoundableTransform))]
    public class UIMaterialComponent : UIComponent, IRenderable
    {
        public UIBoundableTransform BoundableTransform => TransformAs<UIBoundableTransform>(true)!;

        public UIMaterialComponent() 
            : this(XRMaterial.CreateUnlitColorMaterialForward(Color.Magenta)) { }
        public UIMaterialComponent(XRMaterial quadMaterial, bool flipVerticalUVCoord = false)
        {
            XRMesh quadData = XRMesh.Create(VertexQuad.PosZ(1.0f, 1.0f, 0.0f, true, flipVerticalUVCoord));
            RenderCommand.Mesh = new XRMeshRenderer(quadData, quadMaterial);
            //RenderCommand.ZIndex = 0;

            RenderInfo3D = RenderInfo3D.New(this, RenderCommand);
            RenderInfo2D = RenderInfo2D.New(this, RenderCommand);
            RenderedObjects = [RenderInfo3D, RenderInfo2D];
            RenderInfo3D.PreAddRenderCommandsCallback = ShouldRender3D;
            RenderInfo2D.PreAddRenderCommandsCallback = ShouldRender2D;
        }

        private bool ShouldRender3D(RenderInfo info, RenderCommandCollection passes, XRCamera? camera)
        {
            var canvas = BoundableTransform?.ParentCanvas;
            return canvas is not null && canvas.DrawSpace != ECanvasDrawSpace.Screen;
        }
        private bool ShouldRender2D(RenderInfo info, RenderCommandCollection passes, XRCamera? camera)
        {
            var canvas = BoundableTransform?.ParentCanvas;
            return canvas is not null && canvas.DrawSpace == ECanvasDrawSpace.Screen;
        }

        public RenderInfo3D RenderInfo3D { get; }
        public RenderInfo2D RenderInfo2D { get; }

        /// <summary>
        /// The material used to render on this UI component.
        /// </summary>
        public XRMaterial? Material
        {
            get => RenderCommand.Mesh?.Material;
            set
            {
                if (RenderCommand.Mesh is null)
                    return;

                RenderCommand.Mesh.Material = value;
            }
        }

        public XRTexture? Texture(int index)
            => (RenderCommand.Mesh?.Material?.Textures?.IndexInRange(index) ?? false)
                ? RenderCommand.Mesh.Material.Textures[index]
                : null;

        public T? Texture<T>(int index) where T : XRTexture
            => (RenderCommand.Mesh?.Material?.Textures?.IndexInRange(index) ?? false)
                ? RenderCommand.Mesh.Material.Textures[index] as T
                : null;

        /// <summary>
        /// Retrieves the linked material's uniform parameter at the given index.
        /// Use this to set uniform values to be passed to the shader.
        /// </summary>
        public T2 Parameter<T2>(int index) where T2 : ShaderVar
            => RenderCommand.Mesh.Parameter<T2>(index);
        /// <summary>
        /// Retrieves the linked material's uniform parameter with the given name.
        /// Use this to set uniform values to be passed to the shader.
        /// </summary>
        public T2 Parameter<T2>(string name) where T2 : ShaderVar
            => RenderCommand.Mesh.Parameter<T2>(name);

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);

            if (transform is not UIBoundableTransform tfm)
                return;

            tfm.UpdateRenderInfoBounds(RenderInfo2D, RenderInfo3D);

            var w = tfm.ActualWidth;
            var h = tfm.ActualHeight;
            RenderCommand.WorldMatrix = Matrix4x4.CreateScale(w, h, 1.0f) * tfm.WorldMatrix;
        }

        public RenderCommandMesh3D RenderCommand { get; } = new RenderCommandMesh3D(EDefaultRenderPass.OpaqueForward);
        public RenderInfo[] RenderedObjects { get; }
    }
}
