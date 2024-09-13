using Extensions;
using XREngine.Data.Core;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering
{
    public abstract class XRMaterialBase : GenericRenderObject
    {
        private int _renderPass = 0;
        public int RenderPass
        {
            get => _renderPass;
            set => SetField(ref _renderPass, value);
        }

        public XREvent<XRMaterialBase> SettingUniforms;

        public XRMaterialBase() { }
        protected XRMaterialBase(ShaderVar[] parameters)
        {
            Parameters = [.. parameters]; //Make copy
        }
        protected XRMaterialBase(XRTexture[] textures)
        {
            Textures = new EventList<XRTexture>(textures);
        }
        protected XRMaterialBase(ShaderVar[] parameters, XRTexture[] textures)
        {
            Parameters = [.. parameters]; //Make copy
            Textures = new EventList<XRTexture>(textures);
        }

        protected XRRenderProgram? _shaderPipelineProgram;
        /// <summary>
        /// This is the program that represents this material.
        /// Will only be set if the renderer is using shader pipelines, so it can be combined later.
        /// May contain all kinds of shaders, including vertex, fragment, geometry, compute, etc.
        /// If contains a vertex shader, the default generated vertex shader will not be used.
        /// </summary>
        public XRRenderProgram? ShaderPipelineProgram => _shaderPipelineProgram;

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

        protected EventList<XRTexture> _textures = [];
        /// <summary>
        /// These are the texture samplers that each shader in the program has requested.
        /// </summary>
        public EventList<XRTexture> Textures
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
            => Parameters.FirstOrDefault(x => x.Name == name) as T2;
    }
}
