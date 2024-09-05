namespace XREngine.Data.Animation
{
    public interface IAnimationState
    {
        string Name { get; set; }
        List<AnimatorTransition> Transitions { get; }

        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
