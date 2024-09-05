using XREngine.Data.Core;

namespace XREngine.Rendering.Models.Materials
{
    [Serializable]
    public class StencilTestFace : XRBase
    {
        public EStencilOp BothFailOp { get; set; } = EStencilOp.Keep;
        public EStencilOp StencilPassDepthFailOp { get; set; } = EStencilOp.Keep;
        public EStencilOp BothPassOp { get; set; } = EStencilOp.Keep;
        public EComparison Func { get; set; }
        public int Ref { get; set; }
        public uint ReadMask { get; set; }
        public uint WriteMask { get; set; }

        public override string ToString()
            => $"{Func} Ref:{Ref} Read Mask:{ReadMask} Write Mask:{WriteMask}";
    }
}
