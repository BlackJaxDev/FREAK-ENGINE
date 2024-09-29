using Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Models.Materials.Shaders.Parameters;
using XREngine.Rendering.Shaders.Generator;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering
{
    /// <summary>
    /// A mesh renderer takes a mesh and a material and renders it.
    /// </summary>
    public class XRMeshRenderer : GenericRenderObject
    {
        public XRMeshRenderer(XRMesh? mesh, XRMaterial? material)
        {
            ArgumentNullException.ThrowIfNull(mesh);
            ArgumentNullException.ThrowIfNull(material);

            _mesh = mesh;
            _material = material;

            InitializeBones();
            InitializeBoneMatricesBuffer();
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
        private Dictionary<uint, Matrix4x4> _modifiedBonesRendering = [];
        private Dictionary<uint, Matrix4x4> _modifiedBonesUpdating = [];

        public string? GenerateVertexShaderSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : ShaderGeneratorBase
        {
            if (Mesh is null)
                return null;

            return ((T)Activator.CreateInstance(typeof(T), Mesh)!).Generate();
        }

        /// <summary>
        /// This is the one bone affecting the transform of this mesh, and is handled differently than if there were multiple.
        /// </summary>
        public TransformBase? SingleBind
        {
            get => _singleBind;
            private set => SetField(ref _singleBind, value);
        }

        private TransformBase? _singleBind = null;

        private void InitializeBones()
        {
            _bones = null;
            SingleBind = null;

            if (Mesh?.UtilizedBones is null)
                return;

            if (Mesh.UtilizedBones.Length == 1)
            {
                SingleBind = Mesh.UtilizedBones[0];
            }
            else if (Mesh.Weights != null && Mesh.Weights.Length > 0)
            {
                _bones = new RenderBone[Mesh.UtilizedBones.Length];
                for (int i = 0; i < _bones.Length; ++i)
                {
                    var rb = new RenderBone(Mesh.UtilizedBones[i], (uint)i + 1u);
                    rb.TransformChanged += BoneTransformUpdated;
                    _bones[i] = rb;
                }

                int facePointCount = Mesh.FaceIndices.Length;
                int[] boneWeightOffsets = new int[facePointCount];
                int[] boneWeightCounts = new int[facePointCount];
                List<int> boneIndices = [];
                List<float> boneWeights = [];

                int offset = 0;
                for (int fpInd = 0; fpInd < facePointCount; ++fpInd)
                {
                    var weightGroup = Mesh.Weights.TryGet(Mesh.FaceIndices[fpInd].WeightIndex);
                    if (weightGroup is null)
                    {
                        boneWeightCounts[fpInd] = 0;
                        boneWeightOffsets[fpInd] = 0;
                        continue;
                    }
                    foreach (var pair in weightGroup.Weights)
                    {
                        int boneIndex = pair.Key;
                        if (boneIndex < 0)
                        {
                            boneWeightCounts[fpInd] = 0;
                            boneWeightOffsets[fpInd] = 0;
                        }
                        else
                        {
                            int count = weightGroup.Weights.Count;

                            boneWeightCounts[fpInd] = count;
                            boneWeightOffsets[fpInd] = offset;
                            offset += count;

                            boneIndices.Add(boneIndex);
                            boneWeights.Add(pair.Value);
                        }
                    }
                }

                Mesh.SetBufferRaw(boneWeightCounts, ECommonBufferType.BoneMatrixCount.ToString(), false, true);
                Mesh.SetBufferRaw(boneWeightOffsets, ECommonBufferType.BoneMatrixOffset.ToString(), false, true);
            }
        }
        private void BoneTransformUpdated(RenderBone bone)
        {
            Matrix4x4 worldMatrix = bone.Transform.WorldMatrix;
            if (!_modifiedBonesUpdating.TryAdd(bone.Index, worldMatrix))
                _modifiedBonesUpdating[bone.Index] = worldMatrix;
        }

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
                BoneMatricesBuffer.PushSubData((int)(bone.Key * 16 * sizeof(float)), 16);
            }
            _modifiedBonesRendering.Clear();
            //BoneMatricesBuffer.PushData();
        }

        /// <summary>
        /// Creates the streamable buffer for bone world transforms.
        /// This is used on the GPU for dynamic skinning.
        /// </summary>
        private void InitializeBoneMatricesBuffer()
        {
            _bones = null;
            BoneMatricesBuffer?.Dispose();
            BoneMatricesBuffer = null;

            if (Mesh is null || Mesh?.UtilizedBones is null || Mesh.UtilizedBones.Length == 0)
                return;

            _bones = new RenderBone[Mesh.UtilizedBones.Length];
            for (int i = 0; i < _bones.Length; ++i)
            {
                var rb = new RenderBone(Mesh.UtilizedBones[i], (uint)i + 1u);
                rb.TransformChanged += BoneTransformUpdated;
                _bones[i] = rb;
            }

            BoneMatricesBuffer = new XRDataBuffer(EBufferTarget.UniformBuffer, false)
            {
                Mapped = false,
                Usage = EBufferUsage.DynamicDraw,
            };
            List<Matrix4x4> matrices = _bones.Select(x => x.Transform.WorldMatrix).ToList();
            matrices.Insert(0, Matrix4x4.Identity);
            BoneMatricesBuffer.SetDataRaw(matrices, false);
            foreach (RenderBone bone in _bones)
                _modifiedBonesUpdating.Add(bone.Index, bone.Transform.WorldMatrix);
        }

        /// <summary>
        /// All bone matrices for the mesh.
        /// Stream-write buffer.
        /// </summary>
        public XRDataBuffer? BoneMatricesBuffer { get; private set; }
        /// <summary>
        /// Weight values from 0.0 to 1.0 for each blendshape that affects each vertex.
        /// Same length as BlendshapeIndices, stream-write buffer.
        /// </summary>
        public XRDataBuffer? BlendshapeWeights { get; private set; }

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