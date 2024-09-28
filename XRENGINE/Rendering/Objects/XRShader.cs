
using XREngine.Core.Files;
using XREngine.Rendering.Models.Materials;

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

        private TextFile _source = string.Empty;
        public TextFile Source
        {
            get => _source;
            set => SetField(ref _source, value);
        }

        public XRShader() { }
        public XRShader(EShaderType type) => Type = type;
        public XRShader(EShaderType type, TextFile source)
        {
            Type = type;
            Source = source;
            Debug.Out($"Loaded shader of type {type} from {source.FilePath}{Environment.NewLine}{source.Text}");
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
        /// <param name="relativePath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static XRShader EngineShader(string relativePath, EShaderType type)
            => ShaderHelper.LoadShader(relativePath, type);

        public static Task<XRShader> EngineShaderAsync(string relativePath, EShaderType type)
            => ShaderHelper.LoadShaderAsync(relativePath, type);
    }
}
