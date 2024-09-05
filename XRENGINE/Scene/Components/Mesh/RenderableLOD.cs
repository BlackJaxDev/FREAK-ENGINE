using XREngine.Data.Core;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Data.Components
{
    public class RenderableLOD : XRBase
    {
        public XRMeshRenderer? Manager { get; set; }
        public float VisibleDistance { get; set; } = 0.0f;
        public ShaderVar[]? Parameters => Manager?.Material?.Parameters;
    }
}
