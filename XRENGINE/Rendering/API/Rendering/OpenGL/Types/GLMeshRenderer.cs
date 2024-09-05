﻿using Extensions;
using Silk.NET.OpenGL;
using System.ComponentModel;
using System.Numerics;
using XREngine.Data;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        /// <summary>
        /// Used to render raw primitive data.
        /// </summary>
        public partial class GLMeshRenderer(OpenGLRenderer renderer, XRMeshRenderer mesh) : GLObject<XRMeshRenderer>(renderer, mesh)
        {
            public override GLObjectType Type => GLObjectType.VertexArray;

            public delegate void DelSettingUniforms(GLRenderProgram vertexProgram, GLRenderProgram materialProgram);

            //Vertex buffer information
            private Dictionary<string, GLDataBuffer> _buffers = [];
            private GLRenderProgramPipeline? _pipeline; //Used to connect the material shader(s) to the vertex shader

            /// <summary>
            /// Combined shader program. If this is null, the vertex and fragment etc shaders are separate.
            /// This program may be cached and reused due to the nature of multiple possible combinations of shaders.
            /// </summary>
            private GLRenderProgram? _combinedProgram;

            /// <summary>
            /// This is the program that will be used to render the mesh.
            /// Use this for setting buffer bindings and uniforms.
            /// Either the combined program, the material's program if it contains a vertex shader, or the default vertex program.
            /// </summary>
            public GLRenderProgram VertexProgram
            {
                get
                {
                    if (_combinedProgram is not null)
                        return _combinedProgram;
                    
                    if (Material?.Program?.Data.ShaderTypeMask.HasFlag(EProgramStageMask.VertexShaderBit) ?? false)
                        return Material.Program!;

                    return _defaultVertexProgram!;
                }
            }

            /// <summary>
            /// Default vertex shader for this mesh. Used when the material doesn't have a vertex shader, and shader pipelines are enabled.
            /// </summary>
            private GLRenderProgram? _defaultVertexProgram;

            public GLDataBuffer? TriangleIndicesBuffer
            {
                get => _triangleIndicesBuffer;
                private set => _triangleIndicesBuffer = value;
            }
            public GLDataBuffer? LineIndicesBuffer
            {
                get => _lineIndicesBuffer;
                private set => _lineIndicesBuffer = value;
            }
            public GLDataBuffer? PointIndicesBuffer
            {
                get => _pointIndicesBuffer;
                private set => _pointIndicesBuffer = value;
            }

            public IndexSize TrianglesElementType
            {
                get => _trianglesElementType;
                private set => _trianglesElementType = value;
            }
            public IndexSize LineIndicesElementType
            {
                get => _lineIndicesElementType;
                private set => _lineIndicesElementType = value;
            }
            public IndexSize PointIndicesElementType
            {
                get => _pointIndicesElementType;
                private set => _pointIndicesElementType = value;
            }

            private GLDataBuffer? _triangleIndicesBuffer = null;
            private GLDataBuffer? _lineIndicesBuffer = null;
            private GLDataBuffer? _pointIndicesBuffer = null;
            private IndexSize _trianglesElementType;
            private IndexSize _lineIndicesElementType;
            private IndexSize _pointIndicesElementType;

            /// <summary>
            /// Determines how to use the results of the <see cref="ConditionalRenderQuery"/>.
            /// </summary>
            public EConditionalRenderType ConditionalRenderType { get; set; } = EConditionalRenderType.QueryNoWait;
            /// <summary>
            /// A render query that is used to determine if this mesh should be rendered or not.
            /// </summary>
            public GLRenderQuery? ConditionalRenderQuery { get; set; } = null;

            public uint Instances { get; set; } = 1;
            public GLMaterial? Material { get; set; }

            protected override void LinkData()
            {
                if (Data.Mesh != null)
                    Data.Mesh.DataChanged += OnMeshChanged;
                
                OnMeshChanged(Data.Mesh);
                Data.RenderRequested += Render;
                Data.PropertyChanged += OnDataPropertyChanged;
                Data.PropertyChanging += OnDataPropertyChanging;
                base.LinkData();
            }

            protected override void UnlinkData()
            {
                base.UnlinkData();

                if (Data.Mesh != null)
                    Data.Mesh.DataChanged -= OnMeshChanged;

                Data.RenderRequested -= Render;
                Data.PropertyChanged -= OnDataPropertyChanged;
                Data.PropertyChanging -= OnDataPropertyChanging;

                Destroy();
            }

            private void OnDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(XRMeshRenderer.Mesh):
                        OnMeshChanged(Data.Mesh);
                        break;
                }
            }

            private void OnDataPropertyChanging(object? sender, PropertyChangingEventArgs e)
            {
                //switch (e.PropertyName)
                //{
                //    case nameof(XRMeshRenderer.Mesh):
                //        DestroySkinningBuffers();
                //        break;
                //}
            }

            private void MakeIndexBuffers()
            {
                _triangleIndicesBuffer?.Destroy();
                _triangleIndicesBuffer = null;

                _lineIndicesBuffer?.Destroy();
                _lineIndicesBuffer = null;

                _pointIndicesBuffer?.Destroy();
                _pointIndicesBuffer = null;

                var mesh = Data.Mesh;
                if (mesh is null)
                    return;

                SetIndexBuffer(ref _triangleIndicesBuffer, ref _trianglesElementType, mesh, EPrimitiveType.Triangles);
                SetIndexBuffer(ref _lineIndicesBuffer, ref _lineIndicesElementType, mesh, EPrimitiveType.Lines);
                SetIndexBuffer(ref _pointIndicesBuffer, ref _pointIndicesElementType, mesh, EPrimitiveType.Points);
            }

            private void SetIndexBuffer(ref GLDataBuffer? buffer, ref IndexSize bufferElementSize, XRMesh mesh, EPrimitiveType type)
            {
                var indices = mesh.GetIndices(type);
                if (indices is null)
                    return;

                buffer = new GLDataBuffer(Renderer, new XRDataBuffer(EBufferTarget.ElementArrayBuffer, true)) { MeshRenderer = this };
                //TODO: primitive restart will use MaxValue for restart id
                if (mesh.FaceIndices.Length < byte.MaxValue)
                {
                    bufferElementSize = IndexSize.Byte;
                    buffer.Data.SetDataRaw(indices?.Select(x => (byte)x)?.ToList() ?? []);
                }
                else if (mesh.FaceIndices.Length < short.MaxValue)
                {
                    bufferElementSize = IndexSize.TwoBytes;
                    buffer.Data.SetDataRaw(indices?.Select(x => (ushort)x)?.ToList() ?? []);
                }
                else
                {
                    bufferElementSize = IndexSize.FourBytes;
                    buffer.Data.SetDataRaw(indices);
                }
            }

            private void OnMeshChanged(XRMesh? mesh)
            {
                _defaultVertexProgram?.Destroy();
                _defaultVertexProgram = null;

                _combinedProgram?.Destroy();
                _combinedProgram = null;
            }

            public static class CommonBindingNames
            {
                public const string BoneIndices = "MatrixIds";

                public static string BoneBlockName = "Bones";
            }

            public GLMaterial GetRenderMaterial(XRMaterial? localMaterialOverride = null) =>
                Renderer.GlobalMaterialOverride ?? 
                (localMaterialOverride is null ? null : (Renderer.GetOrCreateAPIRenderObject(localMaterialOverride) as GLMaterial)) ??
                Material ?? 
                Renderer.InvalidMaterial;

            public void Render(Matrix4x4 modelMatrix, XRMaterial? materialOverride, uint instances)
            {
                if (Data is null || !Renderer.Active)
                    return;

                GLMaterial material = GetRenderMaterial(materialOverride);
                if (material is null)
                    return;

                if (!IsGenerated)
                    Generate();

                if (Data.SingleBind != null)
                    modelMatrix *= Data.SingleBind.WorldMatrix;
                
                GLRenderProgram vertexProgram, materialProgram;
                if (Engine.Rendering.Settings.AllowShaderPipelines && _pipeline is not null)
                {
                    materialProgram = material.Program!;

                    _pipeline.Bind();
                    _pipeline.Clear(EProgramStageMask.AllShaderBits);
                    _pipeline.Set(materialProgram.Data.ShaderTypeMask, materialProgram);
                    
                    //If the program doesn't override the vertex shader, use the default one for this mesh
                    if (!materialProgram.Data.ShaderTypeMask.HasFlag(EProgramStageMask.VertexShaderBit))
                    {
                        vertexProgram = _defaultVertexProgram!;
                        _pipeline.Set(EProgramStageMask.VertexShaderBit, vertexProgram);
                    }
                    else
                        vertexProgram = materialProgram;
                }
                else
                {
                    vertexProgram = materialProgram = _combinedProgram!;
                    _combinedProgram!.Use();
                }

                Data.PushBoneMatricesToGPU();

                vertexProgram.Uniform(EEngineUniform.ModelMatrix, modelMatrix);
                //Api.Uniform(vtxId, Uniform.GetLocation(vtxId, ECommonUniform.PrevModelMatrix), _lastRenderedModelMatrix);

                //TODO: normal matrix can be calculated in the shader with invert and transpose of the model matrix
                //vertexProgram.Uniform(Uniform.GetLocation(vertexProgram, EEngineUniform.NormalMatrix), normalMatrix);

                XRCamera? camera = Engine.Rendering.State.CurrentlyRenderingCamera;
                Matrix4x4 worldMatrix;
                Matrix4x4 projMatrix;

                if (camera != null)
                {
                    worldMatrix = camera.Transform.WorldMatrix;
                    projMatrix = camera.ProjectionMatrix;
                }
                else
                {
                    //No camera? Everything will be rendered in world space instead of camera space.
                    //This is used by point lights to render depth cubemaps, for example.
                    worldMatrix = Matrix4x4.Identity;
                    projMatrix = Matrix4x4.Identity;
                }

                vertexProgram.Uniform(EEngineUniform.WorldToCameraSpaceMatrix, worldMatrix);
                vertexProgram.Uniform(EEngineUniform.ProjMatrix, projMatrix);

                material.SetUniforms();

                OnSettingUniforms(vertexProgram, materialProgram);

                Renderer.RenderMesh(this, false, instances);
            }

            private void OnSettingUniforms(GLRenderProgram vertexProgram, GLRenderProgram materialProgram)
                => Data.OnSettingUniforms(vertexProgram.Data, materialProgram.Data);

            protected internal override void PostGenerated()
            {
                MakeIndexBuffers();

                //Determine how we're combining the material and vertex shader here
                if (Engine.Rendering.Settings.AllowShaderPipelines)
                {
                    _combinedProgram = null;
                    _defaultVertexProgram = new GLRenderProgram(Renderer, new XRRenderProgram(new XRShader(EShaderType.Vertex, Data.VertexShaderSource!)));

                    Data.BoneMatricesBuffer?.SetBlockName(_defaultVertexProgram.Data, CommonBindingNames.BoneBlockName);
                }
                else
                {
                    var material = Material ?? Renderer.InvalidMaterial; //Don't use GetRenderMaterial here, global and local override materials are for current render only
                    IEnumerable<XRShader> shaders = material.Data.Shaders;

                    //If the material doesn't have a vertex shader, use the default one
                    bool useDefaultVertexShader = material.Data.VertexShaders.Count == 0;
                    if (useDefaultVertexShader)
                        shaders = shaders.Append(new XRShader(EShaderType.Vertex, Data.VertexShaderSource!));

                    _defaultVertexProgram = null;
                    _combinedProgram = new GLRenderProgram(Renderer, new XRRenderProgram(shaders));

                    if (useDefaultVertexShader)
                        Data.BoneMatricesBuffer?.SetBlockName(_combinedProgram.Data, CommonBindingNames.BoneBlockName);
                }

                Renderer.BindMesh(this);
                BindBuffers();
                Renderer.BindMesh(null);
            }

            /// <summary>
            /// Creates OpenGL API buffers for the mesh's buffers.
            /// </summary>
            public void BindBuffers()
            {
                var mesh = Data.Mesh;
                if (mesh is null)
                    return;

                _buffers = [];
                foreach (var pair in mesh.Buffers)
                {
                    GLDataBuffer buffer = new(Renderer, pair.Value) { MeshRenderer = this };
                    buffer.Generate();
                    _buffers.Add(pair.Key, buffer);
                }

                if (TriangleIndicesBuffer is not null)
                {
                    TriangleIndicesBuffer.Generate();
                    Api.VertexArrayElementBuffer(BindingId, TriangleIndicesBuffer.BindingId);
                }
                if (LineIndicesBuffer is not null)
                {
                    LineIndicesBuffer.Generate();
                    Api.VertexArrayElementBuffer(BindingId, LineIndicesBuffer.BindingId);
                }
                if (PointIndicesBuffer is not null)
                {
                    PointIndicesBuffer.Generate();
                    Api.VertexArrayElementBuffer(BindingId, PointIndicesBuffer.BindingId);
                }
            }

            protected internal override void PostDeleted()
            {
                TriangleIndicesBuffer?.Dispose();
                TriangleIndicesBuffer = null;

                LineIndicesBuffer?.Dispose();
                LineIndicesBuffer = null;

                PointIndicesBuffer?.Dispose();
                PointIndicesBuffer = null;

                _pipeline?.Destroy();
                _pipeline = null;

                _defaultVertexProgram?.Destroy();
                _defaultVertexProgram = null;

                _combinedProgram?.Destroy();
                _combinedProgram = null;

                foreach (var buffer in _buffers)
                    buffer.Value.Destroy();
                _buffers = [];
            }
        }

        private GLMeshRenderer? _currentMesh;

        public virtual void BindMesh(GLMeshRenderer? mesh)
        {
            _currentMesh = mesh;
        }
        public void RenderMesh(GLMeshRenderer manager, bool preservePreviouslyBound = true, uint instances = 1)
        {
            GLMeshRenderer? prev = _currentMesh;
            BindMesh(manager);
            RenderCurrentMesh(instances);
            BindMesh(preservePreviouslyBound ? prev : null);
        }

        //TODO: use instances for left eye, right eye, visible scene mirrors, and shadow maps in parallel
        public void RenderCurrentMesh(uint instances = 1)
        {
            if (_currentMesh?.Data?.Mesh is null)
                return;

            Api.DrawElementsInstancedBaseInstance(
                ToGLEnum(_currentMesh.Data.Mesh.Type),
                _currentMesh.TriangleIndicesBuffer?.Data?.ElementCount ?? 0,
                ToGLEnum(_currentMesh.TrianglesElementType),
                null,
                instances,
                0);
        }

        /// <summary>
        /// Use to globally override the material that meshes render with.
        /// For example, for shadow mapping
        /// </summary>
        public GLMaterial? GlobalMaterialOverride { get; set; }

        /// <summary>
        /// Modifies the rendering API's state to adhere to the given material's settings.
        /// </summary>
        /// <param name="r"></param>
        private void ApplyRenderParameters(RenderingParameters r)
        {
            if (r is null)
                return;

            Api.ColorMask(r.WriteRed, r.WriteGreen, r.WriteBlue, r.WriteAlpha);
            Api.CullFace(ToGLEnum(r.CullMode));

            Api.PointSize(r.PointSize);
            Api.LineWidth(r.LineWidth.Clamp(0.0f, 1.0f));

            if (r.DepthTest.Enabled == ERenderParamUsage.Enabled)
            {
                Api.Enable(EnableCap.DepthTest);
                Api.DepthFunc(ToGLEnum(r.DepthTest.Function));
                Api.DepthMask(r.DepthTest.UpdateDepth);
            }
            else if (r.DepthTest.Enabled == ERenderParamUsage.Disabled)
                Api.Disable(EnableCap.DepthTest);

            if (r.BlendMode.Enabled == ERenderParamUsage.Enabled)
            {
                Api.Enable(EnableCap.Blend);

                Api.BlendEquationSeparate(
                    r.BlendMode.Buffer,
                    ToGLEnum(r.BlendMode.RgbEquation),
                    ToGLEnum(r.BlendMode.AlphaEquation));

                Api.BlendFuncSeparate(
                    r.BlendMode.Buffer,
                    ToGLEnum(r.BlendMode.RgbSrcFactor),
                    ToGLEnum(r.BlendMode.RgbDstFactor),
                    ToGLEnum(r.BlendMode.AlphaSrcFactor),
                    ToGLEnum(r.BlendMode.AlphaDstFactor));
            }
            else if (r.BlendMode.Enabled == ERenderParamUsage.Disabled)
                Api.Disable(EnableCap.Blend);

            //if (r.AlphaTest.Enabled == ERenderParamUsage.Enabled)
            //{
            //    Api.Enable(EnableCap.AlphaTest);
            //    Api.AlphaFunc(AlphaFunction.Never + (int)r.AlphaTest.Comp, r.AlphaTest.Ref);
            //}
            //else if (r.AlphaTest.Enabled == ERenderParamUsage.Disabled)
            //    Api.Disable(EnableCap.AlphaTest);

            if (r.StencilTest.Enabled == ERenderParamUsage.Enabled)
            {
                StencilTest st = r.StencilTest;
                StencilTestFace b = st.BackFace;
                StencilTestFace f = st.FrontFace;
                Api.StencilOpSeparate(GLEnum.Back,
                    (StencilOp)(int)b.BothFailOp,
                    (StencilOp)(int)b.StencilPassDepthFailOp,
                    (StencilOp)(int)b.BothPassOp);
                Api.StencilOpSeparate(GLEnum.Front,
                    (StencilOp)(int)f.BothFailOp,
                    (StencilOp)(int)f.StencilPassDepthFailOp,
                    (StencilOp)(int)f.BothPassOp);
                Api.StencilMaskSeparate(GLEnum.Back, b.WriteMask);
                Api.StencilMaskSeparate(GLEnum.Front, f.WriteMask);
                Api.StencilFuncSeparate(GLEnum.Back,
                    StencilFunction.Never + (int)b.Func, b.Ref, b.ReadMask);
                Api.StencilFuncSeparate(GLEnum.Front,
                    StencilFunction.Never + (int)f.Func, f.Ref, f.ReadMask);
            }
            else if (r.StencilTest.Enabled == ERenderParamUsage.Disabled)
            {
                //GL.Disable(EnableCap.StencilTest);
                Api.StencilMask(0);
                Api.StencilOp(GLEnum.Keep, GLEnum.Keep, GLEnum.Keep);
                Api.StencilFunc(StencilFunction.Always, 0, 0);
            }
        }

        private GLEnum ToGLEnum(EBlendingFactor factor)
            => factor switch
            {
                EBlendingFactor.Zero => GLEnum.Zero,
                EBlendingFactor.One => GLEnum.One,
                EBlendingFactor.SrcColor => GLEnum.SrcColor,
                EBlendingFactor.OneMinusSrcColor => GLEnum.OneMinusSrcColor,
                EBlendingFactor.DstColor => GLEnum.DstColor,
                EBlendingFactor.OneMinusDstColor => GLEnum.OneMinusDstColor,
                EBlendingFactor.SrcAlpha => GLEnum.SrcAlpha,
                EBlendingFactor.OneMinusSrcAlpha => GLEnum.OneMinusSrcAlpha,
                EBlendingFactor.DstAlpha => GLEnum.DstAlpha,
                EBlendingFactor.OneMinusDstAlpha => GLEnum.OneMinusDstAlpha,
                EBlendingFactor.ConstantColor => GLEnum.ConstantColor,
                EBlendingFactor.OneMinusConstantColor => GLEnum.OneMinusConstantColor,
                EBlendingFactor.ConstantAlpha => GLEnum.ConstantAlpha,
                EBlendingFactor.OneMinusConstantAlpha => GLEnum.OneMinusConstantAlpha,
                EBlendingFactor.SrcAlphaSaturate => GLEnum.SrcAlphaSaturate,
                _ => GLEnum.Zero,
            };

        private GLEnum ToGLEnum(EBlendEquationMode equation)
            => equation switch
            {
                EBlendEquationMode.FuncAdd => GLEnum.FuncAdd,
                EBlendEquationMode.FuncSubtract => GLEnum.FuncSubtract,
                EBlendEquationMode.FuncReverseSubtract => GLEnum.FuncReverseSubtract,
                EBlendEquationMode.Min => GLEnum.Min,
                EBlendEquationMode.Max => GLEnum.Max,
                _ => GLEnum.FuncAdd,
            };

        private GLEnum ToGLEnum(EComparison function)
            => function switch
            {
                EComparison.Never => GLEnum.Never,
                EComparison.Less => GLEnum.Less,
                EComparison.Equal => GLEnum.Equal,
                EComparison.Lequal => GLEnum.Lequal,
                EComparison.Greater => GLEnum.Greater,
                EComparison.Nequal => GLEnum.Notequal,
                EComparison.Gequal => GLEnum.Gequal,
                EComparison.Always => GLEnum.Always,
                _ => GLEnum.Never,
            };

        private GLEnum ToGLEnum(ECulling cullMode)
            => cullMode switch
            {
                ECulling.None => GLEnum.None,
                ECulling.Front => GLEnum.Front,
                ECulling.Back => GLEnum.Back,
                _ => GLEnum.FrontAndBack,
            };

        private GLEnum ToGLEnum(IndexSize elementType)
            => elementType switch
            {
                IndexSize.Byte => GLEnum.UnsignedByte,
                IndexSize.TwoBytes => GLEnum.UnsignedShort,
                IndexSize.FourBytes => GLEnum.UnsignedInt,
                _ => GLEnum.UnsignedInt,
            };

        private GLEnum ToGLEnum(EPrimitiveType type)
            => type switch
            {
                EPrimitiveType.Points => GLEnum.Points,
                EPrimitiveType.Lines => GLEnum.Lines,
                EPrimitiveType.LineLoop => GLEnum.LineLoop,
                EPrimitiveType.LineStrip => GLEnum.LineStrip,
                EPrimitiveType.Triangles => GLEnum.Triangles,
                EPrimitiveType.TriangleStrip => GLEnum.TriangleStrip,
                EPrimitiveType.TriangleFan => GLEnum.TriangleFan,
                EPrimitiveType.LinesAdjacency => GLEnum.LinesAdjacency,
                EPrimitiveType.LineStripAdjacency => GLEnum.LineStripAdjacency,
                EPrimitiveType.TrianglesAdjacency => GLEnum.TrianglesAdjacency,
                EPrimitiveType.TriangleStripAdjacency => GLEnum.TriangleStripAdjacency,
                EPrimitiveType.Patches => GLEnum.Patches,
                _ => GLEnum.Triangles,
            };
    }
}