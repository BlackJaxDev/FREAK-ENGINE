using Extensions;
using System;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Data.Transforms.Rotations;
using XREngine.Scene.Transforms;

namespace XREngine.Scene.Components.Animation
{
    public class HumanoidComponent : XRComponent
    {
        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            RegisterTick(ETickGroup.PrePhysics, ETickOrder.Scene, SolveFullBodyIK);
        }

        public HumanoidComponent() { }

        public class BoneDef : XRBase
        {
            private Rotator? _minRotation;
            private Rotator? _maxRotation;
            private Vector3 _minPositionOffset;
            private Vector3 _maxPositionOffset;
            private bool _isRoot;
            private bool _isMovable;
            private SceneNode? _node;

            public SceneNode? Node
            {
                get => _node;
                set => SetField(ref _node, value);
            }
            public Rotator? MinRotation
            {
                get => _minRotation;
                set => SetField(ref _minRotation, value);
            }
            public Rotator? MaxRotation
            {
                get => _maxRotation;
                set => SetField(ref _maxRotation, value);
            }
            public Vector3 MinPositionOffset
            {
                get => _minPositionOffset;
                set => SetField(ref _minPositionOffset, value);
            }
            public Vector3 MaxPositionOffset
            {
                get => _maxPositionOffset;
                set => SetField(ref _maxPositionOffset, value);
            }
            /// <summary>
            /// Indicates if this is a user-guided bone
            /// </summary>
            public bool IsRoot
            {
                get => _isRoot;
                set => SetField(ref _isRoot, value);
            }
            /// <summary>
            /// Indicates if the bone is allowed to move during solving
            /// </summary>
            public bool IsMovable
            {
                get => _isMovable;
                set => SetField(ref _isMovable, value);
            }
        }

        public BoneDef Hips { get; } = new();
        public BoneDef Spine { get; } = new();
        public BoneDef Chest { get; } = new();
        public BoneDef Neck { get; } = new();
        public BoneDef Head { get; } = new();

        public class BodySide : XRBase
        {
            public class Fingers : XRBase
            {
                public class Finger : XRBase
                {
                    /// <summary>
                    /// First bone
                    /// </summary>
                    public BoneDef Proximal { get; } = new();
                    /// <summary>
                    /// Second bone
                    /// </summary>
                    public BoneDef Intermediate { get; } = new();
                    /// <summary>
                    /// Last bone
                    /// </summary>
                    public BoneDef Distal { get; } = new();
                }

                public Finger Pinky { get; } = new();
                public Finger Ring { get; } = new();
                public Finger Middle { get; } = new();
                public Finger Index { get; } = new();
                public Finger Thumb { get; } = new();
            }

            public BoneDef Shoulder { get; } = new();
            public BoneDef Arm { get; } = new();
            public BoneDef Elbow { get; } = new();
            public BoneDef Wrist { get; } = new();
            public Fingers Hand { get; } = new();
            public BoneDef Leg { get; } = new();
            public BoneDef Knee { get; } = new();
            public BoneDef Ankle { get; } = new();
            public BoneDef Toes { get; } = new();
        }

        public BodySide Left { get; } = new();
        public BodySide Right { get; } = new();

        public BoneChainItem[]? _hipToHeadChain = null;
        public BoneChainItem[]? _leftLegToAnkleChain = null;
        public BoneChainItem[]? _rightLegToAnkleChain = null;
        public BoneChainItem[]? _leftShoulderToWristChain = null;
        public BoneChainItem[]? _rightShoulderToWristChain = null;

        public BoneChainItem[] GetHipToHeadChain()
            => _hipToHeadChain ??= [Hips, Spine, Chest, Neck, Head];

        public BoneChainItem[] GetLeftLegToAnkleChain()
            => _leftLegToAnkleChain ??= [Left.Leg, Left.Knee, Left.Ankle];

        public BoneChainItem[] GetRightLegToAnkleChain()
            => _rightLegToAnkleChain ??= [Right.Leg, Right.Knee, Right.Ankle];

        public BoneChainItem[] GetLeftShoulderToWristChain()
            => _leftShoulderToWristChain ??= [Left.Shoulder, Left.Arm, Left.Elbow, Left.Wrist];

        public BoneChainItem[] GetRightShoulderToWristChain()
            => _rightShoulderToWristChain ??= [Right.Shoulder, Right.Arm, Right.Elbow, Right.Wrist];

        public TransformBase? HeadTarget => Head.Node?.Transform;

        public TransformBase? LeftHandTarget => Left.Wrist.Node?.Transform;
        public TransformBase? RightHandTarget => Right.Wrist.Node?.Transform;

        public TransformBase? LeftFootTarget => Left.Wrist.Node?.Transform;
        public TransformBase? RightFootTarget => Right.Wrist.Node?.Transform;

        public TransformBase? LeftElbowTarget => Left.Wrist.Node?.Transform;
        public TransformBase? RightElbowTarget => Right.Wrist.Node?.Transform;

        public TransformBase? LeftKneeTarget => Left.Wrist.Node?.Transform;
        public TransformBase? RightKneeTarget => Right.Wrist.Node?.Transform;

        public void SolveFullBodyIK()
        {
            Engine.Profiler.Start();

            int maxIterations = 5;
            for (int i = 0; i < maxIterations; i++)
            {
                if (HeadTarget is not null)
                    SolveFABRIK(GetHipToHeadChain(), HeadTarget.WorldTranslation);

                if (LeftHandTarget is not null)
                    SolveFABRIK(GetLeftShoulderToWristChain(), LeftHandTarget.WorldTranslation);

                if (RightHandTarget is not null)
                    SolveFABRIK(GetRightShoulderToWristChain(), RightHandTarget.WorldTranslation);
                
                if (LeftFootTarget is not null)
                    SolveFABRIK(GetLeftLegToAnkleChain(), LeftFootTarget.WorldTranslation);

                if (RightFootTarget is not null)
                    SolveFABRIK(GetRightLegToAnkleChain(), RightFootTarget.WorldTranslation);
            }
        }

        public static void AdjustRootConstraints(BoneChainItem rootBone, Vector3 overallMovement)
        {
            // Example: Allow more movement if the target is far away
            float distance = overallMovement.Length();

            rootBone.Def.MaxPositionOffset = new Vector3(distance * 0.1f, distance * 0.1f, distance * 0.1f);
            rootBone.Def.MinPositionOffset = -rootBone.Def.MaxPositionOffset;
        }
        public class BoneChainItem(BoneDef def) : XRBase
        {
            public static implicit operator BoneChainItem(BoneDef def) => new(def);

            private BoneChainItem? _parentPrev;
            private BoneChainItem? _childNext;
            private BoneDef _def = def;
            private Vector3 _localAxis;

            public BoneChainItem? ParentPrev
            {
                get => _parentPrev;
                set => SetField(ref _parentPrev, value);
            }
            public BoneChainItem? ChildNext
            {
                get => _childNext;
                set => SetField(ref _childNext, value);
            }
            public BoneDef Def
            {
                get => _def;
                set => SetField(ref _def, value);
            }

            protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
            {
                base.OnPropertyChanged(propName, prev, field);
                switch (propName)
                {
                    case nameof(ChildNext):
                        LocalAxis = GetLocalDirToChild();
                        break;
                }
            }

            public Vector3 GetLocalDirToChild()
                => (ChildPositionLocal() - LocalPosition).Normalize();

            public Vector3 GetWorldDirToChild()
                => (ChildPositionWorld() - WorldPosition).Normalize();

            public Vector3 ChildPositionLocal()
                => Vector3.Transform(ChildNext?.WorldPosition ?? Vector3.Zero, Transform?.InverseWorldMatrix ?? Matrix4x4.Identity);

            public Vector3 ChildPositionWorld()
                => ChildNext?.WorldPosition ?? Vector3.Zero;

            public Vector3 LocalPosition
            {
                get => Transform?.Translation ?? Vector3.Zero;
                set
                {
                    if (Transform is not null)
                        Transform.Translation = value;
                }
            }
            public Vector3 WorldPosition
            {
                get => Transform?.WorldTranslation ?? Vector3.Zero;
                set
                {
                    if (Transform is not null)
                        Transform.Translation = value - (ParentPrev?.WorldPosition ?? Vector3.Zero);
                }
            }
            public Rotator LocalRotator
            {
                get => Transform?.Rotator ?? Rotator.GetZero();
                set
                {
                    if (Transform is not null)
                        Transform.Rotator = value;
                }
            }
            public Quaternion LocalRotation
            {
                get => Transform?.Rotation?? Quaternion.Identity;
                set
                {
                    if (Transform is not null)
                        Transform.Rotation = value;
                }
            }
            /// <summary>
            /// Distance to the next child bone in the chain.
            /// </summary>
            public float Length => WorldPosition.Distance(ChildNext?.WorldPosition ?? Vector3.Zero);
            public Transform? Transform => SceneNode?.GetTransformAs<Transform>(true);
            public SceneNode? SceneNode => Def.Node;
            public Vector3 LocalAxis
            {
                get => _localAxis;
                set => SetField(ref _localAxis, value);
            }
        }
        public static void SolveFABRIK(BoneChainItem[] chain, Vector3 targetPosition, float tolerance = 0.001f, int maxIterations = 10)
        {
            // Check if the chain has movable roots
            bool hasMovableRoot = chain[0].Def.IsMovable;

            // Original root position
            Vector3 originalRootPosition = chain[0].WorldPosition;

            // Calculate total length
            float totalLength = 0;
            for (int i = 0; i < chain.Length - 1; i++)
                totalLength += chain[i].Length;

            // Distance from root to target
            //float distanceToTarget = Vector3.Distance(originalRootPosition, targetPosition);

            int iterations = 0;
            float diff = Vector3.Distance(chain[^1].WorldPosition, targetPosition);

            while (diff > tolerance && iterations < maxIterations)
            {
                // **Forward Reaching Phase**

                // Move end effector to the target
                chain[^1].WorldPosition = targetPosition;

                // Iterate backwards through the chain
                for (int i = chain.Length - 2; i >= 0; i--)
                {
                    BoneChainItem parent = chain[i];
                    BoneChainItem child = chain[i + 1];

                    // If the bone is movable
                    if (parent.Def.IsMovable)
                        parent.WorldPosition = child.WorldPosition - parent.GetWorldDirToChild() * parent.Length;
                    else
                        break;
                }

                // **Backward Reaching Phase**

                // If root is movable, constrain its movement
                chain[0].WorldPosition = hasMovableRoot 
                    ? ConstrainPosition(chain[0], originalRootPosition) 
                    : originalRootPosition;

                for (int i = 0; i < chain.Length - 1; i++)
                {
                    BoneChainItem parent = chain[i];
                    BoneChainItem child = chain[i + 1];
                    child.WorldPosition = parent.WorldPosition + parent.GetWorldDirToChild() * parent.Length;
                }

                diff = Vector3.Distance(chain[^1].WorldPosition, targetPosition);
                iterations++;
            }

            UpdateBoneRotations(chain);
        }
        public void SolveFABRIKWithFixedEnds(BoneChainItem[] chain, Vector3 startPosition, Vector3 endPosition, float tolerance = 0.001f, int maxIterations = 10)
        {
            int numBones = chain.Length;
            if (numBones < 2)
            {
                Debug.LogWarning("The chain must have at least two bones.");
                return;
            }

            // Set the start and end positions
            chain[0].WorldPosition = startPosition;
            chain[numBones - 1].WorldPosition = endPosition;

            // Compute the total length of the chain
            float totalLength = 0;
            for (int i = 0; i < numBones - 1; i++)
                totalLength += chain[i].Length;
            
            // Distance between the fixed points
            float distanceBetweenFixedPoints = Vector3.Distance(startPosition, endPosition);

            if (distanceBetweenFixedPoints > totalLength)
            {
                Debug.LogWarning("The fixed points are too far apart to be connected by the chain.");
                // Optionally, stretch the chain to reach both points
                for (int i = 1; i < numBones - 1; i++)
                    chain[i].WorldPosition = Vector3.Lerp(startPosition, endPosition, (float)i / (numBones - 1));
            }
            else
            {
                int iterations = 0;
                float diff = float.MaxValue;

                while (diff > tolerance && iterations < maxIterations)
                {
                    // Store positions to check for convergence
                    Vector3[] prevPositions = new Vector3[numBones];
                    for (int i = 0; i < numBones; i++)
                        prevPositions[i] = chain[i].WorldPosition;
                    
                    // **Forward Reaching Phase**
                    chain[0].WorldPosition = startPosition;
                    for (int i = 0; i < numBones - 1; i++)
                    {
                        float length = chain[i].Length;
                        Vector3 dir = (chain[i + 1].WorldPosition - chain[i].WorldPosition).Normalize();
                        chain[i + 1].WorldPosition = chain[i].WorldPosition + dir * length;
                    }

                    // **Backward Reaching Phase**
                    chain[numBones - 1].WorldPosition = endPosition;
                    for (int i = numBones - 2; i >= 0; i--)
                    {
                        float length = chain[i].Length;
                        Vector3 dir = (chain[i].WorldPosition - chain[i + 1].WorldPosition).Normalize();
                        chain[i].WorldPosition = chain[i + 1].WorldPosition + dir * length;
                    }

                    // Enforce the start position again
                    chain[0].WorldPosition = startPosition;

                    // Compute the maximum change in positions
                    diff = 0;
                    for (int i = 0; i < numBones; i++)
                    {
                        float dist = Vector3.Distance(chain[i].WorldPosition, prevPositions[i]);
                        if (dist > diff)
                            diff = dist;
                    }

                    iterations++;
                }
            }

            // Update rotations after positions are set
            UpdateBoneRotations(chain);
        }

        public static Vector3 ConstrainPosition(BoneChainItem bone, Vector3 originalPosition)
            => ApplyDistanceCurve(originalPosition, bone.WorldPosition, 1.0f, AnimationCurve.EaseOut);
        public static void UpdateBoneRotations(BoneChainItem[] chain)
        {
            for (int i = 0; i < chain.Length - 1; i++)
            {
                BoneChainItem parent = chain[i];
                parent.LocalRotator = ApplyJointConstraints(parent, XRMath.RotationBetweenVectors(parent.LocalAxis, parent.GetLocalDirToChild()));
            }
        }
        public static Rotator ApplyJointConstraints(BoneChainItem bone, Quaternion desiredRotation)
        {
            Rotator euler = Rotator.FromQuaternion(desiredRotation);
            euler.NormalizeRotations180();

            var max = bone.Def.MaxRotation;
            var min = bone.Def.MinRotation;

            // Clamp each axis based on bone's rotation limits
            if (min is not null && max is not null)
            {
                euler.Yaw = euler.Yaw.Clamp(min.Value.Yaw, max.Value.Yaw);
                euler.Pitch = euler.Pitch.Clamp(min.Value.Pitch, max.Value.Pitch);
                euler.Roll = euler.Roll.Clamp(min.Value.Roll, max.Value.Roll);
            }
            else if (min is not null)
            {
                euler.Yaw = euler.Yaw.ClampMin(min.Value.Yaw);
                euler.Pitch = euler.Pitch.ClampMin(min.Value.Pitch);
                euler.Roll = euler.Roll.ClampMin(min.Value.Roll);
            }
            else if (max is not null)
            {
                euler.Yaw = euler.Yaw.ClampMax(max.Value.Yaw);
                euler.Pitch = euler.Pitch.ClampMax(max.Value.Pitch);
                euler.Roll = euler.Roll.ClampMax(max.Value.Roll);
            }
            return euler;
        }
        public static Vector3 ApplyDistanceCurve(Vector3 originalPosition, Vector3 currentPosition, float maxDistance, AnimationCurve curve)
            => originalPosition + (currentPosition - originalPosition) * curve.Evaluate(Vector3.Distance(originalPosition, currentPosition) / maxDistance);
    }
}
