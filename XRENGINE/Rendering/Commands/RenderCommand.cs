using XREngine.Data.Core;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.Commands
{
    public abstract class RenderCommand : XRBase, IComparable<RenderCommand>, IComparable
    {
        public RenderCommand() { }
        public RenderCommand(int renderPass) => RenderPass = renderPass;

        private int _renderPass = (int)EDefaultRenderPass.OpaqueForward;
        /// <summary>
        /// Used by the engine for proper order of rendering.
        /// </summary>
        public int RenderPass
        {
            get => _renderPass;
            set => SetField(ref _renderPass, value);
        }

        public abstract int CompareTo(RenderCommand? other);
        public int CompareTo(object? obj) => CompareTo(obj as RenderCommand);

        public abstract void Render(bool shadowPass);

        public virtual void PreRender(XRCamera? camera)
        {

        }
    }
}
