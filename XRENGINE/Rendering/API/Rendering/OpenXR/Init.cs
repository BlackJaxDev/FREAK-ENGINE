using Silk.NET.OpenXR;
using XREngine.Data.Geometry;
using XREngine.Rendering;

public unsafe partial class OpenXRAPI : AbstractRenderer<XR>
{
    protected override XR GetAPI()
        => XR.GetApi();

    protected override void Initialize()
    {
        CreateInstance();
        SetupDebugMessenger();
    }
    protected override void CleanUp()
    {
        DestroyInstance();
    }

    protected override void WindowRenderCallback(double delta)
    {

    }

    public override void CropRenderArea(BoundingRectangle region)
    {

    }

    protected override void SetRenderArea(BoundingRectangle region)
    {

    }

    protected override AbstractRenderAPIObject CreateAPIRenderObject(GenericRenderObject renderObject)
    {
        throw new NotImplementedException();
    }
}