using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Models.Materials.Functions;

namespace XREngine.Core.Tools
{
    public class GLSLManager
    {
        public class Variable
        {
            public int LayoutLocation { get; set; }
            public EShaderVarType Type { get; set; }
            public string Name { get; set; }
        }
        public class Method
        {

        }

        public EGLSLVersion Version { get; set; }
        public List<Variable> Uniforms { get; set; }
        public List<Variable> In { get; set; }
        public List<Variable> Out { get; set; }

        public GLSLManager()
        {

        }
        public void Parse(string text)
        {
            Uniforms = new List<Variable>();
            In = new List<Variable>();
            Out = new List<Variable>();
        }
    }
}
