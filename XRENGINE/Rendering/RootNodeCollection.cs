﻿using System.Collections;
using XREngine.Components;
using XREngine.Scene;

namespace XREngine.Rendering
{
    public partial class XRWorldInstance
    {
        public class RootNodeCollection : IReadOnlyList<SceneNode>
        {
            public Action<XRComponent>? ComponentCacheAction { get; set; }
            public Action<XRComponent>? ComponentUncacheAction { get; set; }
            public Action<SceneNode>? NodeCacheAction { get; set; }
            public Action<SceneNode>? NodeUncacheAction { get; set; }

            private readonly List<SceneNode> _rootNodes = [];

            public SceneNode this[int index] => _rootNodes[index];

            public int Count => _rootNodes.Count;

            public void Remove(SceneNode node)
            {
                if (node is null)
                    return;

                _rootNodes.Remove(node);
                UncacheComponents(node);
            }

            public void Add(SceneNode node)
            {
                if (node is null)
                    return;

                _rootNodes.Add(node);
                CacheComponents(node);
            }

            private void CacheComponents(SceneNode node)
                => node.IterateHierarchy(c =>
                {
                    NodeCacheAction?.Invoke(c);
                    foreach (var comp in c.Components)
                        ComponentCacheAction?.Invoke(comp);
                });

            private void UncacheComponents(SceneNode node)
                => node.IterateHierarchy(c =>
                {
                    NodeUncacheAction?.Invoke(c);
                    foreach (var comp in c.Components)
                        ComponentUncacheAction?.Invoke(comp);
                });

            public IEnumerator<SceneNode> GetEnumerator()
                => _rootNodes.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator()
                => ((IEnumerable)_rootNodes).GetEnumerator();
        }

    }
}
