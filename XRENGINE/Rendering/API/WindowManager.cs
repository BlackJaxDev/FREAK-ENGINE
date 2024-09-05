namespace XREngine.Rendering
{
    ///// <summary>
    ///// Represents a global interface for connecting worlds to window render handlers
    ///// related to a particular instance of work.
    ///// For example, main editor world, model editor world, a 2D UI world, etc
    ///// </summary>
    //public class WindowManager : XRBase
    //{
    //    protected World? _targetWorld;
    //    public World? TargetWorld
    //    {
    //        get => _targetWorld;
    //        set
    //        {
    //            if (_targetWorld?.Manager == this)
    //                _targetWorld.Manager = null;

    //            _targetWorld = value;

    //            if (_targetWorld != null)
    //                _targetWorld.Manager = this;
    //        }
    //    }

    //    private readonly List<WindowContext> _contexts = [];
    //    public IReadOnlyList<WindowContext> AssociatedContexts => _contexts;

    //    private readonly ConcurrentQueue<WindowContext> _contextAddQueue = new();
    //    private readonly ConcurrentQueue<WindowContext> _contextRemoveQueue = new();

    //    /// <summary>
    //    /// Occurs during the synchronization period between render and pre-render. Adds and removes contexts.
    //    /// </summary>
    //    public virtual void SwapBuffers()
    //    {
    //        while (_contextRemoveQueue.TryDequeue(out WindowContext? ctx))
    //        {
    //            if (ctx is null)
    //                continue;

    //            _contexts.Remove(ctx);
    //            ctx.Removed(this);
    //        }
    //        while (_contextAddQueue.TryDequeue(out WindowContext? ctx))
    //        {
    //            if (ctx is null)
    //                continue;

    //            _contexts.Add(ctx);
    //            ctx.Added(this);
    //        }
    //    }

    //    public int ID { get; internal set; }

    //    public void GlobalCollectVisible(float delta)
    //        => TargetWorld?.GlobalCollectVisible(delta);
    //    public void GlobalPreRender(float delta)
    //        => TargetWorld?.GlobalPreRender(delta);
    //    public void GlobalSwap(float delta)
    //        => TargetWorld?.GlobalSwap(delta);

    //    public void AddContext(WindowContext ctx)
    //        => _contextAddQueue.Enqueue(ctx);
    //    public void RemoveContext(WindowContext ctx)
    //        => _contextRemoveQueue.Enqueue(ctx);
    //}
}
