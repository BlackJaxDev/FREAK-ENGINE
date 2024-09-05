using XREngine.Data.Core;

namespace XREngine.Rendering.Commands
{
    public abstract class RenderCommand : XRBase, IComparable<RenderCommand>, IComparable
    {
        public RenderCommand() { }
        public RenderCommand(int renderPass) => RenderPass = renderPass;

        /// <summary>
        /// Used by the engine for proper order of rendering.
        /// </summary>
        public int RenderPass { get; set; } = 0;//ERenderPass.OpaqueForward;

        public abstract int CompareTo(RenderCommand? other);
        public int CompareTo(object? obj) => CompareTo(obj as RenderCommand);

        public abstract void Render(bool shadowPass);
    }
}
