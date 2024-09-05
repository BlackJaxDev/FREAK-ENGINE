using System.ComponentModel;

namespace XREngine.Rendering.Models.Materials
{
    public class ShaderBool : ShaderVar, 
        IUniformable,
        IShaderBooleanType,
        IShaderNonVectorType
    {
        [Browsable(false)]
        public override EShaderVarType TypeName => EShaderVarType._bool;
        [Category(CategoryName)]
        public bool Value { get => _value; set { _value = value; OnValueChanged(); } }
        protected override void SetProgramUniform(XRRenderProgram program, string location)
            => program.Uniform(location, _value);
        [Browsable(false)]
        public unsafe bool* Data { get { fixed (bool* ptr = &_value) return ptr; } }
        internal override string GetShaderValueString() => _value.ToString().ToLowerInvariant();
        [Browsable(false)]
        public override object GenericValue => Value;

        private bool _value;

        public ShaderBool() : this(false, NoName) { }
        public ShaderBool(string name)
            : this(false, name, null) { }
        public ShaderBool(bool defaultValue, string name)
            : this(defaultValue, name, null) { }
        public ShaderBool(bool defaultValue, string name, IShaderVarOwner owner)
            : base(name, owner) => _value = defaultValue;
    }
}
