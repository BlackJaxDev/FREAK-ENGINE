using System.Collections;

namespace XREngine.Scenes
{
    public class Scene : IEnumerable<SceneNode>
    {
        private readonly List<SceneNode> rootNodes = new List<SceneNode>();
        public List<SceneNode> RootNodes => rootNodes;

        public IEnumerator<SceneNode> GetEnumerator()
            => ((IEnumerable<SceneNode>)rootNodes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)rootNodes).GetEnumerator();
    }
}
