using XREngine.Rendering.Commands;
using XREngine.Rendering.Pipelines.Commands;

namespace XREngine.Rendering;

public class CustomRenderPipeline(
    ViewportRenderCommandContainer commands,
    Lazy<XRMaterial> invalidMaterialFactory,
    Dictionary<int, IComparer<RenderCommand>?> renderPasses,
    string userInterfaceFBOName = "UserInterfaceFBO") : RenderPipeline
{
    protected override Lazy<XRMaterial> InvalidMaterialFactory
        => invalidMaterialFactory;

    public override string GetUserInterfaceFBOName()
        => userInterfaceFBOName;
    protected override ViewportRenderCommandContainer GenerateCommandChain()
        => commands;
    protected override Dictionary<int, IComparer<RenderCommand>?> GetPassIndicesAndSorters()
        => renderPasses;
}
