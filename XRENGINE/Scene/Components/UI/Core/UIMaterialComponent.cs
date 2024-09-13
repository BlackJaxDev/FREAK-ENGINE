using Extensions;
using System.Drawing;
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
        public UIMaterialComponent() 
            : this(XRMaterial.CreateUnlitColorMaterialForward(Color.Magenta)) { }
        public UIMaterialComponent(XRMaterial material, bool flipVerticalUVCoord = false)
        {
            XRMesh quadData = XRMesh.Create(VertexQuad.PosZ(1.0f, 1.0f, 0.0f, true, flipVerticalUVCoord));
            RenderCommand.Mesh = new XRMeshRenderer(quadData, material);
            RenderCommand.ZIndex = 0;

            RenderedObjects = [RenderInfo2D.New(this)];
        }

        //[Category("Rendering")]
        //public IRenderInfo2D RenderInfo { get; } = new RenderInfo2D(0, 0);

        //[Category("Rendering")]
        //public override int LayerIndex
        //{
        //    get => base.LayerIndex;
        //    set
        //    {
        //        base.LayerIndex = value;
        //        RenderInfo.LayerIndex = value;
        //    }
        //}
        //[Category("Rendering")]
        //public override int IndexWithinLayer
        //{
        //    get => base.IndexWithinLayer;
        //    set
        //    {
        //        base.IndexWithinLayer = value;
        //        RenderInfo.IndexWithinLayer = value;
        //    }
        //}

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

        // 3--2
        // |\ |
        // | \|
        // 0--1
        //public unsafe override Vector2 Resize(Vector2 parentBounds)
        //{
        //    //013312

        //    Vector2 r = base.Resize(parentBounds);

        //    DataBuffer buffer = _quad.Data[0];
        //    Vector3* data = (Vector3*)buffer.Address;
        //    data[0] = new Vector3(0.0f);
        //    data[1] = data[4] = new Vector3(Width, 0.0f, 0.0f);
        //    data[2] = data[3] = new Vector3(0.0f, Height, 0.0f);
        //    data[5] = new Vector3(Width, Height, 0.0f);
        //    buffer.PushData();

        //    return r;
        //}

        //protected override void OnRecalcLocalTransform(out Matrix4 localTransform, out Matrix4 inverseLocalTransform)
        //{
        //    base.OnRecalcLocalTransform(out localTransform, out inverseLocalTransform);
        //    localTransform = localTransform * Matrix4.CreateScale(_size.X, _size.Y, 1.0f);
        //    inverseLocalTransform = Matrix4.CreateScale(1.0f / _size.X, 1.0f / _size.Y, 1.0f) * inverseLocalTransform;
        //}

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);
            //RenderCommand.WorldMatrix = Transform.WorldMatrix * Matrix4x4.CreateScale(ActualWidth, ActualHeight, 1.0f);
        }

        public RenderCommandMesh2D RenderCommand { get; } = new RenderCommandMesh2D(0);
        public RenderInfo[] RenderedObjects { get; }
        
        public void AddRenderCommands(RenderCommandCollection passes, XRCamera camera)
        {
            passes.Add(RenderCommand);
        }

        //public enum BackgroundImageDisplay
        //{
        //    Stretch,
        //    CenterFit,
        //    ResizeWithBars,
        //    Tile,
        //}
        //private BackgroundImageDisplay _backgroundUV = BackgroundImageDisplay.Stretch;
        //public BackgroundImageDisplay BackgroundUV
        //{
        //    get => _backgroundUV;
        //    set
        //    {
        //        _backgroundUV = value;
        //        OnResized();
        //    }
        //}

        //float* points = stackalloc float[8];
        //float tAspect = (float)_bgImage.Width / (float)_bgImage.Height;
        //float wAspect = (float)Width / (float)Height;

        //switch (_bgType)
        //{
        //    case BackgroundImageDisplay.Stretch:

        //        points[0] = points[1] = points[3] = points[6] = 0.0f;
        //        points[2] = points[4] = Width;
        //        points[5] = points[7] = Height;

        //        break;

        //    case BackgroundImageDisplay.Center:

        //        if (tAspect > wAspect)
        //        {
        //            points[1] = points[3] = 0.0f;
        //            points[5] = points[7] = Height;

        //            points[0] = points[6] = Width * ((Width - ((float)Height / _bgImage.Height * _bgImage.Width)) / Width / 2.0f);
        //            points[2] = points[4] = Width - points[0];
        //        }
        //        else
        //        {
        //            points[0] = points[6] = 0.0f;
        //            points[2] = points[4] = Width;

        //            points[1] = points[3] = Height * (((Height - ((float)Width / _bgImage.Width * _bgImage.Height))) / Height / 2.0f);
        //            points[5] = points[7] = Height - points[1];
        //        }
        //        break;

        //    case BackgroundImageDisplay.ResizeWithBars:

        //        if (tAspect > wAspect)
        //        {
        //            points[0] = points[6] = 0.0f;
        //            points[2] = points[4] = Width;

        //            points[1] = points[3] = Height * (((Height - ((float)Width / _bgImage.Width * _bgImage.Height))) / Height / 2.0f);
        //            points[5] = points[7] = Height - points[1];
        //        }
        //        else
        //        {
        //            points[1] = points[3] = 0.0f;
        //            points[5] = points[7] = Height;

        //            points[0] = points[6] = Width * ((Width - ((float)Height / _bgImage.Height * _bgImage.Width)) / Width / 2.0f);
        //            points[2] = points[4] = Width - points[0];
        //        }

        //        break;
        //}
    }
}
