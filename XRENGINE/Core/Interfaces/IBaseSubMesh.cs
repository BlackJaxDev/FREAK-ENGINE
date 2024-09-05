using XREngine.Data.Rendering;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;

namespace XREngine
{
    public interface IBaseSubMesh
    {
        EventList<LOD> LODs { get; }
        ERenderPass RenderPass { get; set; }
        RenderInfo3D RenderInfo { get; set; }
    }
}
