using System.Numerics;
using XREngine.Components;
using XREngine.Data.Rendering;
using XREngine.Rendering.Info;

namespace XREngine.Rendering.UI
{
    public class TextComponent : XRComponent, IRenderable
    {
        public TextComponent()
        {
            RenderedObjects[0] = RenderInfo3D.New(this, _rc);
        }

        private FontGlyphSet? _font;
        private string? _text;
        private bool _animatableTransforms = false;
        private List<(Matrix4x4 transform, Vector4 uvs)>? _glyphs;

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
        private XRDataBuffer? _uvsBuffer;

        public XRDataBuffer? TransformsBuffer => _transformsBuffer;

        private void UpdateText(bool fontChanged)
        {
            if (Font?.Atlas is null)
                return;

            if (fontChanged || _rc.Mesh is null)
            {
                _rc.Mesh?.Destroy();
                var mesh = XRMesh.Create(VertexQuad.PosZ(1.0f, true, 0.0f, true));
                var mat = new XRMaterial(
                    [Font.Atlas],
                    XRShader.EngineShader(Path.Combine("Common", "Text.vs"), EShaderType.Vertex),
                    XRShader.EngineShader(Path.Combine("Common", "Text.fs"), EShaderType.Fragment))
                {
                    RenderPass = (int)EDefaultRenderPass.OpaqueForward
                };
                mat.RenderOptions.CullMode = ECullMode.None;
                mat.RenderOptions.DepthTest.Enabled = Models.Materials.ERenderParamUsage.Enabled;
                mat.RenderOptions.DepthTest.Function = Models.Materials.EComparison.Always;
                var meshRend = new XRMeshRenderer(mesh, mat);
                CreateSSBOs(meshRend);
                _rc.Mesh = meshRend;
            }

            Font.GetQuads(Text, out _glyphs);
            _rc.Instances = (uint)_glyphs.Count;
            UpdateSSBOs();
        }

        private uint _allocatedGlyphCount = 0;

        private void UpdateSSBOs()
        {
            if (_rc.Mesh is null)
                return;

            if (_glyphs is null || _glyphs.Count == 0)
                return;

            //Get nearest power of 2 for the number of glyphs
            uint numGlyphs = (uint)_glyphs.Count;
            uint powerOf2 = 1u;
            while (powerOf2 < numGlyphs)
                powerOf2 *= 2u;

            if (_allocatedGlyphCount < powerOf2)
            {
                _allocatedGlyphCount = powerOf2;
                _transformsBuffer?.Resize(_allocatedGlyphCount);
                _uvsBuffer?.Resize(_allocatedGlyphCount);
            }

            WriteData();
        }

        private void CreateSSBOs(XRMeshRenderer meshRend)
        {
            string transformsBindingName = "GlyphTransformsBuffer";
            string uvsBindingName = $"GlyphTexCoordsBuffer";

            _transformsBuffer?.Destroy();
            _transformsBuffer = new(transformsBindingName, EBufferTarget.ShaderStorageBuffer, _allocatedGlyphCount, EComponentType.Float, 16, false, false)
            {
                //RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
                //StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
                Usage = AnimatableTransforms ? EBufferUsage.StreamDraw : EBufferUsage.StaticCopy,
                //BindingIndexOverride = 0,
            };

            _uvsBuffer?.Destroy();
            _uvsBuffer = new(uvsBindingName, EBufferTarget.ShaderStorageBuffer, _allocatedGlyphCount, EComponentType.Float, 4, false, false)
            {
                //RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
                //StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
                Usage = EBufferUsage.StaticCopy,
                //BindingIndexOverride = 1,
            };

            meshRend.Buffers.Add(transformsBindingName, _transformsBuffer);
            meshRend.Buffers.Add(uvsBindingName, _uvsBuffer);
        }

        private unsafe void WriteData()
        {
            if (_glyphs is null || _transformsBuffer is null || _uvsBuffer is null)
                return;

            var tfmPtr = (float*)_transformsBuffer.Source!.Address.Pointer;
            var uvsPtr = (float*)_uvsBuffer.Source!.Address.Pointer;

            for (int i = 0; i < _glyphs.Count; i++)
            {
                var (transform, uvs) = _glyphs[i];

                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 4; x++)
                        *tfmPtr++ = transform[x, y];
                
                for (int j = 0; j < 4; j++)
                    *uvsPtr++ = uvs[j];
            }
        }

        private readonly RenderCommandMesh3D _rc = new((int)EDefaultRenderPass.OpaqueForward);
        public RenderInfo[] RenderedObjects { get; } = new RenderInfo[1];
    }
}
