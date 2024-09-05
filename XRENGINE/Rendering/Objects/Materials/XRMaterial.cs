using System.ComponentModel;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Rendering.Models.Materials;
using Color = System.Drawing.Color;

namespace XREngine.Rendering
{
    public class XRMaterial : XRMaterialBase
    {
        /// <summary>
        /// Static global invalid material, loaded from <ExeDir>/Shaders/Common/Invalid.fs
        /// </summary>
        public static XRMaterial InvalidMaterial { get; }
            = new XRMaterial(ShaderHelper.LoadShader("Common/Invalid.fs", EShaderType.Fragment));

        //CreateUnlitColorMaterialForward(Color.Magenta);

        [Browsable(false)]
        public IReadOnlyList<XRShader> FragmentShaders => _fragmentShaders;
        [Browsable(false)]
        public IReadOnlyList<XRShader> GeometryShaders => _geometryShaders;
        [Browsable(false)]
        public IReadOnlyList<XRShader> TessEvalShaders => _tessEvalShaders;
        [Browsable(false)]
        public IReadOnlyList<XRShader> TessCtrlShaders => _tessCtrlShaders;
        [Browsable(false)]
        public IReadOnlyList<XRShader> VertexShaders => _vertexShaders;

        private readonly List<XRShader> _fragmentShaders = [];
        private readonly List<XRShader> _geometryShaders = [];
        private readonly List<XRShader> _tessEvalShaders = [];
        private readonly List<XRShader> _tessCtrlShaders = [];
        private readonly List<XRShader> _vertexShaders = [];
        private EventList<XRShader> _shaders;

        public XRMaterial()
        {
            _shaders = [];
            _shaders.PostModified += ShadersChanged;
            ShadersChanged();
        }
        public XRMaterial(params XRShader[] shaders)
        {
            _shaders = new EventList<XRShader>(shaders);
            _shaders.PostModified += ShadersChanged;
            ShadersChanged();
        }
        public XRMaterial(ShaderVar[] parameters, params XRShader[] shaders) : base(parameters)
        {
            _shaders = new EventList<XRShader>(shaders);
            _shaders.PostModified += ShadersChanged;
            ShadersChanged();
        }
        public XRMaterial(XRTexture[] textures, params XRShader[] shaders) : base(textures)
        {
            _shaders = new EventList<XRShader>(shaders);
            _shaders.PostModified += ShadersChanged;
            ShadersChanged();
        }
        public XRMaterial(ShaderVar[] parameters, XRTexture[] textures, params XRShader[] shaders) : base(parameters, textures)
        {
            _shaders = new EventList<XRShader>(shaders);
            _shaders.PostModified += ShadersChanged;
            ShadersChanged();
        }
        public XRMaterial(XRTexture[] textures, ShaderVar[] parameters, params XRShader[] shaders) : base(parameters, textures)
        {
            _shaders = new EventList<XRShader>(shaders);
            _shaders.PostModified += ShadersChanged;
            ShadersChanged();
        }

        public EventList<XRShader> Shaders
        {
            get => _shaders;
            set
            {
                _shaders.PostModified -= ShadersChanged;
                _shaders = value ?? [];
                _shaders.PostModified += ShadersChanged;
            }
        }

        //[TPostDeserialize]
        internal void ShadersChanged()
        {
            _fragmentShaders.Clear();
            _geometryShaders.Clear();
            _tessCtrlShaders.Clear();
            _tessEvalShaders.Clear();
            _vertexShaders.Clear();

            _shaderPipelineProgram?.Destroy();
            _shaderPipelineProgram = null;
            
            foreach (var shader in Shaders)
                if (shader != null)
                    switch (shader.Type)
                    {
                        case EShaderType.Vertex:
                            _vertexShaders.Add(shader);
                            break;
                        case EShaderType.Fragment:
                            _fragmentShaders.Add(shader);
                            break;
                        case EShaderType.Geometry:
                            _geometryShaders.Add(shader);
                            break;
                        case EShaderType.TessControl:
                            _tessCtrlShaders.Add(shader);
                            break;
                        case EShaderType.TessEvaluation:
                            _tessEvalShaders.Add(shader);
                            break;
                    }
            
            if (Engine.Rendering.Settings.AllowShaderPipelines)
                _shaderPipelineProgram = new XRRenderProgram(Shaders);
        }

        public static XRMaterial CreateUnlitAlphaTextureMaterialForward(XRTexture2D texture)
            => new(new[] { texture }, ShaderHelper.UnlitAlphaTextureFragForward());

        public static XRMaterial CreateUnlitTextureMaterialForward(XRTexture2D texture)
            => new(new[] { texture }, ShaderHelper.UnlitTextureFragForward());

        public static XRMaterial CreateUnlitTextureMaterialForward()
            => new(ShaderHelper.UnlitTextureFragForward());

        public static XRMaterial CreateLitTextureMaterial(bool deferred = true)
            => new(deferred ? ShaderHelper.TextureFragDeferred() : ShaderHelper.LitTextureFragForward());

        public static XRMaterial CreateLitTextureMaterial(XRTexture2D texture, bool deferred = true)
            => new([texture], deferred ? ShaderHelper.TextureFragDeferred() : ShaderHelper.LitTextureFragForward());

        public static XRMaterial CreateUnlitColorMaterialForward()
            => CreateUnlitColorMaterialForward(Color.DarkTurquoise);

        public static XRMaterial CreateUnlitColorMaterialForward(ColorF4 color)
            => new([new ShaderVector4(color, "MatColor")], ShaderHelper.UnlitColorFragForward());

        public static XRMaterial CreateLitColorMaterial(bool deferred = true)
            => CreateLitColorMaterial(Color.DarkTurquoise, deferred);

        public static XRMaterial CreateLitColorMaterial(ColorF4 color, bool deferred = true)
        {
            ShaderVar[] parameters;
            XRShader frag;
            if (deferred)
            {
                frag = ShaderHelper.LitColorFragDeferred();
                parameters =
                [
                    new ShaderVector3((ColorF3)color, "BaseColor"),
                    new ShaderFloat(color.A, "Opacity"),
                    new ShaderFloat(1.0f, "Specular"),
                    new ShaderFloat(1.0f, "Roughness"),
                    new ShaderFloat(0.0f, "Metallic"),
                    new ShaderFloat(1.0f, "IndexOfRefraction"),
                ];
            }
            else
            {
                frag = ShaderHelper.LitColorFragForward();
                parameters =
                [
                    new ShaderVector4(color, "MatColor"),
                    new ShaderFloat(20.0f, "MatSpecularIntensity"),
                    // ShaderFloat(128.0f, "MatShininess"),
                ];
            }

            return new(parameters, frag);
        }
        public enum EOpaque
        {
            /// <summary>
            ///  (the default): Takes the transparency information from the color’s
            ///  alpha channel, where the value 1.0 is opaque.
            /// </summary>
            A_ONE,
            /// <summary>
            /// Takes the transparency information from the color’s red, green,
            /// and blue channels, where the value 0.0 is opaque, with each channel 
            /// modulated independently.
            /// </summary>
            RGB_ZERO,
            /// <summary>
            /// Takes the transparency information from the color’s
            /// alpha channel, where the value 0.0 is opaque.
            /// </summary>
            A_ZERO,
            /// <summary>
            ///  Takes the transparency information from the color’s red, green,
            ///  and blue channels, where the value 1.0 is opaque, with each channel 
            ///  modulated independently.
            /// </summary>
            RGB_ONE,
        }
        /// <summary>
        /// Creates a Blinn lighting model material for a forward renderer.
        /// </summary>
        public static XRMaterial CreateBlinnMaterial_Forward(
            Vector3? emission,
            Vector3? ambient,
            Vector3? diffuse,
            Vector3? specular,
            float shininess,
            float transparency,
            Vector3 transparent,
            EOpaque transparencyMode,
            float reflectivity,
            Vector3 reflective,
            float indexOfRefraction)
        {
            // color = emission + ambient * al + diffuse * max(N * L, 0) + specular * max(H * N, 0) ^ shininess
            // where:
            // • al – A constant amount of ambient light contribution coming from the scene.In the COMMON
            // profile, this is the sum of all the <light><technique_common><ambient> values in the <visual_scene>.
            // • N – Normal vector (normalized)
            // • L – Light vector (normalized)
            // • I – Eye vector (normalized)
            // • H – Half-angle vector, calculated as halfway between the unit Eye and Light vectors, using the equation H = normalize(I + L)

            int count = 0;
            if (emission.HasValue) ++count;
            if (ambient.HasValue) ++count;
            if (diffuse.HasValue) ++count;
            if (specular.HasValue) ++count;
            ShaderVar[] parameters = new ShaderVar[count + 1];
            count = 0;

            string source = "#version 450\n";

            if (emission.HasValue)
            {
                source += "uniform Vector3 Emission;\n";
                parameters[count++] = new ShaderVector3(emission.Value, "Emission");
            }
            else
                source += "uniform sampler2D Emission;\n";

            if (ambient.HasValue)
            {
                source += "uniform Vector3 Ambient;\n";
                parameters[count++] = new ShaderVector3(ambient.Value, "Ambient");
            }
            else
                source += "uniform sampler2D Ambient;\n";

            if (diffuse.HasValue)
            {
                source += "uniform Vector3 Diffuse;\n";
                parameters[count++] = new ShaderVector3(diffuse.Value, "Diffuse");
            }
            else
                source += "uniform sampler2D Diffuse;\n";

            if (specular.HasValue)
            {
                source += "uniform Vector3 Specular;\n";
                parameters[count++] = new ShaderVector3(specular.Value, "Specular");
            }
            else
                source += "uniform sampler2D Specular;\n";

            source += "uniform float Shininess;\n";
            parameters[count++] = new ShaderFloat(shininess, "Shininess");

            if (transparencyMode == EOpaque.RGB_ZERO ||
                transparencyMode == EOpaque.RGB_ONE)
                source += @"
float luminance(in Vector3 color)
{
    return (color.r * 0.212671) + (color.g * 0.715160) + (color.b * 0.072169);
}";

            switch (transparencyMode)
            {
                case EOpaque.A_ONE:
                    source += "\nresult = mix(fb, mat, transparent.a * transparency);";
                    break;
                case EOpaque.RGB_ZERO:
                    source += @"
result.rgb = fb.rgb * (transparent.rgb * transparency) + mat.rgb * (1.0f - transparent.rgb * transparency);
result.a = fb.a * (luminance(transparent.rgb) * transparency) + mat.a * (1.0f - luminance(transparent.rgb) * transparency);";
                    break;
                case EOpaque.A_ZERO:
                    source += "\nresult = mix(mat, fb, transparent.a * transparency);";
                    break;
                case EOpaque.RGB_ONE:
                    source += @"
result.rgb = fb.rgb * (1.0f - transparent.rgb * transparency) + mat.rgb * (transparent.rgb * transparency);
result.a = fb.a * (1.0f - luminance(transparent.rgb) * transparency) + mat.a * (luminance(transparent.rgb) * transparency);";
                    break;
            }


            //#version 450

            //layout (location = 0) out Vector4 OutColor;

            //uniform Vector4 MatColor;
            //uniform float MatSpecularIntensity;
            //uniform float MatShininess;

            //uniform Vector3 CameraPosition;
            //uniform Vector3 CameraForward;

            //in Vector3 FragPos;
            //in Vector3 FragNorm;

            //" + LightingSetupBasic() + @"

            //void main()
            //{
            //    Vector3 normal = normalize(FragNorm);

            //    " + LightingCalcForward() + @"

            //    OutColor = MatColor * Vector4(totalLight, 1.0);
            //}

            return new(parameters, new XRShader(EShaderType.Fragment, source));
        }
    }
}
