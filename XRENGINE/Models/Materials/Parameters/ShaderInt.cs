using System.ComponentModel;

namespace XREngine.Rendering.Models.Materials
{
    public class ShaderInt(int defaultValue, string name, IShaderVarOwner? owner) : ShaderVar(name, owner),
        IUniformable,
        IShaderSignedIntType, 
        IShaderNonVectorType,
        IShaderNumericType,
        IShaderNonDecimalType,
        IShaderSignedType
    {
        [Browsable(false)]
        public override EShaderVarType TypeName => EShaderVarType._int;
        [Category(CategoryName)]
        public int Value { get => defaultValue; set { defaultValue = value; OnValueChanged(); } }
        protected override void SetProgramUniform(XRRenderProgram program, string location)
            => program.Uniform(location, defaultValue);
        [Browsable(false)]
        public unsafe int* Data { get { fixed (int* ptr = &defaultValue) return ptr; } }
        internal override string GetShaderValueString() => defaultValue.ToString();
        [Browsable(false)]
        public override object GenericValue => Value;

        public ShaderInt() : this(0, NoName) { }
        public ShaderInt(int defaultValue, string name)
            : this(defaultValue, name, null) { }
    }
}
