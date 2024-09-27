using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Pipelines.Commands;
using static XREngine.Engine.Rendering.State;

namespace XREngine.Rendering;

public class TestRenderPipeline : RenderPipeline
{
    protected override Lazy<XRMaterial> InvalidMaterialFactory => new(MakeInvalidMaterial, LazyThreadSafetyMode.PublicationOnly);

    private XRMaterial MakeInvalidMaterial()
    {
        Debug.Out("Generating invalid material");
        return XRMaterial.CreateUnlitColorMaterialForward();
    }

    protected override Dictionary<int, IComparer<RenderCommand>?> GetPassIndicesAndSorters()
        => new() { { (int)EDefaultRenderPass.OpaqueForward, null }, };

    protected override ViewportRenderCommandContainer GenerateCommandChain()
    {
        ViewportRenderCommandContainer c = [];
        using (c.AddUsing<VPRC_PushViewportRenderArea>(t => t.UseInternalResolution = false))
        {
            c.Add<VPRC_Manual>().ManualAction = () =>
            {
                EnableDepthTest(true);
                DepthFunc(EComparison.Lequal);
                Clear(new ColorF4(0.0f, 0.0f, 1.0f, 1.0f));
                Clear(true, true, true);
            };
            c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.OpaqueForward;
        }
        return c;
    }

    const string UserInterfaceFBOName = "UserInterfaceFBO";
    public override string GetUserInterfaceFBOName()
        => UserInterfaceFBOName;
}
