using Silk.NET.Windowing;

public abstract class BaseAPI
{
    protected IWindow? window;
    protected bool _frameBufferInvalidated = false;

    public void FrameBufferResized()
        => _frameBufferInvalidated = true;

    public abstract void UpdateWindowOptions(WindowOptions options);
    public virtual void SetWindow(IWindow window) => this.window = window;
    public abstract void Init();
    public abstract void CleanUp();
}
