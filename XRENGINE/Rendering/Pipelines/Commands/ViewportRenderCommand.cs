namespace XREngine.Rendering.Pipelines.Commands
{
    public abstract class ViewportRenderCommand(ViewportRenderCommandContainer commandContainer)
    {
        public const string SceneShaderPath = "Scene3D";

        public ViewportRenderCommandContainer CommandContainer { get; } = commandContainer;
        public XRRenderPipeline Pipeline => CommandContainer.Pipeline;

        /// <summary>
        /// If true, the command will execute in the shadow pass.
        /// Otherwise, it will only execute in the main pass.
        /// </summary>
        public bool ExecuteInShadowPass { get; set; } = false;

        /// <summary>
        /// If the command should execute.
        /// Can be used to skip commands while executing.
        /// Will be reset to true if execution is skipped.
        /// </summary>
        public bool ShouldExecute { get; set; } = true;

        /// <summary>
        /// Executes the command.
        /// </summary>
        protected abstract void Execute();

        public void ExecuteIfShould()
        {
            if (ShouldExecute)
                Execute();
            ShouldExecute = true;
        }
    }
}
