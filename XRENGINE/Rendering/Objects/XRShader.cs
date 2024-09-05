
namespace XREngine.Rendering
{
    public class XRShader : GenericRenderObject
    {
        private EShaderType _type = EShaderType.Fragment;
        public EShaderType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        private string _source = string.Empty;
        public string Source
        {
            get => _source;
            set => SetField(ref _source, value);
        }

        public XRShader() { }
        public XRShader(EShaderType type) => Type = type;
        public XRShader(EShaderType type, string source)
        {
            Type = type;
            Source = source;
        }

        public static EShaderType ResolveType(string extension)
        {
            extension = extension.ToLowerInvariant();

            if (extension.StartsWith('.'))
                extension = extension[1..];

            return extension switch
            {
                "vs" or "vert" => EShaderType.Vertex,
                "gs" or "geom" => EShaderType.Geometry,
                "tcs" or "tesc" => EShaderType.TessControl,
                "tes" or "tese" => EShaderType.TessEvaluation,
                "cs" or "comp" => EShaderType.Compute,
                _ => EShaderType.Fragment,
            };
        }

        /// <summary>
        /// Loads a shader from common engine shaders.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static XRShader EngineShader(string localPath, EShaderType type)
        {
            throw new NotImplementedException();
        }
    }
}
