using System.ComponentModel;

namespace XREngine.Rendering.Models.Materials
{
    public class ShaderDouble : ShaderVar,
        IUniformable,
        IShaderDoubleType, 
        IShaderNonVectorType,
        IShaderNumericType,
        IShaderDecimalType, 
        IShaderSignedType
    {
        [Browsable(false)]
        public override EShaderVarType TypeName => EShaderVarType._double;
        [Category(CategoryName)]
        public double Value { get => _value; set { _value = value; OnValueChanged(); } }
        protected override void SetProgramUniform(XRRenderProgram program, string location)
            => program.Uniform(location, _value);
        [Browsable(false)]
        public unsafe double* Data { get { fixed (double* ptr = &_value) return ptr; } }
        internal override string GetShaderValueString() => _value.ToString("0.0######");
        [Browsable(false)]
        public override object GenericValue => Value;

        private double _value;

        public ShaderDouble() : this(0.0, NoName) { }
        public ShaderDouble(double defaultValue, string name)
            : this(defaultValue, name, null) { }
        public ShaderDouble(double defaultValue, string name, IShaderVarOwner owner)
            : base(name, owner) => _value = defaultValue;
    }
}
