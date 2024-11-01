using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Pipelines.Commands;

namespace XREngine.Components.Lights
{
    public class ShadowRenderPipeline : RenderPipeline
    {
        protected override Lazy<XRMaterial> InvalidMaterialFactory => new(MakeInvalidMaterial, LazyThreadSafetyMode.PublicationOnly);
        private XRMaterial MakeInvalidMaterial()
        {
            Debug.Out("Generating invalid material");
            return XRMaterial.CreateColorMaterialDeferred();
        }

        protected override ViewportRenderCommandContainer GenerateCommandChain()
        {
            return DefaultRenderPipeline.CreateFBOTargetCommands();
        }
        protected override Dictionary<int, IComparer<RenderCommand>?> GetPassIndicesAndSorters()
        {
            return new()
            {
                { -1, null }, //Prerender
                //{ 0, _nearToFarSorter }, //No background pass
                { 1, null }, //OpaqueDeferredLit
                //{ 2, _nearToFarSorter }, //No decals
                { 3, null }, //OpaqueForward
                { 4, null }, //TransparentForward
                //{ 5, _nearToFarSorter }, //No on top (UI)
            };
        }
    }
}
