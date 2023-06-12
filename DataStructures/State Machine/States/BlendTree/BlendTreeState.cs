namespace XREngine.Data.State_Machine
{
    public class BlendTreeState : AnimationState
    {
        public List<AnimationState> Children { get; }
        public List<float> Weights { get; }

        public BlendTreeState(string name) : base(name)
        {
            Children = new List<AnimationState>();
            Weights = new List<float>();
        }

        public void AddChild(AnimationState child, float weight)
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
