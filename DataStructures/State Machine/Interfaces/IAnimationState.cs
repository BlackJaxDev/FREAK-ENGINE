namespace XREngine.Data.State_Machine
{
    public interface IAnimationState
    {
        string Name { get; set; }
        List<AnimationTransition> Transitions { get; }

        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
