namespace XREngine.Rendering.Pipelines.Commands
{
    public abstract class ViewportStateRenderCommandBase : ViewportRenderCommand
    {
        public ViewportStateRenderCommandBase(ViewportRenderCommandContainer pipeline) : base(pipeline)
        {

        }

        public abstract StateObject GetUsingState();
    }
    public abstract class ViewportStateRenderCommand<T>(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommandBase(pipeline) where T : ViewportPopStateRenderCommand
    {
        public T PopCommand { get; } = (T)Activator.CreateInstance(typeof(T), pipeline);
        /// <summary>
        /// For use with the using statement. At the end of the using statement, a command will be added to the pipeline to pop the state.
        /// </summary>
        /// <returns></returns>
        public override StateObject GetUsingState() => new(() => CommandContainer.Add(PopCommand));
    }
}
