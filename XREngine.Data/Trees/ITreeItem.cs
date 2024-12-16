namespace XREngine.Data.Trees
{
    public interface ITreeItem
    {
        bool ShouldRender { get; }
        IRenderableBase Owner { get; }
    }
}
