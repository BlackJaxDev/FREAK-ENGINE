using MagicPhysX;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Components.Scene;
using XREngine.Input.Devices;
using XREngine.Rendering.Physics.Physx;
using XREngine.Rendering.Physics.Physx.Joints;
using XREngine.Scene.Transforms;

namespace XREngine.Components
{
    /// <summary>
    /// Add-on for the CharacterPawnComponent that adds VR-specific input handling.
    /// Normal keyboard, mouse and gamepad input is available both on desktop and in VR to allow for special VR control setups.
    /// </summary>
    [RequireComponents(typeof(CharacterPawnComponent))]
    public class VRCharacterInputSet : OptionalInputSetComponent
    {
        public CharacterPawnComponent CharacterPawn => GetSiblingComponent<CharacterPawnComponent>(true)!;

        private VRControllerTransform? _rightHandTransform;
        public VRControllerTransform? RightHandTransform
        {
            get => _rightHandTransform;
            set => SetField(ref _rightHandTransform, value);
        }

        private VRControllerTransform? _leftHandTransform;
        public VRControllerTransform? LeftHandTransform
        {
            get => _leftHandTransform;
            set => SetField(ref _leftHandTransform, value);
        }

        private float _grabRadius = 0.1f;
        public float GrabRadius
        {
            get => _grabRadius;
            set => SetField(ref _grabRadius, value);
        }

        private float _grabForceThreshold = 0.2f;
        public float GrabForceThreshold
        {
            get => _grabForceThreshold;
            set => SetField(ref _grabForceThreshold, value);
        }

        private float _releaseForceThreshold = 0.1f;
        public float ReleaseForceThreshold
        {
            get => _releaseForceThreshold;
            set => SetField(ref _releaseForceThreshold, value);
        }

        private PhysxDynamicRigidBody? _leftHandOverlap = null;
        public PhysxDynamicRigidBody? LeftHandOverlap
        {
            get => _leftHandOverlap;
            set => SetField(ref _leftHandOverlap, value);
        }

        private PhysxDynamicRigidBody? _rightHandOverlap = null;
        public PhysxDynamicRigidBody? RightHandOverlap
        {
            get => _rightHandOverlap;
            set => SetField(ref _rightHandOverlap, value);
        }

        private PhysxDynamicRigidBody? _rightHandBody = null;
        public PhysxDynamicRigidBody? RightHandRigidBody
        {
            get => _rightHandBody;
            set => SetField(ref _rightHandBody, value);
        }

        private PhysxDynamicRigidBody? _leftHandBody = null;
        public PhysxDynamicRigidBody? LeftHandRigidBody
        {
            get => _leftHandBody;
            set => SetField(ref _leftHandBody, value);
        }

        private PhysxJoint_Distance? _leftHandConstraint;
        public PhysxJoint_Distance? LeftHandConstraint
        {
            get => _leftHandConstraint;
            set => SetField(ref _leftHandConstraint, value);
        }

        private PhysxJoint_Distance? _rightHandConstraint;
        public PhysxJoint_Distance? RightHandConstraint
        {
            get => _rightHandConstraint;
            set => SetField(ref _rightHandConstraint, value);
        }

        public enum EVRActionCategory
        {
            /// <summary>
            /// Global actions are always available.
            /// </summary>
            Global,
            /// <summary>
            /// Actions that are only available when one controller is off.
            /// </summary>
            OneHanded,
            /// <summary>
            /// Actions that are enabled when the quick menu (the menu on the controller) is open.
            /// </summary>
            QuickMenu,
            /// <summary>
            /// Actions that are enabled when the main menu is fully open.
            /// </summary>
            Menu,
            /// <summary>
            /// Actions that are enabled when the avatar's menu is open.
            /// </summary>
            AvatarMenu,
        }

        public enum EVRGameAction
        {
            Interact,
            Jump,
            ToggleMute,
            GrabLeft,
            GrabRight,
            PlayspaceDragLeft,
            PlayspaceDragRight,
            ToggleQuickMenu,
            ToggleMenu,
            ToggleAvatarMenu,
            LeftHandPose,
            RightHandPose,
            Locomote,
            Turn,
        }

        public override void RegisterInput(InputInterface input)
        {
            input.RegisterVRBoolAction(EVRActionCategory.Global, EVRGameAction.Jump, CharacterPawn.Jump);
            input.RegisterVRVector2Action(EVRActionCategory.Global, EVRGameAction.Locomote, Locomote);
            input.RegisterVRVector2Action(EVRActionCategory.Global, EVRGameAction.Turn, Turn);
            input.RegisterVRFloatAction(EVRActionCategory.Global, EVRGameAction.GrabLeft, GrabLeft);
            input.RegisterVRFloatAction(EVRActionCategory.Global, EVRGameAction.GrabRight, GrabRight);
        }

        private void GrabLeft(float lastGrabForce, float grabForce)
        {
            if (grabForce > GrabForceThreshold)
                Grab(true);
            else if (grabForce < ReleaseForceThreshold)
                Release(true);
        }

        private void GrabRight(float lastGrabForce, float grabForce)
        {
            if (grabForce > GrabForceThreshold)
                Grab(false);
            else if (grabForce < ReleaseForceThreshold)
                Release(false);
        }

        private void Release(bool left)
        {
            //Destroy constraint between hand and object
            if (left)
            {
                LeftHandConstraint?.Release();
                LeftHandConstraint = null;
            }
            else
            {
                RightHandConstraint?.Release();
                RightHandConstraint = null;
            }
        }

        private unsafe void Grab(bool left)
        {
            //Attach constraint between hand and object

            //First, check if we can grab anything new
            if (left)
            {
                if (LeftHandOverlap is null)
                    return;
                if (LeftHandConstraint is not null)
                    return;
            }
            else
            {
                if (RightHandOverlap is null)
                    return;
                if (RightHandConstraint is not null)
                    return;
            }

            if (World?.PhysicsScene is not PhysxScene px)
                return;

            var handRB = left ? LeftHandRigidBody : RightHandRigidBody;
            if (handRB is null) //Can't grab without a hand
                return;
            
            var itemRB = left ? LeftHandOverlap : RightHandOverlap;
            if (itemRB is null) //Can't grab nothing
                return;

            var (handPos, handRot) = handRB.Transform;
            var localFrameHand = (Vector3.Zero, Quaternion.Identity);
            //The item's local frame is the hand transform in the item's local space
            var localFrameItem = itemRB.WorldToLocal(handPos, handRot);
            var joint = CreateGrabConstraint(px, itemRB, handRB, localFrameHand, localFrameItem);
            if (left)
                LeftHandConstraint = joint;
            else
                RightHandConstraint = joint;
        }

        private static unsafe PhysxJoint_Distance CreateGrabConstraint(
            PhysxScene px,
            PhysxDynamicRigidBody item,
            PhysxDynamicRigidBody hand,
            (Vector3 Zero, Quaternion Identity) localFrameHand,
            (Vector3 Zero, Quaternion Identity) localFrameItem)
        {
            var joint = px.NewDistanceJoint(item, localFrameItem, hand, localFrameHand);
            joint.Damping = 0.5f;
            joint.Stiffness = 0.5f;
            joint.MinDistance = 0;
            joint.MaxDistance = 0;
            joint.Tolerance = 0.1f;
            joint.ContactDistance = 0.1f;
            joint.DistanceFlags =
                PxDistanceJointFlags.SpringEnabled |
                PxDistanceJointFlags.MinDistanceEnabled |
                PxDistanceJointFlags.MaxDistanceEnabled;
            return joint;
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            if (RightHandRigidBody is null && RightHandTransform is not null)
                InitRightHandBody();
            if (LeftHandRigidBody is null && LeftHandTransform is not null)
                InitLeftHandBody();
        }

        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
            if (RightHandRigidBody is not null)
                DeinitRightHandBody();
            if (LeftHandRigidBody is not null)
                DeinitLeftHandBody();
        }

        private void DeinitRightHandBody()
        {
            var tfm = RightHandTransform;
            if (tfm is not null)
                tfm.WorldMatrixChanged -= RightHandTransform_WorldMatrixChanged;
            RightHandRigidBody?.Destroy();
            RightHandRigidBody = null;
        }

        private void DeinitLeftHandBody()
        {
            var tfm = LeftHandTransform;
            if (tfm is not null)
                tfm.WorldMatrixChanged -= LeftHandTransform_WorldMatrixChanged;
            LeftHandRigidBody?.Destroy();
            LeftHandRigidBody = null;
        }

        private void InitLeftHandBody()
        {
            var tfm = LeftHandTransform;
            if (tfm is null)
                return;

            tfm.WorldMatrixChanged += LeftHandTransform_WorldMatrixChanged;
            if (World?.PhysicsScene is PhysxScene px)
            {
                LeftHandRigidBody?.Destroy();
                LeftHandRigidBody = NewHandRigidBody(tfm);
            }
        }

        private void InitRightHandBody()
        {
            var tfm = RightHandTransform;
            if (tfm is null)
                return;

            tfm.WorldMatrixChanged += RightHandTransform_WorldMatrixChanged;
            if (World?.PhysicsScene is PhysxScene px)
            {
                RightHandRigidBody?.Destroy();
                RightHandRigidBody = NewHandRigidBody(tfm);
            }
        }

        private static PhysxDynamicRigidBody NewHandRigidBody(VRControllerTransform tfm)
            => new()
            {
                ActorFlags = PxActorFlags.DisableGravity,
                Flags = PxRigidBodyFlags.Kinematic | PxRigidBodyFlags.UseKinematicTargetForSceneQueries,
                //CollisionGroup = 1,
                //GroupsMask = ,
                KinematicTarget = (tfm.WorldTranslation, tfm.WorldRotation)
            };

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(RightHandTransform):
                        if (RightHandTransform is not null && IsActive)
                            DeinitRightHandBody();
                        break;
                    case nameof(LeftHandTransform):
                        if (LeftHandTransform is not null && IsActive)
                            DeinitLeftHandBody();
                        break;
                    case nameof(RightHandRigidBody):
                        {
                            if (RightHandRigidBody is not null && World?.PhysicsScene is PhysxScene px)
                                px.RemoveActor(RightHandRigidBody);
                        }
                        break;
                    case nameof(LeftHandRigidBody):
                        {
                            if (LeftHandRigidBody is not null && World?.PhysicsScene is PhysxScene px)
                                px.RemoveActor(LeftHandRigidBody);
                        }
                        break;
                    case nameof(LeftHandConstraint):
                        if (LeftHandConstraint is not null && LeftHandOverlap is not null)
                            LeftHandReleased?.Invoke(LeftHandOverlap);
                        break;
                    case nameof(RightHandConstraint):
                        if (RightHandConstraint is not null && RightHandOverlap is not null)
                            RightHandReleased?.Invoke(RightHandOverlap);
                        break;
                }
            }
            return change;
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(RightHandTransform):
                    if (RightHandTransform is not null && IsActive)
                        InitRightHandBody();
                    break;
                case nameof(LeftHandTransform):
                    if (LeftHandTransform is not null && IsActive)
                        InitLeftHandBody();
                    break;
                case nameof(RightHandRigidBody):
                    {
                        if (RightHandRigidBody is null)
                            break;
                        RightHandRigidBody.KinematicTarget = (RightHandTransform?.WorldTranslation ?? Vector3.Zero, RightHandTransform?.WorldRotation ?? Quaternion.Identity);
                        if (World?.PhysicsScene is PhysxScene px)
                            px.AddActor(RightHandRigidBody);
                    }
                    break;
                case nameof(LeftHandRigidBody):
                    {
                        if (LeftHandRigidBody is null)
                            break;
                        LeftHandRigidBody.KinematicTarget = (LeftHandTransform?.WorldTranslation ?? Vector3.Zero, LeftHandTransform?.WorldRotation ?? Quaternion.Identity);
                        if (World?.PhysicsScene is PhysxScene px)
                            px.AddActor(LeftHandRigidBody);
                    }
                    break;
                case nameof(LeftHandOverlap):
                    if (LeftHandOverlap is not null)
                        LeftHandOverlapChanged?.Invoke(LeftHandOverlap);
                    break;
                case nameof(RightHandOverlap):
                    if (RightHandOverlap is not null)
                        RightHandOverlapChanged?.Invoke(RightHandOverlap);
                    break;
                case nameof(LeftHandConstraint):
                    if (LeftHandConstraint is not null && LeftHandOverlap is not null)
                        LeftHandGrabbed?.Invoke(LeftHandOverlap);
                    break;
                case nameof(RightHandConstraint):
                    if (RightHandConstraint is not null && RightHandOverlap is not null)
                        RightHandGrabbed?.Invoke(RightHandOverlap);
                    break;
            }
        }

        public event Action<PhysxDynamicRigidBody>? LeftHandGrabbed;
        public event Action<PhysxDynamicRigidBody>? RightHandGrabbed;
        public event Action<PhysxDynamicRigidBody>? LeftHandReleased;
        public event Action<PhysxDynamicRigidBody>? RightHandReleased;
        public event Action<PhysxDynamicRigidBody>? LeftHandOverlapChanged;
        public event Action<PhysxDynamicRigidBody>? RightHandOverlapChanged;

        private unsafe void RightHandTransform_WorldMatrixChanged(TransformBase tfm)
        {
            //Don't do anything if the hand doesn't exist or is constrained to an item already
            if (RightHandRigidBody is null || RightHandConstraint is not null)
                return;

            RightHandRigidBody.KinematicTarget = (tfm.WorldTranslation, tfm.WorldRotation);

            if (World?.PhysicsScene is PhysxScene px)
                RightHandOverlap = OverlapTest(tfm, px);
        }

        private unsafe void LeftHandTransform_WorldMatrixChanged(TransformBase tfm)
        {
            //Don't do anything if the hand doesn't exist or is constrained to an item already
            if (LeftHandRigidBody is null || LeftHandConstraint is not null)
                return;

            LeftHandRigidBody.KinematicTarget = (tfm.WorldTranslation, tfm.WorldRotation);

            if (World?.PhysicsScene is PhysxScene px)
                LeftHandOverlap = OverlapTest(tfm, px);
        }

        private unsafe PhysxDynamicRigidBody? OverlapTest(TransformBase tfm, PhysxScene px)
        {
            var handPos = tfm.WorldTranslation;
            var sphere = new IAbstractPhysicsGeometry.Sphere(GrabRadius);
            return px.OverlapAny(sphere, (handPos, Quaternion.Identity), out var hit, PxQueryFlags.Dynamic, null, null) &&
                PhysxDynamicRigidBody.AllDynamic.TryGetValue((nint)hit.actor, out var a) &&
                a is PhysxDynamicRigidBody rb
                ? rb
                : null;
        }

        private void Turn(Vector2 oldValue, Vector2 newValue)
            => CharacterPawn.LookRight(newValue.X);

        private void Locomote(Vector2 oldValue, Vector2 newValue)
        {
            CharacterPawn.MoveRight(newValue.X);
            CharacterPawn.MoveForward(newValue.Y);
        }
    }
}
