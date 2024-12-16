using XREngine.Components;
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
    /// The culling volume is used to determine if the object is visible to the camera.
    /// If the culling volume is null, the object is always rendered.
    /// When this render info is visible, all render commands are added to the rendering passes.
    /// </summary>
    public abstract class RenderInfo : XRBase, ITreeItem
    {
        public abstract ITreeNode? TreeNode { get; }
        public IRenderable? Owner { get; }

        public override string ToString()
            => $"{Owner?.ToString() ?? "Unknown"}";

        public delegate void DelPreRenderCallback(RenderInfo info, RenderCommand command, XRCamera? camera, bool shadowPass);
        public event DelPreRenderCallback? PreRenderCallback;

        public delegate void DelSwapBuffersCallback(RenderInfo info, RenderCommand command, bool shadowPass);
        public event DelSwapBuffersCallback? SwapBuffersCallback;

        protected RenderInfo(IRenderable owner, params RenderCommand[] renderCommands)
        {
            Owner = owner;
            RenderCommands.PostAnythingAdded += Added;
            RenderCommands.PostAnythingRemoved += Removed;
            RenderCommands.AddRange(renderCommands);
        }

        private void Removed(RenderCommand item)
        {
            item.OnPreRender -= PreRender;
            item.OnSwapBuffers -= SwapBuffers;
        }

        private void Added(RenderCommand item)
        {
            item.OnPreRender += PreRender;
            item.OnSwapBuffers += SwapBuffers;
        }

        private void SwapBuffers(RenderCommand command, bool swapBuffers)
            => SwapBuffersCallback?.Invoke(this, command, swapBuffers);

        private void PreRender(RenderCommand command, XRCamera? camera, bool shadowPass)
            => PreRenderCallback?.Invoke(this, command, camera, shadowPass);

        private EventList<RenderCommand> _renderCommands = [];
        public EventList<RenderCommand> RenderCommands
        {
            get => _renderCommands;
            set => SetField(ref _renderCommands, value);
        }

        private bool _isVisible = true;
        /// <summary>
        /// IsVisible determines if the object exists in the visual scene tree at all.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetField(ref _isVisible, value);
        }

        /// <summary>
        /// ShouldRender determines if the object is rendered. If false, the object still exists in the visual scene tree, but is not rendered.
        /// </summary>
        public virtual bool ShouldRender => true;

        private XRWorldInstance? _worldInstance;
        /// <summary>
        /// This is the world instance that this render info is part of. It is set automatically when the render info is added to a visual scene.
        /// </summary>
        public XRWorldInstance? WorldInstance
        {
            get => _worldInstance;
            internal set => SetField(ref _worldInstance, value);
        }

        private UICanvasComponent? _userInterfaceCanvas;
        public UICanvasComponent? UserInterfaceCanvas
        {
            get => _userInterfaceCanvas;
            set => SetField(ref _userInterfaceCanvas, value);
        }

        public delegate bool DelAddRenderCommandsCallback(RenderInfo info, RenderCommandCollection passes, XRCamera? camera);

        /// <summary>
        /// This callback is called before render commands are added to the render pass.
        /// Return false to skip adding render commands.
        /// </summary>
        public DelAddRenderCommandsCallback? PreAddRenderCommandsCallback { get; set; }
        IRenderableBase ITreeItem.Owner => Owner;

        public void AddRenderCommands(RenderCommandCollection passes, XRCamera? camera, bool shadowPass)
        {
            if (!(PreAddRenderCommandsCallback?.Invoke(this, passes, camera) ?? true))
                return;

            for (int i = 0; i < RenderCommands.Count; i++)
            {
                RenderCommand cmd = RenderCommands[i];
                if (!cmd.Enabled)
                    continue;

                cmd.PreRender(camera, shadowPass);
                passes.Add(cmd);
            }
        }
    }
}