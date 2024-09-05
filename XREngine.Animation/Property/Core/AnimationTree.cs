using System.ComponentModel;
using XREngine.Data.Core;

namespace XREngine.Animation
{
    public class AnimationTree : BaseAnimation
    {
        public EAnimTreeTraversalMethod TraversalMethod { get; set; } = EAnimTreeTraversalMethod.Parallel;

        public AnimationTree()
            : base(0.0f, false) { }
        public AnimationTree(AnimationMember rootFolder)
            : this() => RootMember = rootFolder;

        public AnimationTree(string animationName, string memberPath, BasePropAnim anim) : this()
        {
            //Name = animationName;

            string[] memberPathParts = memberPath.Split('.');
            AnimationMember? last = null;

            foreach (string childMemberName in memberPathParts)
            {
                AnimationMember member = new AnimationMember(childMemberName);

                if (last is null)
                    RootMember = member;
                else
                    last.Children.Add(member);

                last = member;
            }

            LengthInSeconds = anim.LengthInSeconds;
            Looped = anim.Looped;
            if (last != null)
                last.Animation = anim;
        }

        private int _totalAnimCount = 0;
        private AnimationMember _root;

        internal List<XRObjectBase> Owners { get; } = [];

        private int _endedAnimations = 0;
        private bool _removeOnEnd;
        private bool _beginOnSpawn;

        [Category(AnimCategory)]
        public bool RemoveOnEnd
        {
            get => _removeOnEnd;
            set => SetField(ref _removeOnEnd, value);
        }
        [Category(AnimCategory)]
        public bool BeginOnSpawn 
        {
            get => _beginOnSpawn;
            set => SetField(ref _beginOnSpawn, value);
        }

        public AnimationMember RootMember
        {
            get => _root;
            set
            {
                _root?.Unregister(this);
                _root = value;
                _totalAnimCount = _root?.Register(this) ?? 0;
            }
        }

        private void OwnersModified()
        {
            //if (Owners.Count == 0 && IsTicking)
            //    UnregisterTick(_group, _order, OnProgressed, _pausedBehavior);
            //else if (_state == EAnimationState.Playing && Owners.Count != 0 && !IsTicking)
            //    RegisterTick(_group, _order, OnProgressed, _pausedBehavior);
        }
        internal void AnimationHasEnded(BaseAnimation obj)
        {
            if (++_endedAnimations >= _totalAnimCount)
                Stop();
        }
        public Dictionary<string, BasePropAnim> GetAllAnimations()
        {
            Dictionary<string, BasePropAnim> anims = new Dictionary<string, BasePropAnim>();
            _root.CollectAnimations(null, anims);
            return anims;
        }
        protected override void PostStarted()
        {
            _root?.StartAnimations();
        }
        protected override void PostStopped()
        {
            if (_endedAnimations < _totalAnimCount)
                _root?.StopAnimations();
        }
        protected override void OnProgressed(float delta)
        {
            foreach (XRObjectBase obj in Owners)
                _root?._tick(obj, delta);
        }
    }
}
