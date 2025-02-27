using XREngine.Data.Core;

namespace XREngine.Rendering.Models.Materials
{
    [Serializable]
    public class StencilTestFace : XRBase
    {
        public EStencilOp BothFailOp { get; set; } = EStencilOp.Keep;
        public EStencilOp StencilPassDepthFailOp { get; set; } = EStencilOp.Keep;
        public EStencilOp BothPassOp { get; set; } = EStencilOp.Keep;
        public EComparison Function { get; set; }
        public int Reference { get; set; }
        public uint ReadMask { get; set; }
        public uint WriteMask { get; set; }

        public override string ToString()
            => $"{Function} Ref:{Reference} Read Mask:{ReadMask} Write Mask:{WriteMask}";
    }
}
