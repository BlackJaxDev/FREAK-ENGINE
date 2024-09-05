using System.ComponentModel;

namespace XREngine.Rendering.Models.Materials
{
    public class ShaderFloat(float defaultValue, string name, IShaderVarOwner? owner) : ShaderVar(name, owner),
        IUniformable,
        IShaderFloatType,
        IShaderNonVectorType,
        IShaderNumericType,
        IShaderDecimalType,
        IShaderSignedType
    {
        [Browsable(false)]
        public override EShaderVarType TypeName => EShaderVarType._float;
        [Category(CategoryName)]
        public float Value { get => defaultValue; set { defaultValue = value; OnValueChanged(); } }
        protected override void SetProgramUniform(XRRenderProgram program, string location)
            => program.Uniform(location, defaultValue);
        [Browsable(false)]
        public unsafe float* Data { get { fixed (float* ptr = &defaultValue) return ptr; } }
        internal override string GetShaderValueString() => defaultValue.ToString("0.0######") + "f";
        [Browsable(false)]
        public override object GenericValue => Value;

        public ShaderFloat() : this(0.0f, NoName) { }
        public ShaderFloat(float defaultValue, string name) 
            : this(defaultValue, name, null) { }
    }
}
