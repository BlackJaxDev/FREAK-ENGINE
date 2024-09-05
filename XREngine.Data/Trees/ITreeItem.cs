namespace XREngine.Data.Trees
{
    public interface ITreeItem
    {
        bool ShouldRender { get; }
        //void AddRenderCommands(RenderCommandCollection passes, XRCamera camera);
    }
}
