using System.ComponentModel;
using XREngine.Data.Vectors;

namespace XREngine.Rendering.Models.Materials
{
    public class ShaderIVector3 : ShaderVar
    {
        [Browsable(false)]
        public override EShaderVarType TypeName => EShaderVarType._ivec3;
        [Category(CategoryName)]
        public IVector3 Value { get => _value; set { _value = value; OnValueChanged(); } }
        protected override void SetProgramUniform(XRRenderProgram program, string location)
            => program.Uniform(location, _value);
        internal override string GetShaderValueString()
        => string.Format("iVector3({0}, {1}, {2})",
            _value.X.ToString(),
            _value.Y.ToString(),
            _value.Z.ToString());
        [Browsable(false)]
        public override object GenericValue => Value;

        private IVector3 _value;

        public ShaderIVector3() : this(new IVector3(), NoName) { }
        public ShaderIVector3(IVector3 defaultValue, string name)
            : this(defaultValue, name, null) { }
        public ShaderIVector3(IVector3 defaultValue, string name, IShaderVarOwner? owner)
            : base(name, owner)
        {
            _value = defaultValue;
            _fields.Add(".x", new ShaderDouble(defaultValue.X, "X", this));
            _fields.Add(".y", new ShaderDouble(defaultValue.Y, "Y", this));
            _fields.Add(".z", new ShaderDouble(defaultValue.Z, "Z", this));
        }
    }
}
