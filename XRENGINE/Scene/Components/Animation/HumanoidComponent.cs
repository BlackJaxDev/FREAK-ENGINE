﻿using Extensions;
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

        protected internal override void AddedToSceneNode(SceneNode sceneNode)
        {
            base.AddedToSceneNode(sceneNode);
            SetFromNode();
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
        public TransformBase? HipsTarget => Hips.Node?.Transform;

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
                if (HeadTarget is not null && HipsTarget is not null)
                    SolveFABRIKWithFixedEnds(GetHipToHeadChain(), HipsTarget?.WorldTranslation ?? Vector3.Zero, HeadTarget?.WorldTranslation ?? Vector3.Zero);

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
        public static void SolveFABRIK(
            BoneChainItem[] chain,
            Vector3 targetPosition,
            float tolerance = 0.001f,
            int maxIterations = 10)
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
        public static void SolveFABRIKWithFixedEnds(
            BoneChainItem[] chain,
            Vector3 startPosition,
            Vector3 endPosition,
            float tolerance = 0.001f,
            int maxIterations = 10)
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

        public void SetFromNode()
        {
            Debug.Out(SceneNode.PrintTree());

            //Start at the hips
            Hips.Node = SceneNode.FindDescendantByName("Hips", StringComparison.InvariantCultureIgnoreCase);

            //Find middle bones
            FindChildren(Hips, [
                (Spine, ByName("Spine")),
                (Chest, ByName("Chest")),
                (Left.Leg, ByPosition("Leg", x => x.X > 0.0f)),
                (Right.Leg, ByPosition("Leg", x => x.X < 0.0f)),
            ]);

            if (Spine.Node is not null && Chest.Node is null)
                FindChildren(Spine, [
                    (Chest, ByName("Chest")),
                ]);

            if (Chest.Node is not null)
                FindChildren(Chest, [
                    (Neck, ByName("Neck")),
                    (Head, ByName("Head")),
                    (Left.Shoulder, ByPosition("Shoulder", x => x.X > 0.0f)),
                    (Right.Shoulder, ByPosition("Shoulder", x => x.X < 0.0f)),
                ]);

            if (Neck.Node is not null && Head.Node is null)
                FindChildren(Neck, [
                    (Head, ByName("Head")),
                ]);

            //Find shoulder bones
            if (Left.Shoulder.Node is not null)
                FindChildren(Left.Shoulder, [
                    (Left.Arm, ByName("Arm")),
                    (Left.Elbow, ByName("Elbow")),
                    (Left.Wrist, ByName("Wrist")),
                ]);

            if (Right.Shoulder.Node is not null)
                FindChildren(Right.Shoulder, [
                    (Right.Arm, ByName("Arm")),
                    (Right.Elbow, ByName("Elbow")),
                    (Right.Wrist, ByName("Wrist")),
                ]);

            if (Left.Arm.Node is not null && Left.Elbow.Node is null)
                FindChildren(Left.Arm, [
                    (Left.Elbow, ByName("Elbow")),
                    (Left.Wrist, ByName("Wrist")),
                ]);

            if (Right.Arm.Node is not null && Right.Elbow.Node is null)
                FindChildren(Right.Arm, [
                    (Right.Elbow, ByName("Elbow")),
                    (Right.Wrist, ByName("Wrist")),
                ]);

            if (Left.Elbow.Node is not null && Left.Wrist.Node is null)
                FindChildren(Left.Elbow, [
                    (Left.Wrist, ByName("Wrist")),
                ]);

            if (Right.Elbow.Node is not null && Right.Wrist.Node is null)
                FindChildren(Right.Elbow, [
                    (Right.Wrist, ByName("Wrist")),
                ]);

            //Find finger bones
            if (Left.Wrist.Node is not null)
                FindChildren(Left.Wrist, [
                    (Left.Hand.Pinky.Proximal, ByNameContainsAll("pinky", "1")),
                    (Left.Hand.Pinky.Intermediate, ByNameContainsAll("pinky", "2")),
                    (Left.Hand.Pinky.Distal, ByNameContainsAll("pinky", "3")),
                    (Left.Hand.Ring.Proximal, ByNameContainsAll("ring", "1")),
                    (Left.Hand.Ring.Intermediate, ByNameContainsAll("ring", "2")),
                    (Left.Hand.Ring.Distal, ByNameContainsAll("ring", "3")),
                    (Left.Hand.Middle.Proximal, ByNameContainsAll("middle", "1")),
                    (Left.Hand.Middle.Intermediate, ByNameContainsAll("middle", "2")),
                    (Left.Hand.Middle.Distal, ByNameContainsAll("middle", "3")),
                    (Left.Hand.Index.Proximal, ByNameContainsAll("index", "1")),
                    (Left.Hand.Index.Intermediate, ByNameContainsAll("index", "2")),
                    (Left.Hand.Index.Distal, ByNameContainsAll("index", "3")),
                    (Left.Hand.Thumb.Proximal, ByNameContainsAll("thumb", "1")),
                    (Left.Hand.Thumb.Intermediate, ByNameContainsAll("thumb", "2")),
                    (Left.Hand.Thumb.Distal, ByNameContainsAll("thumb", "3")),
                ]);

            if (Right.Wrist.Node is not null)
                FindChildren(Right.Wrist, [
                    (Right.Hand.Pinky.Proximal, ByNameContainsAll("pinky", "1")),
                    (Right.Hand.Pinky.Intermediate, ByNameContainsAll("pinky", "2")),
                    (Right.Hand.Pinky.Distal, ByNameContainsAll("pinky", "3")),
                    (Right.Hand.Ring.Proximal, ByNameContainsAll("ring", "1")),
                    (Right.Hand.Ring.Intermediate, ByNameContainsAll("ring", "2")),
                    (Right.Hand.Ring.Distal, ByNameContainsAll("ring", "3")),
                    (Right.Hand.Middle.Proximal, ByNameContainsAll("middle", "1")),
                    (Right.Hand.Middle.Intermediate, ByNameContainsAll("middle", "2")),
                    (Right.Hand.Middle.Distal, ByNameContainsAll("middle", "3")),
                    (Right.Hand.Index.Proximal, ByNameContainsAll("index", "1")),
                    (Right.Hand.Index.Intermediate, ByNameContainsAll("index", "2")),
                    (Right.Hand.Index.Distal, ByNameContainsAll("index", "3")),
                    (Right.Hand.Thumb.Proximal, ByNameContainsAll("thumb", "1")),
                    (Right.Hand.Thumb.Intermediate, ByNameContainsAll("thumb", "2")),
                    (Right.Hand.Thumb.Distal, ByNameContainsAll("thumb", "3")),
                ]);

            //Find leg bones
            if (Left.Leg.Node is not null)
                FindChildren(Left.Leg, [
                    (Left.Knee, ByName("Knee")),
                    (Left.Ankle, ByName("Ankle")),
                    (Left.Toes, ByName("Toes")),
                ]);

            if (Right.Leg.Node is not null)
                FindChildren(Right.Leg, [
                    (Right.Knee, ByName("Knee")),
                    (Right.Ankle, ByName("Ankle")),
                    (Right.Toes, ByName("Toes")),
                ]);

            if (Left.Knee.Node is not null && Left.Ankle.Node is null)
                FindChildren(Left.Knee, [
                    (Left.Ankle, ByName("Ankle")),
                    (Left.Toes, ByName("Toes")),
                ]);

            if (Right.Knee.Node is not null && Right.Ankle.Node is null)
                FindChildren(Right.Knee, [
                    (Right.Ankle, ByName("Ankle")),
                    (Right.Toes, ByName("Toes")),
                ]);

            if (Left.Ankle.Node is not null && Left.Toes.Node is null)
                FindChildren(Left.Ankle, [
                    (Left.Toes, ByName("Toes")),
                ]);

            if (Right.Ankle.Node is not null && Right.Toes.Node is null)
                FindChildren(Right.Ankle, [
                    (Right.Toes, ByName("Toes")),
                ]);
        }

        private static Func<SceneNode, bool> ByNameContainsAny(params string[] names)
            => node => names.Any(name => node.Name?.Contains(name, StringComparison.InvariantCultureIgnoreCase) ?? false);

        private static Func<SceneNode, bool> ByNameContainsAll(params string[] names)
            => node => names.Any(name => node.Name?.Contains(name, StringComparison.InvariantCultureIgnoreCase) ?? false);

        private static Func<SceneNode, bool> ByNameContainsAny(StringComparison comp, params string[] names)
            => node => names.Any(name => node.Name?.Contains(name, comp) ?? false);

        private static Func<SceneNode, bool> ByNameContainsAll(StringComparison comp, params string[] names)
            => node => names.Any(name => node.Name?.Contains(name, comp) ?? false);
        
        private static Func<SceneNode, bool> ByPosition(string nameContains, Func<Vector3, bool> posMatch, StringComparison comp = StringComparison.InvariantCultureIgnoreCase)
            => node => (nameContains is null || (node.Name?.Contains(nameContains, comp) ?? false)) && posMatch(node.Transform.WorldTranslation);

        private static Func<SceneNode, bool> ByName(string name, StringComparison comp = StringComparison.InvariantCultureIgnoreCase)
            => node => node.Name?.Equals(name, comp) ?? false;

        private static void FindChildren(BoneDef def, (BoneDef, Func<SceneNode, bool>)[] childSearch)
        {
            var children = def?.Node?.Transform.Children;
            if (children is not null)
                foreach (TransformBase child in children)
                    SetNodeRefs(child, childSearch);
        }

        private static void SetNodeRefs(TransformBase child, (BoneDef, Func<SceneNode, bool>)[] values)
        {
            var node = child.SceneNode;
            if (node is null)
                return;

            foreach ((BoneDef def, var func) in values)
                if (func(node))
                    def.Node = node;
        }
    }
}