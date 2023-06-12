namespace XREngine.Data.State_Machine
{
    public interface IAnimationTransition
    {
        public IAnimationState TargetState { get; set; }
        public List<IAnimationCondition> Conditions { get; set; }

        void AddCondition(IAnimationCondition condition);
        bool CheckConditions();
    }
}
