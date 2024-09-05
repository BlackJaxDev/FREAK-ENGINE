using System.ComponentModel;

namespace XREngine.Rendering.Models.Materials
{
    public class ShaderUInt(uint defaultValue, string name, IShaderVarOwner? owner) : ShaderVar(name, owner),
        IUniformable,
        IShaderUnsignedIntType,
        IShaderUnsignedType,
        IShaderNonDecimalType, 
        IShaderNumericType,
        IShaderNonVectorType
    {
        [Browsable(false)]
        public override EShaderVarType TypeName => EShaderVarType._uint;
        [Category(CategoryName)]
        public uint Value { get => defaultValue; set { defaultValue = value; OnValueChanged(); } }
        protected override void SetProgramUniform(XRRenderProgram program, string location)
            => program.Uniform(location, defaultValue);
        [Browsable(false)]
        public unsafe uint* Data { get { fixed (uint* ptr = &defaultValue) return ptr; } }
        internal override string GetShaderValueString() => defaultValue.ToString();
        [Browsable(false)]
        public override object GenericValue => Value;

        public ShaderUInt() : this(0u, NoName) { }
        public ShaderUInt(uint defaultValue, string name) 
            : this(defaultValue, name, null) { }
    }
}
