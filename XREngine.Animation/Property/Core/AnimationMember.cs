using System.Reflection;
using XREngine.Data.Core;

namespace XREngine.Animation
{
    public class AnimationMember : XRBase
    {
        public AnimationMember()
        {
            _fieldCache = null;
            _propertyCache = null;
            _methodCache = null;
        }
        /// <summary>
        /// Constructor to create a subtree without an animation at this level.
        /// </summary>
        /// <param name="memberName">The name of this property and optionally sub-properties separated by a period.</param>
        /// <param name="children">Any sub-properties this property owns and you want to animate.</param>
        public AnimationMember(string memberName, params AnimationMember[] children) : this()
        {
            int splitIndex = memberName.IndexOf('.');
            if (splitIndex >= 0)
            {
                string remainingPath = memberName.Substring(splitIndex + 1, memberName.Length - splitIndex - 1);
                _children.Add(new AnimationMember(remainingPath));
                memberName = memberName[..splitIndex];
            }
            if (children != null)
                _children.AddRange(children);
            _memberName = memberName;
            Animation = null;
            MemberType = EAnimationMemberType.Property;
        }
        /// <summary>
        /// Constructor to create a subtree with an animation attached at this level.
        /// </summary>
        /// <param name="memberName">The name of the field, property or method to animate.</param>
        /// <param name="memberType"></param>
        /// <param name="animation"></param>
        public AnimationMember(string memberName, EAnimationMemberType memberType, BasePropAnim animation) : this()
        {
            if (memberType != EAnimationMemberType.Property)
            {
                int splitIndex = memberName.IndexOf('.');
                if (splitIndex >= 0)
                {
                    string remainingPath = memberName.Substring(splitIndex + 1, memberName.Length - splitIndex - 1);
                    _children.Add(new AnimationMember(remainingPath));
                    memberName = memberName[..splitIndex];
                }
            }
            _memberName = memberName;
            Animation = animation;
            MemberType = memberType;
        }

        //Cached at runtime
        private PropertyInfo? _propertyCache;
        private MethodInfo? _methodCache;
        private FieldInfo? _fieldCache;
        internal Action<object?, float>? _tick = null;

        private BasePropAnim? _animation;
        public BasePropAnim? Animation
        {
            get => _animation;
            set => SetField(ref _animation, value);
        }

        private readonly EventList<AnimationMember> _children = [];
        public EventList<AnimationMember> Children => _children;


        private string _memberName = string.Empty;
        public string MemberName
        {
            get => _memberName;
            set => _memberName = value;
        }

        public object[] MethodArguments { get; } = new object[1];
        public int MethodValueArgumentIndex { get; set; } = 0;

        //TODO: resolve _memberType as a new object animated
        private EAnimationMemberType _memberType = EAnimationMemberType.Property;
        public EAnimationMemberType MemberType
        {
            get => _memberType;
            set => SetField(ref _memberType, value);
        }

        private bool _memberNotFound = false;
        public bool MemberNotFound
        {
            get => _memberNotFound;
            private set => SetField(ref _memberNotFound, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(MemberType):
                    MemberNotFound = false;
                    switch (_memberType)
                    {
                        case EAnimationMemberType.Field:
                            _tick = FieldTick;
                            break;
                        case EAnimationMemberType.Property:
                            _tick = PropertyTick;
                            break;
                        case EAnimationMemberType.Method:
                            _tick = MethodTick;
                            break;
                    }
                    break;
            }
        }

        public void CollectAnimations(string? path, Dictionary<string, BasePropAnim> animations)
        {
            if (!string.IsNullOrEmpty(path))
                path += $".{_memberName}";
            else
                path = _memberName ?? string.Empty;

            if (Animation != null)
                animations.Add(path, Animation);

            foreach (AnimationMember member in _children)
                member.CollectAnimations(path, animations);
        }

        //TODO: determine if member is field, property or method once the object is applied
        //No two members can share the same name
        internal void MethodTick(object? obj, float delta)
        {
            if (obj is null || MemberNotFound)
                return;

            if (_methodCache is null)
            {
                Type? type = obj.GetType();
                while (type != null)
                {
                    if ((_methodCache = type.GetMethod(_memberName)) is null)
                        type = type.BaseType;
                    else
                        break;
                }

                if (MemberNotFound = _methodCache is null)
                    return;
            }

            if (_methodCache is not null)
                Animation?.Tick(obj, _methodCache, delta, MethodValueArgumentIndex, MethodArguments);
        }
        internal void PropertyTick(object? obj, float delta)
        {
            if (obj is null || MemberNotFound)
                return;

            if (_propertyCache is null)
            {
                Type? type = obj.GetType();
                while (type != null)
                {
                    if ((_propertyCache = type.GetProperty(_memberName)) is null)
                        type = type.BaseType;
                    else
                        break;
                }

                if (MemberNotFound = _propertyCache is null)
                    return;
            }

            if (_propertyCache is not null)
            {
                if (Animation is not null)
                    Animation.Tick(obj, _propertyCache, delta);
                else
                {
                    object? value = _propertyCache.GetValue(obj);
                    foreach (AnimationMember f in _children)
                        f._tick?.Invoke(value, delta);
                }
            }
        }
        internal void FieldTick(object? obj, float delta)
        {
            if (obj is null || MemberNotFound)
                return;

            if (_fieldCache is null)
            {
                Type? type = obj.GetType();
                while (type != null)
                {
                    if ((_fieldCache = type.GetField(_memberName)) is null)
                        type = type.BaseType;
                    else
                        break;
                }
                if (MemberNotFound = _fieldCache is null)
                    return;
            }

            if (_fieldCache is not null)
            {
                if (Animation is not null)
                    Animation.Tick(obj, _fieldCache, delta);
                else
                {
                    object? value = _fieldCache.GetValue(obj);
                    foreach (AnimationMember f in _children)
                        f._tick?.Invoke(value, delta);
                }
            }
        }
        /// <summary>
        /// Registers to the AnimationHasEnded method in the animation tree
        /// and returns the total amount of animations this member and its child members contain.
        /// </summary>
        /// <param name="tree">The animation tree that owns this member.</param>
        /// <returns>The total amount of animations this member and its child members contain.</returns>
        internal int Register(AnimationTree tree)
        {
            bool animExists = Animation != null;
            int count = animExists ? 1 : 0;

            //TODO: call Animation.File.AnimationEnded -= tree.AnimationHasEnded somewhere
            if (animExists)
                Animation!.AnimationEnded += tree.AnimationHasEnded;

            foreach (AnimationMember folder in _children)
                count += folder.Register(tree);

            return count;
        }
        /// <summary>
        /// Registers to the AnimationHasEnded method in the animation tree
        /// and returns the total amount of animations this member and its child members contain.
        /// </summary>
        /// <param name="tree">The animation tree that owns this member.</param>
        /// <returns>The total amount of animations this member and its child members contain.</returns>
        internal int Unregister(AnimationTree tree)
        {
            bool animExists = Animation != null;
            int count = animExists ? 1 : 0;

            if (animExists)
                Animation!.AnimationEnded -= tree.AnimationHasEnded;

            foreach (AnimationMember folder in _children)
                count += folder.Unregister(tree);

            return count;
        }
        internal void StartAnimations()
        {
            MemberNotFound = false;
            _fieldCache = null;
            _propertyCache = null;
            _methodCache = null;

            Animation?.Start();
            foreach (AnimationMember folder in _children)
                folder.StartAnimations();
        }
        internal void StopAnimations()
        {
            MemberNotFound = false;
            _fieldCache = null;
            _propertyCache = null;
            _methodCache = null;

            Animation?.Stop();
            foreach (AnimationMember folder in _children)
                folder.StopAnimations();
        }
    }
}
