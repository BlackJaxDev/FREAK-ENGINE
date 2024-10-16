﻿using Extensions;
using Silk.NET.OpenGL;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
            private Dictionary<string, GLDataBuffer> _bufferCache = [];
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
                    
                    if (Material?.Program?.Data.GetShaderTypeMask().HasFlag(EProgramStageMask.VertexShaderBit) ?? false)
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
            public GLMaterial? Material => Renderer.GenericToAPI<GLMaterial>(Data.Material);

            protected override void LinkData()
            {
                Data.RenderRequested += Render;
                Data.PropertyChanged += OnDataPropertyChanged;
                Data.PropertyChanging += OnDataPropertyChanging;

                if (Data.Mesh != null)
                    Data.Mesh.DataChanged += OnMeshChanged;
                OnMeshChanged(Data.Mesh);

            }

            protected override void UnlinkData()
            {
                Data.RenderRequested -= Render;
                Data.PropertyChanged -= OnDataPropertyChanged;
                Data.PropertyChanging -= OnDataPropertyChanging;

                if (Data.Mesh != null)
                    Data.Mesh.DataChanged -= OnMeshChanged;
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
                if (indices is null || indices.Length == 0)
                    return;

                buffer = Renderer.GenericToAPI<GLDataBuffer>(new XRDataBuffer(EBufferTarget.ElementArrayBuffer, true) { BindingName = type.ToString() })!;
                //TODO: primitive restart will use MaxValue for restart id
                if (mesh.VertexCount < byte.MaxValue)
                {
                    bufferElementSize = IndexSize.Byte;
                    buffer.Data.SetDataRaw(indices?.Select(x => (byte)x)?.ToList() ?? []);
                }
                else if (mesh.VertexCount < short.MaxValue)
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

            public GLMaterial GetRenderMaterial(XRMaterial? localMaterialOverride = null)
            {
                var globalMaterialOverride = Engine.Rendering.State.PipelineState?.GlobalMaterialOverride;
                var mat =
                    (globalMaterialOverride is null ? null : (Renderer.GetOrCreateAPIRenderObject(globalMaterialOverride) as GLMaterial)) ??
                    (localMaterialOverride is null ? null : (Renderer.GetOrCreateAPIRenderObject(localMaterialOverride) as GLMaterial)) ??
                    Material;

                if (mat is not null)
                    return mat;

                Debug.LogWarning("No material found for mesh renderer, using invalid material.");
                mat = Renderer.GenericToAPI<GLMaterial>(Engine.Rendering.State.CurrentPipeline!.InvalidMaterial)!;
                return mat;
            }

            public void Render(Matrix4x4 modelMatrix, XRMaterial? materialOverride, uint instances)
            {
                if (Data is null || !Renderer.Active)
                    return;

                if (!IsGenerated)
                    Generate();

                GLMaterial material = GetRenderMaterial(materialOverride);
                if (GetPrograms(material,
                    out GLRenderProgram? vertexProgram,
                    out GLRenderProgram? materialProgram))
                {
                    //Api.BindFragDataLocation(materialProgram.BindingId, 0, "OutColor");

                    if (!BuffersBound)
                        return;

                    //if (Data.SingleBind != null)
                    //    modelMatrix *= Data.SingleBind.WorldMatrix;

                    Data.PushBoneMatricesToGPU();
                    SetMeshUniforms(modelMatrix, vertexProgram!);
                    material.SetUniforms(materialProgram);
                    OnSettingUniforms(vertexProgram!, materialProgram!);
                    BindBuffers(vertexProgram!);
                    Renderer.RenderMesh(this, false, instances);
                }
                else
                {
                    //Debug.LogWarning("Failed to get programs for mesh renderer.");
                }
            }

            private void BindBuffers(GLRenderProgram vertexProgram)
            {
                //TODO: make a more efficient way to bind these right before rendering (because apparently re-bufferbase-ing is important?)
                foreach (var buffer in _bufferCache.Where(x => x.Value.Data.Target == EBufferTarget.ShaderStorageBuffer))
                {
                    var b = buffer.Value;
                    b.Bind();
                    uint resourceIndex = Api.GetProgramResourceIndex(vertexProgram!.BindingId, GLEnum.ShaderStorageBlock, b.Data.BindingName);
                    Api.BindBufferBase(ToGLEnum(EBufferTarget.ShaderStorageBuffer), resourceIndex, b.BindingId);
                    //b.PushSubData();
                    b.Unbind();
                }
            }

            private bool GetPrograms(GLMaterial material, [MaybeNullWhen(false)] out GLRenderProgram? vertexProgram, [MaybeNullWhen(false)] out GLRenderProgram? materialProgram)
            {
                if (Engine.Rendering.Settings.AllowShaderPipelines)
                    return GetPipelinePrograms(material, out vertexProgram, out materialProgram);
                else
                    return GetCombinedProgram(out vertexProgram, out materialProgram);
            }

            private bool GetCombinedProgram(out GLRenderProgram? vertexProgram, out GLRenderProgram? materialProgram)
            {
                if ((vertexProgram = materialProgram = _combinedProgram) is null)
                    return false;

                if (!vertexProgram.Link())
                {
                    vertexProgram = null;
                    return false;
                }

                vertexProgram.Use();
                return true;
            }

            private bool GetPipelinePrograms(GLMaterial material, out GLRenderProgram? vertexProgram, out GLRenderProgram? materialProgram)
            {
                _pipeline ??= Renderer.GenericToAPI<GLRenderProgramPipeline>(new XRRenderProgramPipeline())!;
                _pipeline.Bind();
                _pipeline.Clear(EProgramStageMask.AllShaderBits);

                materialProgram = material.Program;
                
                var mask = materialProgram?.Data?.GetShaderTypeMask() ?? EProgramStageMask.None;
                if (!mask.HasFlag(EProgramStageMask.VertexShaderBit))
                {
                    //If the material doesn't have a custom vertex shader, generate the default one for this mesh
                    vertexProgram = _defaultVertexProgram;

                    if (materialProgram?.Link() ?? false)
                        _pipeline.Set(mask, materialProgram);
                    else
                        return false;

                    if (vertexProgram?.Link() ?? false)
                        _pipeline.Set(EProgramStageMask.VertexShaderBit, vertexProgram);
                    else
                        return false;
                }
                else
                {
                    vertexProgram = materialProgram;

                    if (materialProgram?.Link() ?? false)
                        _pipeline.Set(mask, materialProgram);
                    else
                        return false;
                }

                return true;
            }

            private static void SetMeshUniforms(Matrix4x4 modelMatrix, GLRenderProgram vertexProgram)
            {
                XRCamera? camera = Engine.Rendering.State.RenderingCamera;
                Matrix4x4 inverseViewMatrix;
                Matrix4x4 projMatrix;

                if (camera != null)
                {
                    inverseViewMatrix = camera.Transform.WorldMatrix;
                    projMatrix = camera.ProjectionMatrix;
                }
                else
                {
                    //No camera? Everything will be rendered in NDC space instead of world space.
                    //This is used by point lights to render depth cubemaps, for example.
                    inverseViewMatrix = Matrix4x4.Identity;
                    projMatrix = Matrix4x4.Identity;
                }

                vertexProgram.Uniform(EEngineUniform.ModelMatrix, modelMatrix);
                vertexProgram.Uniform(EEngineUniform.InverseViewMatrix, inverseViewMatrix);
                vertexProgram.Uniform(EEngineUniform.ProjMatrix, projMatrix);
            }

            private void OnSettingUniforms(GLRenderProgram vertexProgram, GLRenderProgram materialProgram)
                => Data.OnSettingUniforms(vertexProgram.Data, materialProgram.Data);

            protected internal override void PostGenerated()
            {
                if (Data.GenerateAsync)
                    Task.Run(GenProgramsAndBuffers);
                else
                    GenProgramsAndBuffers();
            }

            private void GenProgramsAndBuffers()
            {
                MakeIndexBuffers();

                //Determine how we're combining the material and vertex shader here
                GLRenderProgram vertexProgram;
                if (Engine.Rendering.Settings.AllowShaderPipelines)
                {
                    _combinedProgram = null;
                    _defaultVertexProgram = Renderer.GenericToAPI<GLRenderProgram>(new XRRenderProgram(false, new XRShader(EShaderType.Vertex, Data.VertexShaderSource!)))!;
                    _defaultVertexProgram.PropertyChanged += SeparatedProgramPropertyChanged;

                    vertexProgram = _defaultVertexProgram;
                }
                else
                {
                    var material = Material;
                    if (material is null)
                    {
                        Debug.LogWarning("No material found for mesh renderer, using invalid material.");
                        material = Renderer.GenericToAPI<GLMaterial>(Engine.Rendering.State.CurrentPipeline!.InvalidMaterial); //Don't use GetRenderMaterial here, global and local override materials are for current render only
                    }
                    IEnumerable<XRShader> shaders = material!.Data.Shaders;

                    //If the material doesn't have a vertex shader, use the default one
                    bool useDefaultVertexShader = material.Data.VertexShaders.Count == 0;
                    if (useDefaultVertexShader)
                        shaders = shaders.Append(new XRShader(EShaderType.Vertex, Data.VertexShaderSource!));

                    _defaultVertexProgram = null;

                    _combinedProgram = Renderer.GenericToAPI<GLRenderProgram>(new XRRenderProgram(shaders, false))!;
                    _combinedProgram.PropertyChanged += CombinedProgramPropertyChanged;

                    vertexProgram = _combinedProgram;
                }

                _bufferCache = [];
                if (Data.Mesh != null)
                    foreach (var pair in (IEventDictionary<string, XRDataBuffer>)Data.Mesh.Buffers)
                        _bufferCache.Add(pair.Key, Renderer.GenericToAPI<GLDataBuffer>(pair.Value)!);
                foreach (var pair in (IEventDictionary<string, XRDataBuffer>)Data.Buffers)
                    _bufferCache.Add(pair.Key, Renderer.GenericToAPI<GLDataBuffer>(pair.Value)!);

                vertexProgram.Data.Link();

                if (!Data.GenerateAsync)
                    vertexProgram.Link();
            }

            private void SeparatedProgramPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName != nameof(GLRenderProgram.IsLinked) || !(_defaultVertexProgram?.IsLinked ?? false))
                    return;
                
                //Continue linking the program
                _defaultVertexProgram.PropertyChanged -= SeparatedProgramPropertyChanged;
                if (Data.GenerateAsync)
                    Engine.EnqueueMainThreadTask(LinkSeparatedVertex);
                else
                    LinkSeparatedVertex();
            }

            private void CombinedProgramPropertyChanged(object? s, PropertyChangedEventArgs e)
            {
                if (e.PropertyName != nameof(GLRenderProgram.IsLinked) || !(_combinedProgram?.IsLinked ?? false))
                    return;
                
                //Continue linking the program
                _combinedProgram.PropertyChanged -= CombinedProgramPropertyChanged;
                if (Data.GenerateAsync)
                    Engine.EnqueueMainThreadTask(LinkCombinedVertex);
                else
                    LinkCombinedVertex();
            }

            private void LinkSeparatedVertex()
            {
                //LinkBlocksToProgram(_defaultVertexProgram!.Data);
                BindBuffers();
            }

            private void LinkCombinedVertex()
            {
                //LinkBlocksToProgram(_combinedProgram!.Data);
                BindBuffers();
            }

            public bool BuffersBound { get; private set; } = false;

            /// <summary>
            /// Creates OpenGL API buffers for the mesh's buffers.
            /// </summary>
            public void BindBuffers()
            {
                var mesh = Data.Mesh;
                if (mesh is null || BuffersBound)
                    return;

                Renderer.BindMesh(this);

                foreach (var buffer in _bufferCache.Values)
                    buffer.Generate();
                
                if (TriangleIndicesBuffer is not null)
                    Api.VertexArrayElementBuffer(BindingId, TriangleIndicesBuffer.BindingId);
                if (LineIndicesBuffer is not null)
                    Api.VertexArrayElementBuffer(BindingId, LineIndicesBuffer.BindingId);
                if (PointIndicesBuffer is not null)
                    Api.VertexArrayElementBuffer(BindingId, PointIndicesBuffer.BindingId);

                Renderer.BindMesh(null);

                BuffersBound = true;
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

                foreach (var buffer in _bufferCache)
                    buffer.Value.Destroy();
                _bufferCache = [];
            }
        }

        public GLMeshRenderer? ActiveMeshRenderer { get; private set; } = null;

        public void BindMesh(GLMeshRenderer? mesh)
        {
            Api.BindVertexArray(mesh?.BindingId ?? 0);
            ActiveMeshRenderer = mesh;
        }
        public void RenderMesh(GLMeshRenderer manager, bool preservePreviouslyBound = true, uint instances = 1)
        {
            GLMeshRenderer? prev = ActiveMeshRenderer;
            BindMesh(manager);
            RenderCurrentMesh(instances);
            BindMesh(preservePreviouslyBound ? prev : null);
        }

        //TODO: use instances for left eye, right eye, visible scene mirrors, and shadow maps in parallel
        public void RenderCurrentMesh(uint instances = 1)
        {
            if (ActiveMeshRenderer?.Data?.Mesh is null)
                return;

            uint triangles = ActiveMeshRenderer.TriangleIndicesBuffer?.Data?.ElementCount ?? 0u;
            uint lines = ActiveMeshRenderer.LineIndicesBuffer?.Data?.ElementCount ?? 0u;
            uint points = ActiveMeshRenderer.PointIndicesBuffer?.Data?.ElementCount ?? 0u;

            if (triangles > 0)
            {
                Api.DrawElements(GLEnum.Triangles, triangles, ToGLEnum(ActiveMeshRenderer.TrianglesElementType), null);
                //Api.DrawElementsInstancedBaseInstance(GLEnum.Triangles, triangles, ToGLEnum(ActiveMeshRenderer.TrianglesElementType), null, instances, 0);
            }
            if (lines > 0)
            {
                Api.DrawElements(GLEnum.Lines, lines, ToGLEnum(ActiveMeshRenderer.LineIndicesElementType), null);
                //Api.DrawElementsInstancedBaseInstance(GLEnum.Lines, lines, ToGLEnum(ActiveMeshRenderer.LineIndicesElementType), null, instances, 0);
            }
            if (points > 0)
            {
                Api.DrawElements(GLEnum.Points, points, ToGLEnum(ActiveMeshRenderer.PointIndicesElementType), null);
                //Api.DrawElementsInstancedBaseInstance(GLEnum.Points, points, ToGLEnum(ActiveMeshRenderer.PointIndicesElementType), null, instances, 0);
            }

            //Api.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit | MemoryBarrierMask.ClientMappedBufferBarrierBit);
        }

        public IGLTexture? BoundTexture { get; set; }

        /// <summary>
        /// Modifies the rendering API's state to adhere to the given material's settings.
        /// </summary>
        /// <param name="r"></param>
        private void ApplyRenderParameters(RenderingParameters r)
        {
            if (r is null)
                return;

            Api.ColorMask(r.WriteRed, r.WriteGreen, r.WriteBlue, r.WriteAlpha);
            if (r.CullMode != ECulling.None)
            {
                Api.Enable(EnableCap.CullFace);
                Api.CullFace(ToGLEnum(r.CullMode));
            }
            else
                Api.Disable(EnableCap.CullFace);

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