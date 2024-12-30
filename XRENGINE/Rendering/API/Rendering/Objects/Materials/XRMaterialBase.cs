using Extensions;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Models.Materials.Shaders.Parameters;

namespace XREngine.Rendering
{
    public abstract class XRMaterialBase : GenericRenderObject
    {
        private int _renderPass = (int)EDefaultRenderPass.OpaqueForward;
        /// <summary>
        /// This is the render pass bucket that any meshes using this material will be put in.
        /// Render passes are used to separate different types of rendering, such as opaque, transparent, etc.
        /// The number of passes and what each pass does is determined by the camera's render pipeline object!
        /// Use EDefaultRenderPass for this value and DefaultRenderPipeline as the render pipeline to use the default rendering setup.
        /// </summary>
        public int RenderPass
        {
            get => _renderPass;
            set => SetField(ref _renderPass, value);
        }

        public event Action<XRMaterialBase, XRRenderProgram>? SettingUniforms;
        public void OnSettingUniforms(XRRenderProgram program)
            => SettingUniforms?.Invoke(this, program);

        public XRMaterialBase() { }
        protected XRMaterialBase(ShaderVar[] parameters)
        {
            Parameters = [.. parameters]; //Make copy
        }
        protected XRMaterialBase(XRTexture?[] textures)
        {
            Textures = new EventList<XRTexture?>(textures);
        }
        protected XRMaterialBase(ShaderVar[] parameters, XRTexture?[] textures)
        {
            Parameters = [.. parameters]; //Make copy
            Textures = new EventList<XRTexture?>(textures);
        }

        private XRRenderProgram? _shaderPipelineProgram;
        /// <summary>
        /// This is the program that represents this material.
        /// Will only be set if the renderer is using shader pipelines, so it can be combined later.
        /// May contain all kinds of shaders, including vertex, fragment, geometry, compute, etc.
        /// If contains a vertex shader, the default generated vertex shader will not be used.
        /// </summary>
        public XRRenderProgram? ShaderPipelineProgram
        {
            get => _shaderPipelineProgram;
            protected set => SetField(ref _shaderPipelineProgram, value);
        }

        private RenderingParameters _renderOptions = new();
        /// <summary>
        /// These are special rendering options that the API can use to set its state separately from the shaders.
        /// </summary>
        public RenderingParameters RenderOptions
        {
            get => _renderOptions ??= new();
            set => _renderOptions = value ?? new();
        }

        protected ShaderVar[] _parameters = [];
        /// <summary>
        /// These are the uniforms that each shader in the program has requested.
        /// </summary>
        public ShaderVar[] Parameters
        {
            get => _parameters ??= [];
            set => SetField(ref _parameters, value ?? []);
        }

        protected EventList<XRTexture?> _textures = [];
        /// <summary>
        /// These are the texture samplers that each shader in the program has requested.
        /// </summary>
        public EventList<XRTexture?> Textures
        {
            get => _textures;
            set => SetField(ref _textures, value ?? []);
        }

        /// <summary>
        /// Retrieves the material's uniform parameter at the given index.
        /// Use this to set uniform values to be passed to the fragment shader.
        /// </summary>
        public T2? Parameter<T2>(int index) where T2 : ShaderVar
            => Parameters.IndexInRangeArrayT(index) ? Parameters[index] as T2 : null;

        /// <summary>
        /// Retrieves the material's uniform parameter with the given name.
        /// Use this to set uniform values to be passed to the fragment shader.
        /// </summary>
        public T2? Parameter<T2>(string name) where T2 : ShaderVar
        {
            if (_nameIndexCache.TryGetValue(name, out var index))
                return Parameter<T2>(index);
            for (var i = 0; i < Parameters.Length; i++)
            {
                if (Parameters[i].Name == name)
                {
                    _nameIndexCache[name] = i;
                    return Parameter<T2>(i);
                }
            }
            return null;
        }

        private readonly Dictionary<string, int> _nameIndexCache = [];

        public void ResetNameIndexCache()
            => _nameIndexCache.Clear();

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Parameters):
                    ResetNameIndexCache();
                    break;
            }
        }

        public void SetFloat(string name, float value)
        {
            var param = Parameter<ShaderFloat>(name);
            if (param is not null)
                param.Value = value;
        }
        public void SetFloat(int index, float value)
        {
            var param = Parameter<ShaderFloat>(index);
            if (param is not null)
                param.Value = value;
        }
        public void SetInt(string name, int value)
        {
            var param = Parameter<ShaderInt>(name);
            if (param is not null)
                param.Value = value;
        }
        public void SetInt(int index, int value)
        {
            var param = Parameter<ShaderInt>(index);
            if (param is not null)
                param.Value = value;
        }
        public void SetVector2(string name, Vector2 value)
        {
            var param = Parameter<ShaderVector2>(name);
            if (param is not null)
                param.Value = value;
        }
        public void SetVector2(int index, Vector2 value)
        {
            var param = Parameter<ShaderVector2>(index);
            if (param is not null)
                param.Value = value;
        }
        public void SetVector3(string name, Vector3 value)
        {
            var param = Parameter<ShaderVector3>(name);
            if (param is not null)
                param.Value = value;
        }
        public void SetVector3(int index, Vector3 value)
        {
            var param = Parameter<ShaderVector3>(index);
            if (param is not null)
                param.Value = value;
        }
        public void SetVector4(string name, Vector4 value)
        {
            var param = Parameter<ShaderVector4>(name);
            if (param is not null)
                param.Value = value;
        }
        public void SetVector4(int index, Vector4 value)
        {
            var param = Parameter<ShaderVector4>(index);
            if (param is not null)
                param.Value = value;
        }
        public void SetMatrix4(string name, Matrix4x4 value)
        {
            var param = Parameter<ShaderMat4>(name);
            if (param is not null)
                param.Value = value;
        }
        public void SetMatrix4(int index, Matrix4x4 value)
        {
            var param = Parameter<ShaderMat4>(index);
            if (param is not null)
                param.Value = value;
        }

        public Dictionary<string, XRFrameBuffer> GrabPasses { get; } = [];

        //public void AddGrabPass(string name, uint w, uint h)
        //{
        //    XRTexture2D t = XRTexture2D.CreateFrameBufferTexture(w, h, EPixelInternalFormat.Rgba8, EPixelFormat.Rgba, EPixelType.UnsignedByte);
        //    //t.SizedInternalFormat = ESizedInternalFormat.Rgba8;
        //    //t.Resizable = true;
        //    t.MinFilter = ETexMinFilter.Linear;
        //    t.MagFilter = ETexMagFilter.Linear;
        //    t.UWrap = ETexWrapMode.ClampToEdge;
        //    t.VWrap = ETexWrapMode.ClampToEdge;
        //    t.Name = name;
        //    var fbo = new XRFrameBuffer((t, EFrameBufferAttachment.ColorAttachment0, 0, -1));
        //    GrabPasses[name] = fbo;
        //}
        //public void DestroyGrabPass(string name)
        //{
        //    if (!GrabPasses.TryGetValue(name, out var fbo))
        //        return;
            
        //    fbo.Destroy();
        //    GrabPasses.Remove(name);
        //}
        //public void DestroyAllGrabPasses()
        //{
        //    foreach (var fbo in GrabPasses.Values)
        //        fbo.Destroy();
        //    GrabPasses.Clear();
        //}

        //public XRTexture2D? GrabPass(
        //    XRFrameBuffer inFBO,
        //    string name,
        //    EReadBufferMode readBuffer = EReadBufferMode.ColorAttachment0,
        //    bool colorBit = true,
        //    bool depthBit = false,
        //    bool stencilBit = false,
        //    bool linearFilter = true,
        //    bool resizeToFit = true)
        //{
        //    var rend = AbstractRenderer.Current;
        //    if (rend is null || inFBO is null || !GrabPasses.TryGetValue(name, out var outFBO))
        //        return null;
        //    if (resizeToFit && (inFBO.Width != outFBO.Width || inFBO.Height != outFBO.Height))
        //        outFBO.Resize(inFBO.Width, inFBO.Height);
        //    rend.BlitFBO(inFBO, outFBO, readBuffer, colorBit, depthBit, stencilBit, linearFilter);
        //    return outFBO.Targets?[0].Target as XRTexture2D;
        //}

        //public XRFrameBuffer? GetGrabPassResult(string name)
        //    => GrabPasses.TryGetValue(name, out var fbo) ? fbo : null;
    }
}
