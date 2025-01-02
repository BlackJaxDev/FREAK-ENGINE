using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    /// <summary>
    /// Base helper class for all UI components that can be rendered.
    /// Automatically handles rendering and culling of UI components.
    /// </summary>
    [RequiresTransform(typeof(UIBoundableTransform))]
    public abstract class UIRenderableComponent : UIComponent, IRenderable
    {
        public UIBoundableTransform BoundableTransform => TransformAs<UIBoundableTransform>(true)!;
        public UIRenderableComponent()
        {
            RenderInfo3D = RenderInfo3D.New(this, RenderCommand3D);
            RenderInfo2D = RenderInfo2D.New(this, RenderCommand2D);
            RenderedObjects = [RenderInfo3D, RenderInfo2D];
            RenderInfo3D.PreAddRenderCommandsCallback = ShouldRender3D;
            RenderInfo2D.PreAddRenderCommandsCallback = ShouldRender2D;
        }
        protected virtual bool ShouldRender3D(RenderInfo info, RenderCommandCollection passes, XRCamera? camera)
        {
            var tfm = BoundableTransform;
            if (!tfm.IsVisibleInHierarchy)
                return false;
            var canvas = tfm.ParentCanvas;
            return canvas is not null && canvas.DrawSpace != ECanvasDrawSpace.Screen;
        }
        protected virtual bool ShouldRender2D(RenderInfo info, RenderCommandCollection passes, XRCamera? camera)
        {
            var tfm = BoundableTransform;
            if (!tfm.IsVisibleInHierarchy)
                return false;
            var canvas = tfm.ParentCanvas;
            return canvas is not null && canvas.DrawSpace == ECanvasDrawSpace.Screen;
        }

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);
            if (transform is not UIBoundableTransform tfm)
                return;
            tfm.UpdateRenderInfoBounds(RenderInfo2D, RenderInfo3D);
            var mtx = GetRenderWorldMatrix(tfm);
            RenderCommand3D.WorldMatrix = mtx;
            RenderCommand2D.WorldMatrix = mtx;
        }

        protected virtual Matrix4x4 GetRenderWorldMatrix(UIBoundableTransform tfm) => tfm.WorldMatrix;

        /// <summary>
        /// The material used to render this UI component.
        /// </summary>
        public XRMaterial? Material
        {
            get => Mesh?.Material;
            set
            {
                var m = Mesh;
                if (m is null)
                    return;

                m.Material = value;
            }
        }
        public int RenderPass
        {
            get => RenderCommand3D.RenderPass;
            set
            {
                RenderCommand3D.RenderPass = value;
                RenderCommand2D.RenderPass = value;
            }
        }
        public RenderInfo3D RenderInfo3D { get; }
        public RenderInfo2D RenderInfo2D { get; }
        public RenderCommandMesh3D RenderCommand3D { get; } = new RenderCommandMesh3D(EDefaultRenderPass.OpaqueForward);
        public RenderCommandMesh2D RenderCommand2D { get; } = new RenderCommandMesh2D((int)EDefaultRenderPass.OpaqueForward);
        public RenderInfo[] RenderedObjects { get; }
        public XRMeshRenderer? Mesh
        {
            get => RenderCommand3D.Mesh;
            set
            {
                RenderCommand3D.Mesh = value;
                RenderCommand2D.Mesh = value;
            }
        }
    }
}
