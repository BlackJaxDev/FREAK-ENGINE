using Extensions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Models.Materials.Shaders.Parameters;
using XREngine.Rendering.Shaders.Generator;
using XREngine.Scene.Transforms;
using static XREngine.Rendering.XRMesh;

namespace XREngine.Rendering
{
    /// <summary>
    /// A mesh renderer takes a mesh and a material and renders it.
    /// </summary>
    public class XRMeshRenderer : GenericRenderObject
    {
        public XRMeshRenderer(XRMesh? mesh, XRMaterial? material)
        {
            _mesh = mesh;
            _material = material;
            ReinitializeBones();
        }

        private XRMesh? _mesh;
        private XRMaterial? _material;
        private string? _vertexShaderSource;

        public string? VertexShaderSource => _vertexShaderSource ??= GenerateVertexShaderSource<DefaultVertexShaderGenerator>();

        public XRMesh? Mesh 
        {
            get => _mesh;
            set => SetField(ref _mesh, value);
        }
        public XRMaterial? Material
        {
            get => _material;
            set => SetField(ref _material, value);
        }

        private RenderBone[]? _bones;
        private ConcurrentDictionary<uint, Matrix4x4> _modifiedBonesRendering = [];
        private ConcurrentDictionary<uint, Matrix4x4> _modifiedBonesUpdating = [];

        public string? GenerateVertexShaderSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : ShaderGeneratorBase
        {
            if (Mesh is null)
                return null;

            return ((T)Activator.CreateInstance(typeof(T), Mesh)!).Generate();
        }

        private TransformBase? _singleBind = null;
        /// <summary>
        /// This is the one bone affecting the transform of this mesh, and is handled differently than if there were multiple.
        /// </summary>
        public TransformBase? SingleBind
        {
            get => _singleBind;
            private set => SetField(ref _singleBind, value);
        }

        private void ReinitializeBones()
        {
            Engine.Time.Timer.SwapBuffers -= SwapBuffers;
            _bones = null;
            SingleBind = null;

            BoneMatricesBuffer?.Destroy();
            BoneMatricesBuffer = null;
            BoneInvBindMatricesBuffer?.Destroy();
            BoneInvBindMatricesBuffer = null;
            BoneWeightIndices?.Destroy();
            BoneWeightIndices = null;
            BoneWeightValues?.Destroy();
            BoneWeightValues = null;

            if (Mesh?.UtilizedBones is null)
                return;

            if (Mesh.UtilizedBones.Length == 1)
                SingleBind = Mesh.UtilizedBones[0].tfm;
            else if (Mesh.Weights != null && Mesh.Weights.Length > 0)
            {
                GetMatrixArrays(
                    out Matrix4x4[] matrices,
                    out Matrix4x4[] invBindMatrices);

                GetValueArrays(
                    out int[] boneWeightOffsets,
                    out int[] boneWeightCounts,
                    out List<int> boneIndices,
                    out List<float> boneWeights);

                SetSkinningBuffers(
                    matrices,
                    invBindMatrices,
                    boneWeightOffsets,
                    boneWeightCounts,
                    boneIndices,
                    boneWeights);

                Engine.Time.Timer.SwapBuffers += SwapBuffers;
            }
        }

        private void SetSkinningBuffers(Matrix4x4[] matrices, Matrix4x4[] invBindMatrices, int[] boneWeightOffsets, int[] boneWeightCounts, List<int> boneIndices, List<float> boneWeights)
        {
            Buffers.SetBufferRaw(boneWeightCounts, ECommonBufferType.BoneMatrixCount.ToString(), false, true);
            Buffers.SetBufferRaw(boneWeightOffsets, ECommonBufferType.BoneMatrixOffset.ToString(), false, true);

            BoneWeightIndices = Buffers.SetBufferRaw(boneIndices, $"{ECommonBufferType.BoneMatrixIndices}Buffer", false, true, false, 0, EBufferTarget.ShaderStorageBuffer);
            BoneWeightValues = Buffers.SetBufferRaw(boneWeights, $"{ECommonBufferType.BoneMatrixWeights}Buffer", false, false, false, 0, EBufferTarget.ShaderStorageBuffer);

            BoneMatricesBuffer = Buffers.SetBufferRaw(matrices, $"{ECommonBufferType.BoneMatrices}Buffer", false, false, false, 0, EBufferTarget.ShaderStorageBuffer);
            //BoneMatricesBuffer.RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
            //BoneMatricesBuffer.StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
            BoneMatricesBuffer.Usage = EBufferUsage.StreamDraw;
            BoneInvBindMatricesBuffer = Buffers.SetBufferRaw(invBindMatrices, $"{ECommonBufferType.BoneInvBindMatrices}Buffer", false, false, false, 0, EBufferTarget.ShaderStorageBuffer);
        }

        private void GetMatrixArrays(out Matrix4x4[] matrices, out Matrix4x4[] invBindMatrices)
        {
            int arrLen = Mesh!.UtilizedBones.Length + 1;
            matrices = new Matrix4x4[arrLen];
            invBindMatrices = new Matrix4x4[arrLen];
            matrices[0] = Matrix4x4.Identity;
            _bones = new RenderBone[Mesh.UtilizedBones.Length];
            for (int i = 0; i < _bones.Length; ++i)
            {
                var (tfm, invBindWorldMtx) = Mesh.UtilizedBones[i];
                uint boneIndex = (uint)i + 1u;
                Matrix4x4 mtx = tfm.WorldMatrix;

                var rb = new RenderBone(tfm, invBindWorldMtx, boneIndex);
                rb.TransformChanged += BoneTransformUpdated;

                _bones[i] = rb;
                _modifiedBonesUpdating.TryAdd(boneIndex, mtx);

                matrices[i + 1] = mtx;
                invBindMatrices[i + 1] = invBindWorldMtx;
            }
        }

        private void GetValueArrays(out int[] boneWeightOffsets, out int[] boneWeightCounts, out List<int> boneIndices, out List<float> boneWeights)
        {
            int facePointCount = Mesh!.FaceIndices.Length;
            boneWeightOffsets = new int[facePointCount];
            boneWeightCounts = new int[facePointCount];
            boneIndices = [];
            boneWeights = [];
            int offset = 0;
            for (int fpInd = 0; fpInd < facePointCount; ++fpInd)
            {
                var weightGroup = Mesh.Weights.TryGet(Mesh.FaceIndices[fpInd].WeightIndex);
                int count = weightGroup?.Weights?.Count ?? 0;
                boneWeightCounts[fpInd] = count;
                boneWeightOffsets[fpInd] = offset;
                offset += count;

                if (weightGroup is null)
                    continue;

                foreach (var pair in weightGroup.Weights)
                {
                    int boneIndex = pair.Key;
                    float boneWeight = pair.Value;

                    if (boneIndex < 0)
                    {
                        boneIndex = -1;
                        boneWeight = 0.0f;
                    }

                    boneIndices.Add(boneIndex + 1); //+1 because 0 is reserved for the identity matrix
                    boneWeights.Add(boneWeight);
                }
            }
        }

        #region Non-Per-Facepoint Buffers

        #region Bone Weighting Buffers
        //Bone weights
        /// <summary>
        /// Indices into the UtilizedBones list for each bone that affects this vertex.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BoneWeightIndices { get; private set; }
        /// <summary>
        /// Weight values from 0.0 to 1.0 for each bone that affects this vertex.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BoneWeightValues { get; private set; }
        #endregion

        #region Blendshape Buffers
        //Deltas for each blendshape on this mesh
        /// <summary>
        /// Remapped array of position deltas for all blendshapes on this mesh.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BlendshapePositionDeltasBuffer { get; private set; }
        /// <summary>
        /// Remapped array of normal deltas for all blendshapes on this mesh.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BlendshapeNormalDeltasBuffer { get; private set; }
        /// <summary>
        /// Remapped array of tangent deltas for all blendshapes on this mesh.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BlendshapeTangentDeltasBuffer { get; private set; }
        /// <summary>
        /// Remapped array of color deltas for all blendshapes on this mesh.
        /// Static read-only buffers.
        /// </summary>
        public XRDataBuffer[]? BlendshapeColorDeltaBuffers { get; private set; } = [];
        /// <summary>
        /// Remapped array of texture coordinate deltas for all blendshapes on this mesh.
        /// Static read-only buffers.
        /// </summary>
        public XRDataBuffer[]? BlendshapeTexCoordDeltaBuffers { get; private set; } = [];
        //Weights for each blendshape on this mesh
        /// <summary>
        /// Indices into the blendshape delta buffers for each blendshape that affects each vertex.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BlendshapeIndices { get; private set; }
        #endregion

        #endregion

        public BufferCollection Buffers { get; private set; } = [];

        private void BoneTransformUpdated(RenderBone bone)
            => _modifiedBonesUpdating.AddOrUpdate(bone.Index, x => bone.Transform.WorldMatrix, (x, y) => bone.Transform.WorldMatrix);

        public void SwapBuffers()
        {
            (_modifiedBonesRendering, _modifiedBonesUpdating) = (_modifiedBonesUpdating, _modifiedBonesRendering);
            _modifiedBonesUpdating.Clear();
        }

        public void PushBoneMatricesToGPU()
        {
            if (BoneMatricesBuffer is null)
                return;

            //TODO: what's faster, pushing sub data per matrix, or pushing all? or mapping?
            foreach (var bone in _modifiedBonesRendering)
            {
                BoneMatricesBuffer.Set(bone.Key, bone.Value);
                //var elemSize = BoneMatricesBuffer.ElementSize;
                //BoneMatricesBuffer.PushSubData((int)(bone.Key * elemSize), elemSize);
            }
            BoneMatricesBuffer.PushSubData(0, BoneMatricesBuffer.Length);
        }

        /// <summary>
        /// All bone matrices for the mesh.
        /// Stream-write buffer.
        /// </summary>
        public XRDataBuffer? BoneMatricesBuffer { get; private set; }
        /// <summary>
        /// All bone inverse bind matrices for the mesh.
        /// </summary>
        public XRDataBuffer? BoneInvBindMatricesBuffer { get; private set; }
        /// <summary>
        /// Weight values from 0.0 to 1.0 for each blendshape that affects each vertex.
        /// Same length as BlendshapeIndices, stream-write buffer.
        /// </summary>
        public XRDataBuffer? BlendshapeWeights { get; private set; }

        private bool _generateAsync = false;
        public bool GenerateAsync
        {
            get => _generateAsync;
            set => SetField(ref _generateAsync, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Mesh):
                    ReinitializeBones();
                    break;
            }
        }

        public delegate void DelSetUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram);
        /// <summary>
        /// Subscribe to this event to send your own uniforms to the material.
        /// </summary>
        public event DelSetUniforms? SettingUniforms;

        public delegate void DelRenderRequested(Matrix4x4 worldMatrix, XRMaterial? materialOverride, uint instances);
        public delegate ShaderVar DelParameterRequested(int index);

        /// <summary>
        /// Tells all renderers to render this mesh.
        /// </summary>
        public event DelRenderRequested? RenderRequested;

        /// <summary>
        /// Use this to render the mesh with an identity transform matrix.
        /// </summary>
        public void Render(XRMaterial? materialOverride = null, uint instances = 1u)
            => Render(Matrix4x4.Identity, materialOverride, instances);

        /// <summary>
        /// Use this to render the mesh.
        /// </summary>
        /// <param name="worldMatrix"></param>
        /// <param name="materialOverride"></param>
        public void Render(Matrix4x4 worldMatrix, XRMaterial? materialOverride = null, uint instances = 1u)
            => RenderRequested?.Invoke(worldMatrix, materialOverride, instances);

        public T? Parameter<T>(int index) where T : ShaderVar 
            => Material?.Parameter<T>(index);
        public T? Parameter<T>(string name) where T : ShaderVar
            => Material?.Parameter<T>(name);

        public void SetParameter(int index, ColorF4 color) => Parameter<ShaderVector4>(index)?.SetValue(color);
        public void SetParameter(int index, int value) => Parameter<ShaderInt>(index)?.SetValue(value);
        public void SetParameter(int index, float value) => Parameter<ShaderFloat>(index)?.SetValue(value);
        public void SetParameter(int index, Vector2 value) => Parameter<ShaderVector2>(index)?.SetValue(value);
        public void SetParameter(int index, Vector3 value) => Parameter<ShaderVector3>(index)?.SetValue(value);
        public void SetParameter(int index, Vector4 value) => Parameter<ShaderVector4>(index)?.SetValue(value);
        public void SetParameter(int index, Matrix4x4 value) => Parameter<ShaderMat4>(index)?.SetValue(value);

        public void SetParameter(string name, ColorF4 color) => Parameter<ShaderVector4>(name)?.SetValue(color);
        public void SetParameter(string name, int value) => Parameter<ShaderInt>(name)?.SetValue(value);
        public void SetParameter(string name, float value) => Parameter<ShaderFloat>(name)?.SetValue(value);
        public void SetParameter(string name, Vector2 value) => Parameter<ShaderVector2>(name)?.SetValue(value);
        public void SetParameter(string name, Vector3 value) => Parameter<ShaderVector3>(name)?.SetValue(value);
        public void SetParameter(string name, Vector4 value) => Parameter<ShaderVector4>(name)?.SetValue(value);
        public void SetParameter(string name, Matrix4x4 value) => Parameter<ShaderMat4>(name)?.SetValue(value);

        internal void OnSettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
            => SettingUniforms?.Invoke(vertexProgram, materialProgram);
    }
}