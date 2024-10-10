using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Data.Trees;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Scene
{
    public delegate void DelRender(RenderCommandCollection renderingPasses, XRCamera camera, XRViewport viewport, XRFrameBuffer? target);
    public abstract class VisualScene : XRBase, IEnumerable<IRenderable>
    {
        public IReadOnlyList<RenderInfo> Renderables => _renderables;
        public abstract IRenderTree RenderablesTree { get; }

        private readonly List<RenderInfo> _renderables = [];

        /// <summary>
        /// Collects render commands for all renderables in the scene that intersect with the given volume.
        /// If the volume is null, all renderables are collected.
        /// Typically, the collectionVolume is the camera's frustum.
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="collectionVolume"></param>
        /// <param name="camera"></param>
        public virtual void CollectRenderedItems(RenderCommandCollection commands, IVolume? collectionVolume, XRCamera? camera)
        {
            void AddRenderCommands(ITreeItem item)
            {
                if (item is RenderInfo renderable)
                    renderable.AddRenderCommands(commands, camera);
            }

            switch (RenderablesTree)
            {
                case I3DRenderTree tree:
                    if (collectionVolume is null)
                        tree.CollectAll(AddRenderCommands);
                    else
                        tree.CollectIntersecting(collectionVolume, false, AddRenderCommands);
                    break;
                case I2DRenderTree tree:
                    tree.CollectAll(AddRenderCommands);
                    break;
            }
        }

        /// <summary>
        /// Swaps the update/render buffers for the scene.
        /// </summary>
        public void SwapBuffers()
            => RenderablesTree.Swap();

        public void AddRenderable(RenderInfo renderable)
        {
            _renderables.Add(renderable);
            RenderablesTree.Add(renderable);
        }

        public void RemoveRenderable(RenderInfo renderable)
        {
            _renderables.Remove(renderable);
            RenderablesTree.Remove(renderable);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => ((System.Collections.IEnumerable)_renderables).GetEnumerator();
        public IEnumerator<IRenderable> GetEnumerator()
            => ((IEnumerable<IRenderable>)_renderables).GetEnumerator();

        public virtual void GlobalCollectVisible()
        {

        }

        /// <summary>
        /// Occurs before rendering any viewports.
        /// </summary>
        public virtual void GlobalPreRender()
        {

        }

        /// <summary>
        /// Occurs after rendering all viewports.
        /// </summary>
        public virtual void GlobalPostRender()
        {

        }
    }
}