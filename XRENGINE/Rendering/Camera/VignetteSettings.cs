
using System.Numerics;
using XREngine.Data;
using XREngine.Data.Colors;
using XREngine.Data.Core;

namespace XREngine.Rendering
{
    public class VignetteSettings : XRBase
    {
        public const string VignetteUniformName = "Vignette";

        public ColorF3 Color { get; set; } = new ColorF3();
        public float Intensity { get; set; } = 0.0f;
        public float Power { get; set; } = 0.0f;

        public void SetUniforms(XRRenderProgram program)
        {
            program.Uniform($"{VignetteUniformName}.{nameof(Color)}", Color);
            program.Uniform($"{VignetteUniformName}.{nameof(Intensity)}", Intensity);
            program.Uniform($"{VignetteUniformName}.{nameof(Power)}", Power);
        }
    }
}