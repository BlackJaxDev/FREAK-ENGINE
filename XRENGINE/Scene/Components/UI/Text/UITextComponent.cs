using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    [RequiresTransform(typeof(UIBoundableTransform))]
    public class UITextComponent : UIComponent, IRenderable
    {
        public UIBoundableTransform BoundableTransform => TransformAs<UIBoundableTransform>(true)!;

        public UITextComponent()
        {
            RenderedObjects[0] = RenderInfo3D = RenderInfo3D.New(this, _rc3D);
            RenderedObjects[1] = RenderInfo2D = RenderInfo2D.New(this, _rc2D);
        }

        private readonly RenderCommandMesh3D _rc3D = new((int)EDefaultRenderPass.TransparentForward);
        private readonly RenderCommandMesh2D _rc2D = new((int)EDefaultRenderPass.TransparentForward);

        public RenderInfo[] RenderedObjects { get; } = new RenderInfo[2];

        private FontGlyphSet? _font;
        private string? _text;
        private bool _animatableTransforms = false;
        private readonly List<(Vector4 transform, Vector4 uvs)> _glyphs = [];

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);
            _rc3D.WorldMatrix = transform.WorldMatrix;
            _rc2D.WorldMatrix = transform.WorldMatrix;
            RenderInfo3D.PreAddRenderCommandsCallback = ShouldRender3D;
            RenderInfo2D.PreAddRenderCommandsCallback = ShouldRender2D;
        }

        public RenderInfo3D RenderInfo3D { get; }
        public RenderInfo2D RenderInfo2D { get; }

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

        override protected void OnTransformChanging()
        {
            base.OnTransformChanging();
            if (SceneNode.TryGetTransformAs<UIBoundableTransform>(out var tfm) && tfm is not null)
            {
                tfm.CalcAutoHeightCallback = null;
                tfm.CalcAutoWidthCallback = null;
            }
        }
        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();
            if (SceneNode.TryGetTransformAs<UIBoundableTransform>(out var tfm) && tfm is not null)
            {
                tfm.CalcAutoHeightCallback = CalcAutoHeight;
                tfm.CalcAutoWidthCallback = CalcAutoWidth;
            }
        }

        private bool _multiLine = false;
        public bool MultiLine
        {
            get => _multiLine;
            set => SetField(ref _multiLine, value);
        }

        private bool _wordWrap = false;
        public bool WordWrap
        {
            get => _wordWrap;
            set => SetField(ref _wordWrap, value);
        }

        //TODO: return and cache max width and height when calculating glyphs instead
        private float CalcAutoWidth(UIBoundableTransform transform)
        {
            //x = pos x, z = scale x
            lock (_glyphLock)
            {
                if (_glyphs is null || _glyphs.Count == 0)
                    return 0.0f;

                if (MultiLine)
                    return _glyphs.Max(g => g.transform.X + g.transform.Z);
                else
                {
                    var last = _glyphs[^1];
                    return last.transform.X + last.transform.Z;
                }
            }
        }
        private float CalcAutoHeight(UIBoundableTransform transform)
        {
            //y = pos y, w = scale y
            lock (_glyphLock)
            {
                if (_glyphs is null || _glyphs.Count == 0)
                    return 0.0f;

                float max = _glyphs.Max(g => g.transform.Y);
                float min = _glyphs.Min(g => g.transform.Y + g.transform.W);
                return max - min;
            }
        }

        private XRDataBuffer? _uvsBuffer;

        public string? Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }
        public FontGlyphSet? Font
        {
            get => _font;
            set => SetField(ref _font, value);
        }
        public bool AnimatableTransforms 
        {
            get => _animatableTransforms;
            set => SetField(ref _animatableTransforms, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Font):
                    UpdateText(true);
                    break;
                case nameof(Text):
                case nameof(AnimatableTransforms):
                    UpdateText(false);
                    break;
            }
        }

        private XRDataBuffer? _transformsBuffer;
        public XRDataBuffer? TransformsBuffer => _transformsBuffer;

        private void UpdateText(bool fontChanged)
        {
            if (Font is null)
                return;
            VerifyCreated(fontChanged, Font.Atlas);
            uint count;
            lock (_glyphLock)
            {
                Font.GetQuads(Text, _glyphs);
                count = (uint)(_glyphs?.Count ?? 0);
            }
            ResizeGlyphCount(count);
        }

        private void VerifyCreated(bool fontChanged, XRTexture2D? atlas)
        {
            if (!fontChanged && _rc3D.Mesh is not null || atlas is null)
                return;
            
            if (_rc3D.Mesh is not null)
            {
                _rc3D.Mesh.SettingUniforms -= MeshRend_SettingUniforms;
                _rc3D.Mesh.Destroy();
            }

            var mesh = XRMesh.Create(VertexQuad.PosZ(1.0f, true, 0.0f, false));
            var mat =
            //XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Red);
            new XRMaterial(
                [atlas],
                XRShader.EngineShader(Path.Combine("Common", "Text.vs"), EShaderType.Vertex),
                XRShader.EngineShader(Path.Combine("Common", "Text.fs"), EShaderType.Fragment))
            {
                RenderPass = (int)EDefaultRenderPass.TransparentForward,
                RenderOptions = new()
                {
                    CullMode = ECullMode.None,
                    DepthTest = new()
                    {
                        Enabled = Models.Materials.ERenderParamUsage.Disabled,
                        Function = Models.Materials.EComparison.Always
                    },
                    //RequiredEngineUniforms = Models.Materials.EUniformRequirements.Camera
                }
            };

            var rend = new XRMeshRenderer(mesh, mat);
            rend.SettingUniforms += MeshRend_SettingUniforms;
            CreateSSBOs(rend);
            _rc3D.Mesh = rend;
            _rc2D.Mesh = rend;
        }

        public bool _pushFull = false;
        public bool _dataChanged = false;

        private void MeshRend_SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            if (!_dataChanged)
                return;

            _dataChanged = false;

            if (_pushFull)
            {
                _pushFull = false;
                PushBuffers();
            }
            else
                PushSubBuffers();
        }

        private uint _allocatedGlyphCount = 20;
        private void ResizeGlyphCount(uint count)
        {
            _rc3D.Instances = count;
            _rc2D.Instances = count;
            if (_allocatedGlyphCount < count)
            {
                _allocatedGlyphCount = count;
                _transformsBuffer?.Resize(_allocatedGlyphCount);
                _uvsBuffer?.Resize(_allocatedGlyphCount);
                _pushFull = true;
            }
            _dataChanged = true;

            var tfm = BoundableTransform;
            if (tfm.UsesAutoSizing)
                tfm.InvalidateLayout();
        }

        private static uint GetAllocationLength(uint numGlyphs)
        {
            //Get nearest power of 2 for the number of glyphs
            uint powerOf2 = 1u;
            while (powerOf2 < numGlyphs)
                powerOf2 *= 2u;
            return powerOf2;
        }

        private void CreateSSBOs(XRMeshRenderer meshRend)
        {
            string transformsBindingName = "GlyphTransformsBuffer";
            string uvsBindingName = $"GlyphTexCoordsBuffer";

            //_allocatedGlyphCount = GetAllocationLength();

            _transformsBuffer?.Destroy();
            _transformsBuffer = new(transformsBindingName, EBufferTarget.ShaderStorageBuffer, _allocatedGlyphCount, EComponentType.Float, 4, false, false)
            {
                //RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
                //StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
                Usage = AnimatableTransforms ? EBufferUsage.StreamDraw : EBufferUsage.StaticCopy,
                BindingIndexOverride = 0,
            };

            _uvsBuffer?.Destroy();
            _uvsBuffer = new(uvsBindingName, EBufferTarget.ShaderStorageBuffer, _allocatedGlyphCount, EComponentType.Float, 4, false, false)
            {
                //RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
                //StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
                Usage = EBufferUsage.StaticCopy,
                BindingIndexOverride = 1,
            };

            meshRend.Buffers.Add(transformsBindingName, _transformsBuffer);
            meshRend.Buffers.Add(uvsBindingName, _uvsBuffer);

            _dataChanged = true;
            _pushFull = true;
        }

        private readonly object _glyphLock = new();
        private unsafe void WriteData()
        {
            if (_transformsBuffer is null || _uvsBuffer is null)
                return;
            
            (Vector4 transform, Vector4 uvs)[] glyphsCopy;
            lock (_glyphLock)
                glyphsCopy = [.. _glyphs];

            float* tfmPtr = (float*)_transformsBuffer.Source!.Address.Pointer;
            float* uvsPtr = (float*)_uvsBuffer.Source!.Address.Pointer;

            for (int i = 0; i < glyphsCopy.Length; i++)
            {
                var (transform, uvs) = glyphsCopy[i];

                *tfmPtr++ = transform.X;
                *tfmPtr++ = transform.Y;
                *tfmPtr++ = transform.Z;
                *tfmPtr++ = transform.W;
                //Debug.Out(i.ToString() + ": " + transform.ToString());

                *uvsPtr++ = uvs.X;
                *uvsPtr++ = uvs.Y;
                *uvsPtr++ = uvs.Z;
                *uvsPtr++ = uvs.W;
                //Debug.Out(uvs.ToString());
            }
        }

        private void PushSubBuffers()
        {
            WriteData();
            _transformsBuffer?.PushSubData();
            _uvsBuffer?.PushSubData();
        }
        private void PushBuffers()
        {
            WriteData();
            _transformsBuffer?.PushData();
            _uvsBuffer?.PushData();
        }
    }
}
