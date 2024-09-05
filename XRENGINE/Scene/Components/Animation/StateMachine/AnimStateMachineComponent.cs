﻿//using Extensions;
//using System.Collections.Concurrent;

//namespace XREngine.Components
//{
//    public class AnimStateMachineComponent : XRComponent
//    {
//        internal int InitialStateIndex { get; set; } = -1;

//        public Skeleton? Skeleton { get; set; }

//        public EventList<AnimState> States
//        {
//            get => _states;
//            set => SetField(ref _states, value, UnlinkStates, LinkStates);
//        }

//        internal ConcurrentDictionary<string, SkeletalAnimation> AnimationTable { get; set; }

//        public AnimState InitialState
//        {
//            get => States.IndexInRange(InitialStateIndex) ? States[InitialStateIndex] : null;
//            set
//            {
//                bool wasNull = !States.IndexInRange(InitialStateIndex);
//                int index = States.IndexOf(value);
//                if (index >= 0)
//                    InitialStateIndex = index;
//                else if (value != null)
//                {
//                    InitialStateIndex = States.Count;
//                    States.Add(value);
//                }
//                else
//                    InitialStateIndex = -1;

//                if (wasNull && IsSpawned && States.IndexInRange(InitialStateIndex))
//                {
//                    _blendManager = new BlendManager(InitialState);
//                    RegisterTick(ETickGroup.PrePhysics, (int)ETickOrder.Animation, Tick);
//                }
//            }
//        }

//        private EventList<AnimState> _states;
//        private BlendManager _blendManager;

//        public AnimStateMachineComponent()
//        {
//            InitialStateIndex = -1;
//            States = [];
//            Skeleton = null;
//        }
//        public AnimStateMachineComponent(Skeleton skeleton)
//        {
//            InitialStateIndex = -1;
//            States = [];
//            Skeleton = skeleton;
//        }
//        public override void Constructing()
//        {
//            if (!States.IndexInRange(InitialStateIndex))
//                return;

//            _blendManager = new BlendManager(InitialState);
//            //RegisterTick(ETickGroup.PrePhysics, ETickOrder.Animation, Tick);
//        }
//        protected override void OnDestroying()
//        {
//            if (!States.IndexInRange(InitialStateIndex))
//                return;

//            //UnregisterTick(ETickGroup.PrePhysics, ETickOrder.Animation, Tick);
//            _blendManager = null;
//        }

//        protected internal void Tick(float delta)
//            => _blendManager?.Tick(delta, States, Skeleton?.File);

//        private void LinkStates(EventList<AnimState> states)
//        {
//            foreach (AnimState state in states)
//                StateAdded(state);

//            states.PostAnythingAdded += StateAdded;
//            states.PostAnythingRemoved += StateRemoved;
//        }
//        private void UnlinkStates(EventList<AnimState> states)
//        {
//            states.PostAnythingAdded -= StateAdded;
//            states.PostAnythingRemoved -= StateRemoved;

//            foreach (AnimState state in states)
//                StateRemoved(state);
//        }
//        private void StateRemoved(AnimState state)
//        {
//            if (state?.Owner == this)
//                state.Owner = null;
//        }
//        private void StateAdded(AnimState state)
//        {
//            if (state != null)
//                state.Owner = this;
//        }
//    }
//}
