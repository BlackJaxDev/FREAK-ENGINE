using Silk.NET.Windowing;

public unsafe partial class OpenXRAPI : BaseAPI
{
    public override void UpdateWindowOptions(WindowOptions options)
    {

    }
    public override void SetWindow(IWindow window)
    {
        base.SetWindow(window);
    }
    public override void Init()
    {

    }
    public override void CleanUp()
    {

    }

    internal void DrawFrame(double delta)
    {
        throw new NotImplementedException();
    }
}