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

        protected List<IPreRendered> _preRenderList = [];
        protected List<IPreRendered> _preRenderAddWaitList = [];
        protected List<IPreRendered> _preRenderRemoveWaitList = [];
        private readonly List<RenderInfo> _renderables = [];

        /// <summary>
        /// Populates the given RenderPasses object with all renderables 
        /// having culling volumes that reside within the collectionVolume.
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="collectionVolume"></param>
        /// <param name="camera"></param>
        public void PreRenderUpdate(RenderCommandCollection commands, IVolume? collectionVolume, XRCamera camera)
        {
            CollectRenderedItems(commands, collectionVolume, camera);

            //TODO: prerender on own consistent animation thread
            //ParallelLoopResult result = await Task.Run(() => Parallel.ForEach(_preRenderList, p => { p.PreRenderUpdate(camera); }));
            foreach (IPreRendered p in _preRenderList)
                if (p.PreRenderEnabled)
                    p.PreRenderUpdate(camera);
        }

        /// <summary>
        /// Collects render commands for all renderables in the scene that intersect with the given volume.
        /// If the volume is null, all renderables are collected.
        /// Typically, the collectionVolume is the camera's frustum.
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="collectionVolume"></param>
        /// <param name="camera"></param>
        public void CollectRenderedItems(RenderCommandCollection commands, IVolume? collectionVolume, XRCamera camera)
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

        //public void CollectVisible(RenderCommandCollection passes, IVolume collectionVolume, XRCamera camera)
        //    => Tree.CollectVisible(collectionVolume, false, x => x.AddRenderCommands(passes, camera));

        public void PreRenderSwap()
        {
            foreach (IPreRendered p in _preRenderRemoveWaitList)
                _preRenderList.Remove(p);
            foreach (IPreRendered p in _preRenderAddWaitList)
                _preRenderList.Add(p);

            _preRenderRemoveWaitList.Clear();
            _preRenderAddWaitList.Clear();

            //foreach (IPreRendered p in _preRenderList)
            //    if (p.PreRenderEnabled)
            //        p.PreRenderSwap();
        }
        public void PreRender(XRViewport? viewport, XRCamera camera)
        {
            foreach (IPreRendered p in _preRenderList)
                if (p.PreRenderEnabled)
                    p.PreRender(viewport, camera);
        }
        public void AddPreRenderedObject(IPreRendered obj)
        {
            if (obj is null)
                return;
            if (!_preRenderList.Contains(obj))
                _preRenderAddWaitList.Add(obj);
        }
        public void RemovePreRenderedObject(IPreRendered obj)
        {
            if (obj is null)
                return;
            if (_preRenderList.Contains(obj))
                _preRenderRemoveWaitList.Add(obj);
        }

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
    }
}