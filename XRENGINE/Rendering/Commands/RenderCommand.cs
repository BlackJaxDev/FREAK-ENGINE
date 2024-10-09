using XREngine.Data.Core;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.Commands
{
    public abstract class RenderCommand : XRBase, IComparable<RenderCommand>, IComparable
    {
        public RenderCommand() { }
        public RenderCommand(int renderPass) => RenderPass = renderPass;

        /// <summary>
        /// Used by the engine for proper order of rendering.
        /// </summary>
        public int RenderPass { get; set; } = (int)EDefaultRenderPass.OpaqueForward;

        public abstract int CompareTo(RenderCommand? other);
        public int CompareTo(object? obj) => CompareTo(obj as RenderCommand);

        public abstract void Render(bool shadowPass);
    }
}
