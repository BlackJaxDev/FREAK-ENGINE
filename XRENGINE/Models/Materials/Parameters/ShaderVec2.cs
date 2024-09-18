using System.ComponentModel;
using System.Numerics;

namespace XREngine.Rendering.Models.Materials
{
    public class ShaderVector2 : ShaderVar
    {
        [Browsable(false)]
        public override EShaderVarType TypeName => EShaderVarType._vec2;
        [Category(CategoryName)]
        public Vector2 Value { get => _value; set { _value = value; OnValueChanged(); } }
        protected override void SetProgramUniform(XRRenderProgram program, string location)
            => program.Uniform(location, _value);
        internal override string GetShaderValueString()
             => $"Vector2({_value.X:0.0######}f, {_value.Y:0.0######}f)";
        [Browsable(false)]
        public override object GenericValue => Value;

        private Vector2 _value;

        public ShaderVector2() : this(new Vector2(), NoName) { }
        public ShaderVector2(Vector2 defaultValue, string name)
            : this(defaultValue, name, null) { }
        public ShaderVector2(Vector2 defaultValue, string name, IShaderVarOwner? owner)
            : base(name, owner)
        {
            _value = defaultValue;
            _fields.Add(".x", new ShaderFloat(defaultValue.X, "X", this));
            _fields.Add(".y", new ShaderFloat(defaultValue.Y, "Y", this));
        }
    }
}
