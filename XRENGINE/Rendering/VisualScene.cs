﻿using System.Numerics;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
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
        public abstract void CollectRenderedItems(RenderCommandCollection meshRenderCommands, XRCamera? activeCamera, bool cullWithFrustum, Func<XRCamera>? cullingCameraOverride, bool shadowPass);

        public virtual void DebugRender(XRCamera? camera, bool onlyContainingItems = false)
        {

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

        /// <summary>
        /// Swaps the update/render buffers for the scene.
        /// </summary>
        public virtual void GlobalSwapBuffers()
            => RenderablesTree.Swap();

        public void Raycast(CameraComponent cameraComponent, Vector2 screenPoint, out SortedDictionary<float, ITreeItem> items, Func<ITreeItem, Segment, float?> directTest)
            => RenderablesTree.Raycast(cameraComponent.Camera.GetWorldSegment(screenPoint), out items, directTest);
    }
}