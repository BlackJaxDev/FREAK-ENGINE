using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Models.Materials.Shaders.Parameters;
using XREngine.Rendering.Shaders.Generator;
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

        public string? GeneratedVertexShaderSource => _vertexShaderSource ??= GenerateVertexShaderSource<DefaultVertexShaderGenerator>();

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

        //private TransformBase? _singleBind = null;
        ///// <summary>
        ///// This is the one bone affecting the transform of this mesh, and is handled differently than if there were multiple.
        ///// </summary>
        //public TransformBase? SingleBind
        //{
        //    get => _singleBind;
        //    private set => SetField(ref _singleBind, value);
        //}

        private void ReinitializeBones()
        {
            //using var timer = Engine.Profiler.Start();

            ResetBoneInfo();

            if (Mesh?.HasSkinning ?? false)
            {
                PopulateBoneMatrixBuffers();
                Engine.Time.Timer.SwapBuffers += SwapBuffers;
            }
        }

        private void ResetBoneInfo()
        {
            Engine.Time.Timer.SwapBuffers -= SwapBuffers;
            _bones = null;
            //SingleBind = null;
            BoneMatricesBuffer?.Destroy();
            BoneMatricesBuffer = null;
            BoneInvBindMatricesBuffer?.Destroy();
            BoneInvBindMatricesBuffer = null;
        }

        public BufferCollection Buffers { get; private set; } = [];

        /// <summary>
        /// All bone matrices for the mesh.
        /// Stream-write buffer.
        /// </summary>
        public XRDataBuffer? BoneMatricesBuffer { get; private set; }

        /// <summary>
        /// All bone inverse bind matrices for the mesh.
        /// </summary>
        public XRDataBuffer? BoneInvBindMatricesBuffer { get; private set; }

        private void PopulateBoneMatrixBuffers()
        {
            //using var timer = Engine.Profiler.Start();

            uint arrLen = (uint)(Mesh?.UtilizedBones?.Length ?? 0);

            //Allocate buffers

            _bones = new RenderBone[arrLen];

            BoneMatricesBuffer = new($"{ECommonBufferType.BoneMatrices}Buffer", EBufferTarget.ShaderStorageBuffer, arrLen + 1, EComponentType.Float, 16, false, false)
            {
                //RangeFlags = EBufferMapRangeFlags.Write | EBufferMapRangeFlags.Persistent | EBufferMapRangeFlags.Coherent;
                //StorageFlags = EBufferMapStorageFlags.Write | EBufferMapStorageFlags.Persistent | EBufferMapStorageFlags.Coherent | EBufferMapStorageFlags.ClientStorage;
                Usage = EBufferUsage.StreamDraw
            };

            BoneInvBindMatricesBuffer = new($"{ECommonBufferType.BoneInvBindMatrices}Buffer", EBufferTarget.ShaderStorageBuffer, arrLen + 1, EComponentType.Float, 16, false, false)
            {
                Usage = EBufferUsage.StaticCopy
            };

            //Populate buffers in parallel

            BoneMatricesBuffer.Set(0, Matrix4x4.Identity);
            BoneInvBindMatricesBuffer.Set(0, Matrix4x4.Identity);

            for (int i = 0; i < _bones.Length; i++)
            {
                var (tfm, invBindWorldMtx) = Mesh!.UtilizedBones[i];
                uint boneIndex = (uint)i + 1u;

                var rb = new RenderBone(tfm, invBindWorldMtx, boneIndex);
                rb.TransformUpdated += BoneTransformUpdated;
                _bones[i] = rb;

                BoneMatricesBuffer.Set(boneIndex, tfm.WorldMatrix);
                BoneInvBindMatricesBuffer.Set(boneIndex, invBindWorldMtx);
            }

            Buffers.Add(BoneMatricesBuffer.BindingName, BoneMatricesBuffer);
            Buffers.Add(BoneInvBindMatricesBuffer.BindingName, BoneInvBindMatricesBuffer);
        }

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

                //This doesn't work, and I don't know why
                //var elemSize = BoneMatricesBuffer.ElementSize;
                //BoneMatricesBuffer.PushSubData((int)(bone.Key * elemSize), elemSize);
            }
            BoneMatricesBuffer.PushSubData(0, BoneMatricesBuffer.Length);
        }

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