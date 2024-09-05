namespace XREngine.Data.Animation
{
    public class AnimatorState(string name) : IAnimationState
    {
        public string Name { get; set; } = name;
        public List<AnimatorTransition> Transitions { get; } = [];

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
