using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Data.Trees;
using XREngine.Rendering.Commands;

namespace XREngine.Rendering.Info
{
    public delegate float DelGetSortOrder(bool shadowPass);
    public delegate void DelCullingVolumeChanged(IVolume oldVolume, IVolume newVolume);
    /// <summary>
    /// Render info defines how a renderable object should be rendered and contains state information for the object.
    /// </summary>
    public abstract class RenderInfo : XRBase, ITreeItem
    {
        public abstract ITreeNode? TreeNode { get; }

        protected RenderInfo(IRenderable owner, params RenderCommand[] renderCommands)
        {
            Owner = owner;
            RenderCommands.AddRange(renderCommands);
        }

        private EventList<RenderCommand> _renderCommands = [];
        public EventList<RenderCommand> RenderCommands
        {
            get => _renderCommands;
            set => SetField(ref _renderCommands, value);
        }

        public bool IsVisible
        {
            get => TreeNode != null;
            set
            {
                var tree = WorldInstance?.VisualScene?.RenderablesTree;
                if (tree is null)
                    return;

                if (value)
                    tree.Add(this);
                else
                    tree.Remove(this);
            }
        }

        public IRenderable? Owner { get; }

        private XRWorldInstance? _worldInstance;
        public XRWorldInstance? WorldInstance
        {
            get => _worldInstance;
            set => SetField(ref _worldInstance, value);
        }

        public bool ShouldRender { get; set; } = true;

        public void AddRenderCommands(RenderCommandCollection passes, XRCamera camera)
        {

        }
    }
}