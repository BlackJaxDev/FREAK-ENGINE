﻿using Extensions;
using XREngine.Data.Core;

namespace XREngine.Rendering.Commands
{
    public class FarToNearRenderCommandSorter : IComparer<RenderCommand>
    {
        int IComparer<RenderCommand>.Compare(RenderCommand? x, RenderCommand? y)
            => -(x?.CompareTo(y) ?? 0);
    }
    public class NearToFarRenderCommandSorter : IComparer<RenderCommand>
    {
        int IComparer<RenderCommand>.Compare(RenderCommand? x, RenderCommand? y)
            => x?.CompareTo(y) ?? 0;
    }

    /// <summary>
    /// This class is used to manage the rendering of objects in the scene.
    /// RenderCommands are collected and placed in sorted passes that are rendered in order.
    /// At the end of the render and update loop, the buffers are swapped for consumption and the update list is cleared for the next frame.
    /// </summary>
    public sealed class RenderCommandCollection : XRBase
    {
        public bool IsShadowPass { get; set; }

        public void SetRenderPasses(Dictionary<int, IComparer<RenderCommand>?> passIndicesAndSorters)
        {
            _updatingPasses = passIndicesAndSorters.ToDictionary(x => x.Key, x => x.Value is null ? [] : (ICollection<RenderCommand>)new SortedSet<RenderCommand>(x.Value));

            //Copy the updating passes setup to rendering passes
            _renderingPasses = [];
            foreach (var pass in _updatingPasses)
                _renderingPasses.Add(pass.Key, []);
        }

        private int _numCommandsRecentlyAddedToUpdate = 0;

        private Dictionary<int, ICollection<RenderCommand>> _updatingPasses = [];
        private Dictionary<int, ICollection<RenderCommand>> _renderingPasses = [];

        public RenderCommandCollection() { }
        public RenderCommandCollection(Dictionary<int, IComparer<RenderCommand>?> passIndicesAndSorters)
            => SetRenderPasses(passIndicesAndSorters);

        public void Add(RenderCommand item)
        {
            int pass = item.RenderPass;
            if (!_updatingPasses.TryGetValue(pass, out var set))
            {
                Debug.Out($"No render pass {pass} found.");
                return;
            }
            set.Add(item);
            ++_numCommandsRecentlyAddedToUpdate;
        }
        internal int GetCommandsAddedCount()
        {
            int added = _numCommandsRecentlyAddedToUpdate;
            _numCommandsRecentlyAddedToUpdate = 0;
            return added;
        }
        internal void Render(int pass)
        {
            if (!_renderingPasses.TryGetValue(pass, out var list))
            {
                //Debug.Out($"No render pass {pass} found.");
                return;
            }

            list.ForEach(x => x.Render(IsShadowPass));
            list.Clear();
        }
        internal void ClearRendering(int pass)
        {
            if (!_renderingPasses.TryGetValue(pass, out var list))
            {
                Debug.Out($"No render pass {pass} found.");
                return;
            }

            list.Clear();
        }
        internal void ClearUpdating(int pass)
        {
            if (!_updatingPasses.TryGetValue(pass, out var list))
            {
                Debug.Out($"No render pass {pass} found.");
                return;
            }

            list.Clear();
            _numCommandsRecentlyAddedToUpdate = 0;
        }
        public void SwapBuffers()
        {
            _renderingPasses.ForEach(x => x.Value.Clear());
            (_updatingPasses, _renderingPasses) = (_renderingPasses, _updatingPasses);
        }

        public void AddRange(IEnumerable<RenderCommand> renderCommands)
        {
            foreach (RenderCommand renderCommand in renderCommands)
                Add(renderCommand);
        }
    }
}
