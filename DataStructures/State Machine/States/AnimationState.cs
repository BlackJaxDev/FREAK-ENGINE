namespace XREngine.Data.State_Machine
{
    public class AnimationState : IAnimationState
    {
        public string Name { get; set; }
        public List<AnimationTransition> Transitions { get; }

        public AnimationState(string name)
        {
            Name = name;
            Transitions = new List<AnimationTransition>();
        }

        public virtual void Enter()
        {
            // Initialize state
        }

        public virtual void Update(float deltaTime)
        {
            // Update state
        }

        public virtual void Exit()
        {
            // Cleanup state
        }
    }
}
