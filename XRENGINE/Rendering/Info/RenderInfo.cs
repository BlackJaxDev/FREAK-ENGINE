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

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(WorldInstance):
                    if (prev is XRWorldInstance prevInstance)
                        prevInstance.VisualScene?.RemoveRenderable(this);
                    if (field is XRWorldInstance newInstance)
                        newInstance.VisualScene?.AddRenderable(this);
                    break;
            }
        }

        public bool ShouldRender { get; set; } = true;

        public delegate void DelAddRenderCommandsCallback(RenderInfo info, RenderCommandCollection passes, XRCamera camera);
        public DelAddRenderCommandsCallback? PreAddRenderCommandsCallback { get; set; }

        public void AddRenderCommands(RenderCommandCollection passes, XRCamera camera)
        {
            PreAddRenderCommandsCallback?.Invoke(this, passes, camera);
            if (RenderCommands.Count == 0)
                Debug.LogWarning($"RenderInfo for {(Owner?.GetType()?.Name ?? "null")} has no render commands.");
            else
                passes.AddRange(RenderCommands);
        }
    }
}