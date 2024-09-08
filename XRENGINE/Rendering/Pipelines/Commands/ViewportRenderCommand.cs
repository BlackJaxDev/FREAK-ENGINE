namespace XREngine.Rendering.Pipelines.Commands
{
    public abstract class ViewportRenderCommand(XRRenderPipeline pipeline)
    {
        public const string SceneShaderPath = "Scene3D";

        public XRRenderPipeline Pipeline { get; } = pipeline;

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

        /// <summary>
        /// Destroys the FBOs for the command.
        /// </summary>
        public virtual void DestroyFBOs() { }
    }
}
