using Silk.NET.OpenXR;

public unsafe partial class OpenXRAPI
{
    public OpenXRAPI()
    {
        Api = XR.GetApi();
        Initialize();
    }
    ~OpenXRAPI()
    {
        CleanUp();
        Api.Dispose();
    }

    public XR Api { get; private set; }

    protected void Initialize()
    {
        CreateInstance();
        SetupDebugMessenger();
    }
    protected void CleanUp()
    {
        DestroyInstance();
    }
}