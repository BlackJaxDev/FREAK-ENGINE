namespace XREngine.Data.State_Machine
{
    public class AnimationTransition : IAnimationTransition
    {
        public IAnimationState TargetState { get; set; }
        public List<IAnimationCondition> Conditions { get; set; }

        public AnimationTransition(IAnimationState targetState)
        {
            TargetState = targetState;
            Conditions = new List<IAnimationCondition>();
        }

        public void AddCondition(IAnimationCondition condition)
        {
            Conditions.Add(condition);
        }

        public bool CheckConditions()
        {
            return Conditions.All(condition => condition.Evaluate());
        }
    }
}
