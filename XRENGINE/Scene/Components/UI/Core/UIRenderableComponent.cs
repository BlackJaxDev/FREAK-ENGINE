using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Core;
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
        public const string UIWidthUniformName = "UIWidth";
        public const string UIHeightUniformName = "UIHeight";

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
        private XRMaterial? _material;
        public XRMaterial? Material
        {
            get => _material;
            set => SetField(ref _material, value);
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
                Material = value?.Material;
            }
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Material):
                        if (Material is not null)
                            Material.SettingUniforms -= OnMaterialSettingUniforms;
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
                case nameof(Material):
                    var m = Mesh;
                    if (m is not null)
                        m.Material = Material;
                    if (Material is not null)
                        Material.SettingUniforms += OnMaterialSettingUniforms;
                    break;
            }
        }

        private void OnMaterialSettingUniforms(XRMaterialBase material, XRRenderProgram program)
        {
            var m = Material;
            if (m is null)
                return;

            var tfm = BoundableTransform;
            var w = tfm.ActualWidth;
            var h = tfm.ActualHeight;

            program.Uniform(UIWidthUniformName, w);
            program.Uniform(UIHeightUniformName, h);
        }

        protected override void UITransformPropertyChanged(object? sender, IXRPropertyChangedEventArgs e)
        {
            base.UITransformPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                //case nameof(UIBoundableTransform.ActualWidth):
                //case nameof(UIBoundableTransform.ActualHeight):
                case nameof(ClipToBounds):
                case nameof(UIBoundableTransform.AxisAlignedRegion):
                    RenderCommand2D.WorldCropRegion = ClipToBounds ? BoundableTransform.AxisAlignedRegion.AsBoundingRectangle() : null;
                    break;
            }
        }

        private bool _clipToBounds = false;
        public bool ClipToBounds
        {
            get => _clipToBounds;
            set => SetField(ref _clipToBounds, value);
        }
    }
}
