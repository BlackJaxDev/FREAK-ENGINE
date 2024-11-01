using XREngine.Rendering.Commands;
using XREngine.Rendering.Pipelines.Commands;

namespace XREngine.Rendering;

public class CustomRenderPipeline(
    ViewportRenderCommandContainer commands,
    Lazy<XRMaterial> invalidMaterialFactory,
    Dictionary<int, IComparer<RenderCommand>?> renderPasses) : RenderPipeline
{
    protected override Lazy<XRMaterial> InvalidMaterialFactory
        => invalidMaterialFactory;

    protected override ViewportRenderCommandContainer GenerateCommandChain()
        => commands;
    protected override Dictionary<int, IComparer<RenderCommand>?> GetPassIndicesAndSorters()
        => renderPasses;
}
