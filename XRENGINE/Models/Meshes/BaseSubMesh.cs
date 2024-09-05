using System.ComponentModel;
using XREngine.Core.Files;
using XREngine.Data.Rendering;
using XREngine.Rendering.Info;

namespace XREngine.Rendering.Models
{
    public abstract class BaseSubMesh : XRAsset, IRenderable
    {
        public RenderInfo3D? RenderInfo { get; set; }
        
        [DisplayName("Levels Of Detail")]
        [Browsable(false)]
        public EventList<LOD> LODs { get; set; } = [];
        public RenderInfo[] RenderedObjects { get; }

        public BaseSubMesh()
        {
            RenderInfo = new(this) { CullingVolume = null, CastsShadows = true, ReceivesShadows = true };
            RenderedObjects = [RenderInfo];
        }

        public BaseSubMesh(
            IVolume cullingVolume,
            XRMesh primitives,
            XRMaterial material)
        {
            RenderInfo = new(this) { CullingVolume = cullingVolume, CastsShadows = true, ReceivesShadows = true };
            RenderedObjects = [RenderInfo];
            LODs = [new(material, primitives, 0.0f)];
        }

        public BaseSubMesh(
            IVolume cullingVolume,
            IEnumerable<LOD> lods)
        {
            RenderInfo = new(this) { CullingVolume = cullingVolume, CastsShadows = true, ReceivesShadows = true };
            RenderedObjects = [RenderInfo];
            LODs = new EventList<LOD>(lods ?? []);
        }

        public BaseSubMesh(
            IVolume cullingVolume,
            params LOD[] lods)
        {
            RenderInfo = new(this) { CullingVolume = cullingVolume, CastsShadows = true, ReceivesShadows = true };
            RenderedObjects = [RenderInfo];
            LODs = new EventList<LOD>(lods);
        }
    }
}
