﻿using System.Numerics;
using XREngine.Components;
using XREngine.Data.Rendering;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    public class TextComponent : XRComponent, IRenderable
    {
        public TextComponent()
        {
            RenderedObjects[0] = RenderInfo3D.New(this, _rc);
        }

        private readonly RenderCommandMesh3D _rc = new((int)EDefaultRenderPass.TransparentForward);
        public RenderInfo[] RenderedObjects { get; } = new RenderInfo[1];

        private FontGlyphSet? _font;
        private string? _text;
        private bool _animatableTransforms = false;
        private List<(Vector4 transform, Vector4 uvs)>? _glyphs;

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);
            _rc.WorldMatrix = transform.WorldMatrix;
        }

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
                if (_rc.Mesh is not null)
                {
                    _rc.Mesh.SettingUniforms -= MeshRend_SettingUniforms;
                    _rc.Mesh.Destroy();
                }

                var mesh = XRMesh.Create(VertexQuad.PosZ(1.0f, true, 0.0f, false));
                var mat =
                //XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Red);
                new XRMaterial(
                    [Font.Atlas],
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
                _rc.Mesh = rend;
            }
            lock (_glyphLock)
                Font.GetQuads(Text, out _glyphs);
            _rc.Instances = (uint)_glyphs.Count;
            UpdateSSBOs();
        }

        public bool PushFull = false;
        public bool DataChanged = false;

        private void MeshRend_SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            if (!DataChanged)
                return;

            DataChanged = false;

            if (PushFull)
            {
                PushFull = false;
                PushBuffers();
            }
            else
                PushSubBuffers();
        }

        private uint _allocatedGlyphCount = 20;

        private void UpdateSSBOs()
        {
            if (_rc.Mesh is null)
                return;

            if (_glyphs is null || _glyphs.Count == 0)
                return;

            uint length = GetAllocationLength();

            if (_allocatedGlyphCount < length)
            {
                _allocatedGlyphCount = length;
                _transformsBuffer?.Resize(_allocatedGlyphCount);
                _uvsBuffer?.Resize(_allocatedGlyphCount);
                PushFull = true;
            }

            DataChanged = true;
        }

        private uint GetAllocationLength()
        {
            //Get nearest power of 2 for the number of glyphs
            uint numGlyphs = (uint)(_glyphs?.Count ?? 0);
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

            DataChanged = true;
            PushFull = true;
        }

        private object _glyphLock = new();
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
