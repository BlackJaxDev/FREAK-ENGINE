namespace XREngine.Data.Animation
{
    public class BlendTreeState(string name) : AnimatorState(name)
    {
        public List<AnimatorState> Children { get; } = [];
        public List<float> Weights { get; } = [];

        public void AddChild(AnimatorState child, float weight)
        {
            Children.Add(child);
            Weights.Add(weight);
        }

        public override void Update(float deltaTime)
        {
            for (int i = 0; i < Children.Count; i++)
                Children[i].Update(deltaTime * Weights[i]);
        }
    }
}
