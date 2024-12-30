using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models.Materials;
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

        private const string TextColorUniformName = "TextColor";
        private readonly RenderCommandMesh3D _rc3D = new((int)EDefaultRenderPass.TransparentForward);
        private readonly RenderCommandMesh2D _rc2D = new((int)EDefaultRenderPass.TransparentForward);

        public RenderInfo[] RenderedObjects { get; } = new RenderInfo[2];
        public RenderInfo3D RenderInfo3D { get; }
        public RenderInfo2D RenderInfo2D { get; }

        private readonly List<(Vector4 transform, Vector4 uvs)> _glyphs = [];
        private XRDataBuffer? _uvsBuffer;
        private XRDataBuffer? _transformsBuffer;
        private XRDataBuffer? _rotationsBuffer;
        private readonly object _glyphLock = new();
        public bool _pushFull = false;
        public bool _dataChanged = false;
        private uint _allocatedGlyphCount = 20;

        private Dictionary<int, (Vector2 translation, Vector2 scale, float rotation)> _glyphRelativeTransforms = [];
        /// <summary>
        /// If AnimatableTransforms is true, this dictionary can be used to update the position, scale, and rotation of individual glyphs.
        /// </summary>
        public Dictionary<int, (Vector2 translation, Vector2 scale, float rotation)> GlyphRelativeTransforms
        {
            get => _glyphRelativeTransforms;
            set => SetField(ref _glyphRelativeTransforms, value);
        }

        private string? _text;
        /// <summary>
        /// The text to display.
        /// </summary>
        public string? Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }

        private FontGlyphSet? _font;
        /// <summary>
        /// The font to use for the text.
        /// </summary>
        public FontGlyphSet? Font
        {
            get => _font;
            set => SetField(ref _font, value);
        }

        private bool _animatableTransforms = false;
        /// <summary>
        /// If true, individual text character positions can be updated.
        /// </summary>
        public bool AnimatableTransforms
        {
            get => _animatableTransforms;
            set => SetField(ref _animatableTransforms, value);
        }

        private bool _wordWrap = false;
        /// <summary>
        /// If true, the text will wrap to the next line when it reaches the width of the text box.
        /// </summary>
        public bool WordWrap
        {
            get => _wordWrap;
            set => SetField(ref _wordWrap, value);
        }

        private float? _fontSize = 30.0f;
        /// <summary>
        /// The size of the font in points (pt).
        /// If null, the font size will be automatically calculated to fill the transform bounds.
        /// </summary>
        public float? FontSize
        {
            get => _fontSize;
            set => SetField(ref _fontSize, value);
        }

        private bool _hideOverflow = true;
        /// <summary>
        /// If true, text that overflows the bounds of the text box will be hidden.
        /// WordWrap can cause vertical overflow, but otherwise overflow is horizontal.
        /// </summary>
        public bool HideOverflow
        {
            get => _hideOverflow;
            set => SetField(ref _hideOverflow, value);
        }

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);
            _rc3D.WorldMatrix = transform.WorldMatrix;
            _rc2D.WorldMatrix = transform.WorldMatrix;
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

        override protected void OnTransformChanging()
        {
            base.OnTransformChanging();

            if (!SceneNode.TryGetTransformAs<UIBoundableTransform>(out var tfm) || tfm is null)
                return;
            
            tfm.CalcAutoHeightCallback = null;
            tfm.CalcAutoWidthCallback = null;
        }
        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();

            if (!SceneNode.TryGetTransformAs<UIBoundableTransform>(out var tfm) || tfm is null)
                return;
            
            tfm.CalcAutoHeightCallback = CalcAutoHeight;
            tfm.CalcAutoWidthCallback = CalcAutoWidth;
        }

        protected override void UITransformPropertyChanged(object? sender, IXRPropertyChangedEventArgs e)
        {
            base.UITransformPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case nameof(UIBoundableTransform.ActualBottomLeftTranslation):
                case nameof(UIBoundableTransform.ActualSize):
                    UpdateText(false);
                    break;
            }
        }

        //TODO: return and cache max width and height when calculating glyphs instead
        private float CalcAutoWidth(UIBoundableTransform transform)
        {
            //x = pos x, z = scale x
            lock (_glyphLock)
            {
                if (_glyphs is null || _glyphs.Count == 0)
                    return 0.0f;

                if (WordWrap)
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

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Font):
                case nameof(AnimatableTransforms):
                case nameof(NonVertexShadersOverride):
                    UpdateText(true);
                    break;
                case nameof(Text):
                    UpdateText(false);
                    break;
                case nameof(GlyphRelativeTransforms):
                    MarkGlyphTransformsChanged();
                    break;
                case nameof(RenderPass):
                    {
                        var mat = _rc3D.Mesh?.Material;
                        if (mat is not null)
                            mat.RenderPass = RenderPass;
                        else
                            UpdateText(true);
                    }
                    break;
                case nameof(RenderParameters):
                    {
                        var mat = _rc3D.Mesh?.Material;
                        if (mat is not null)
                            mat.RenderOptions = RenderParameters;
                        else
                            UpdateText(true);
                    }
                    break;
                case nameof(Color):
                    {
                        var mat = _rc3D.Mesh?.Material;
                        if (mat is not null)
                            mat.SetVector4(TextColorUniformName, Color);
                        else
                            UpdateText(true);
                    }
                    break;
            }
        }

        /// <summary>
        /// Retrieves glyph data from the font and resizes SSBOs if necessary.
        /// Verifies that the mesh is created and the font atlas is loaded.
        /// </summary>
        /// <param name="forceRemake"></param>
        private void UpdateText(bool forceRemake)
        {
            Font ??= FontGlyphSet.LoadDefaultFont();
            VerifyCreated(forceRemake, Font.Atlas);
            uint count;
            lock (_glyphLock)
            {
                var tfm = BoundableTransform;
                Font.GetQuads(Text, _glyphs, FontSize, null, null, WordWrap, 5.0f);
                count = (uint)(_glyphs?.Count ?? 0);
            }
            ResizeGlyphCount(count);
        }

        /// <summary>
        /// Ensures that the mesh is created and the font atlas is loaded, and creates the material and SSBOs.
        /// </summary>
        /// <param name="forceRemake"></param>
        /// <param name="atlas"></param>
        private void VerifyCreated(bool forceRemake, XRTexture2D? atlas)
        {
            if (!forceRemake && _rc3D.Mesh is not null || atlas is null)
                return;

            if (_rc3D.Mesh is not null)
            {
                _rc3D.Mesh.SettingUniforms -= MeshRend_SettingUniforms;
                _rc3D.Mesh.Destroy();
            }

            var mesh = XRMesh.Create(VertexQuad.PosZ(1.0f, true, 0.0f, false));
            XRMaterial mat = CreateMaterial(atlas);

            var rend = new XRMeshRenderer(mesh, mat);
            rend.SettingUniforms += MeshRend_SettingUniforms;
            CreateSSBOs(rend);
            _rc3D.Mesh = rend;
            _rc2D.Mesh = rend;
        }

        private int _renderPass = (int)EDefaultRenderPass.TransparentForward;
        public int RenderPass
        {
            get => _renderPass;
            set => SetField(ref _renderPass, value);
        }

        private RenderingParameters _renderParameters = new()
        {
            CullMode = ECullMode.None,
            DepthTest = new()
            {
                Enabled = ERenderParamUsage.Disabled,
                Function = EComparison.Always
            },
            BlendModeAllDrawBuffers = BlendMode.EnabledTransparent(),
        };
        public RenderingParameters RenderParameters
        {
            get => _renderParameters;
            set => SetField(ref _renderParameters, value);
        }

        private XRShader[]? _nonVertexShadersOverride;
        /// <summary>
        /// Override property for all non-vertex shaders for the text material.
        /// When null, the default fragment shader for text is used.
        /// </summary>
        public XRShader[]? NonVertexShadersOverride
        {
            get => _nonVertexShadersOverride;
            set => SetField(ref _nonVertexShadersOverride, value);
        }

        private ColorF4 _color = new(0.0f, 0.0f, 0.0f, 1.0f);
        public ColorF4 Color
        {
            get => _color;
            set => SetField(ref _color, value);
        }

        /// <summary>
        /// Override this method to create a fully custom material for the text using the font's atlas.
        /// </summary>
        /// <param name="atlas"></param>
        /// <returns></returns>
        protected virtual XRMaterial CreateMaterial(XRTexture2D atlas)
        {
            XRShader vertexShader = XRShader.EngineShader(Path.Combine("Common", "Text.vs"), EShaderType.Vertex);
            XRShader[] nonVertexShaders = NonVertexShadersOverride ?? [XRShader.EngineShader(Path.Combine("Common", "Text.fs"), EShaderType.Fragment)];
            return new([new ShaderVector4(Color, TextColorUniformName)], [atlas], new XRShader[] { vertexShader }.Concat(nonVertexShaders))
            {
                RenderPass = RenderPass,
                RenderOptions = RenderParameters
            };
        }

        /// <summary>
        /// If data has changed, this method will update the SSBOs with the new glyph data and push it to the GPU.
        /// </summary>
        /// <param name="vertexProgram"></param>
        /// <param name="materialProgram"></param>
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

        /// <summary>
        /// Resizes all SSBOs, sets glyph instance count, and invalidates layout if auto-sizing is enabled.
        /// </summary>
        /// <param name="count"></param>
        private void ResizeGlyphCount(uint count)
        {
            _rc3D.Instances = count;
            _rc2D.Instances = count;
            if (_allocatedGlyphCount < count)
            {
                _allocatedGlyphCount = count;
                _transformsBuffer?.Resize(_allocatedGlyphCount);
                _uvsBuffer?.Resize(_allocatedGlyphCount);
                _rotationsBuffer?.Resize(_allocatedGlyphCount);
                _pushFull = true;
            }
            _dataChanged = true;

            var tfm = BoundableTransform;
            if (tfm.UsesAutoSizing)
                tfm.InvalidateLayout();
        }

        /// <summary>
        /// Returns a power of 2 allocation length for the number of glyphs.
        /// This is similar to how a list object would allocate memory with an internal array.
        /// </summary>
        /// <param name="numGlyphs"></param>
        /// <returns></returns>
        private static uint GetAllocationLength(uint numGlyphs)
        {
            //Get nearest power of 2 for the number of glyphs
            uint powerOf2 = 1u;
            while (powerOf2 < numGlyphs)
                powerOf2 *= 2u;
            return powerOf2;
        }

        /// <summary>
        /// Recreates SSBOs for the text glyph data and assigns them to the mesh renderer.
        /// </summary>
        /// <param name="meshRend"></param>
        private void CreateSSBOs(XRMeshRenderer meshRend)
        {
            string transformsBindingName = "GlyphTransformsBuffer";
            string uvsBindingName = $"GlyphTexCoordsBuffer";
            string rotationsBindingName = "GlyphRotationsBuffer";

            //TODO: use memory mapping instead of pushing

            //_allocatedGlyphCount = GetAllocationLength();

            meshRend.Buffers.Remove(transformsBindingName);
            _transformsBuffer?.Destroy();
            _transformsBuffer = new(transformsBindingName, EBufferTarget.ShaderStorageBuffer, _allocatedGlyphCount, EComponentType.Float, 4, false, false)
            {
                //RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
                //StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
                Usage = AnimatableTransforms ? EBufferUsage.StreamDraw : EBufferUsage.StaticCopy,
                BindingIndexOverride = 0,
            };
            meshRend.Buffers.Add(transformsBindingName, _transformsBuffer);

            meshRend.Buffers.Remove(uvsBindingName);
            _uvsBuffer?.Destroy();
            _uvsBuffer = new(uvsBindingName, EBufferTarget.ShaderStorageBuffer, _allocatedGlyphCount, EComponentType.Float, 4, false, false)
            {
                //RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
                //StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
                Usage = EBufferUsage.StaticCopy,
                BindingIndexOverride = 1,
            };
            meshRend.Buffers.Add(uvsBindingName, _uvsBuffer);

            meshRend.Buffers.Remove(rotationsBindingName);
            _rotationsBuffer?.Destroy();

            if (AnimatableTransforms)
            {
                _rotationsBuffer = new(rotationsBindingName, EBufferTarget.ShaderStorageBuffer, _allocatedGlyphCount, EComponentType.Float, 1, false, false)
                {
                    //RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
                    //StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
                    Usage = EBufferUsage.StaticCopy,
                    BindingIndexOverride = 2,
                };
                meshRend.Buffers.Add(rotationsBindingName, _rotationsBuffer);
            }

            _dataChanged = true;
            _pushFull = true;
        }

        /// <summary>
        /// MarkGlyphTransformsChanged should be called after updating GlyphRelativeTransforms.
        /// This will update the transforms buffer with the new values.
        /// </summary>
        public void MarkGlyphTransformsChanged()
            => _dataChanged = true;

        /// <summary>
        /// Writes the glyph data to the SSBOs.
        /// </summary>
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
                (Vector4 transform, Vector4 uvs) = glyphsCopy[i];

                if (AnimatableTransforms && GlyphRelativeTransforms.TryGetValue(i, out var relative))
                {
                    transform.X += relative.translation.X;
                    transform.Y += relative.translation.Y;
                    transform.Z *= relative.scale.X;
                    transform.W *= relative.scale.Y;

                    if (_rotationsBuffer is not null)
                        ((float*)_rotationsBuffer.Source!.Address.Pointer)[i] = relative.rotation;
                }

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

        /// <summary>
        /// Pushes the sub-data of the SSBOs to the GPU.
        /// </summary>
        private void PushSubBuffers()
        {
            WriteData();
            _transformsBuffer?.PushSubData();
            _uvsBuffer?.PushSubData();
            _rotationsBuffer?.PushSubData();
        }

        /// <summary>
        /// Pushes the full data of the SSBOs to the GPU.
        /// </summary>
        private void PushBuffers()
        {
            WriteData();
            _transformsBuffer?.PushData();
            _uvsBuffer?.PushData();
            _rotationsBuffer?.PushData();
        }
    }
}
