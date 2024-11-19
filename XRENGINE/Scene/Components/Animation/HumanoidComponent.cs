using Extensions;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Data.Transforms.Rotations;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;

namespace XREngine.Scene.Components.Animation
{
    public class HumanoidComponent : XRComponent, IRenderable
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

        public HumanoidComponent() 
        {
            RenderedObjects = [RenderInfo3D.New(this, EDefaultRenderPass.OpaqueForward, Render)];
        }

        private void Render(bool shadowPass)
        {
            if (shadowPass)
                return;

            // Draw bones
            //foreach (var bone in GetHipToHeadChain())
            //    Engine.Rendering.Debug.RenderPoint(bone.WorldPosition, ColorF4.Red, false);

            foreach (var bone in GetLeftShoulderToWristChain())
                Engine.Rendering.Debug.RenderPoint(bone.WorldPosition, ColorF4.Red, false);

            //foreach (var bone in GetRightShoulderToWristChain())
            //    Engine.Rendering.Debug.RenderPoint(bone.WorldPosition, ColorF4.Red, false);

            //foreach (var bone in GetLeftLegToAnkleChain())
            //    Engine.Rendering.Debug.RenderPoint(bone.WorldPosition, ColorF4.Red, false);

            //foreach (var bone in GetRightLegToAnkleChain())
            //    Engine.Rendering.Debug.RenderPoint(bone.WorldPosition, ColorF4.Red, false);
        }

        public class BoneDef : XRBase
        {
            private Rotator? _minRotation;
            private Rotator? _maxRotation;
            private Vector3 _minPositionOffset;
            private Vector3 _maxPositionOffset;
            private bool _isRoot;
            private bool _isMovable = false;
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
            public BoneDef Foot { get; } = new();
            public BoneDef Toes { get; } = new();
        }

        public BodySide Left { get; } = new();
        public BodySide Right { get; } = new();

        public BoneChainItem[]? _hipToHeadChain = null;
        public BoneChainItem[]? _leftLegToAnkleChain = null;
        public BoneChainItem[]? _rightLegToAnkleChain = null;
        public BoneChainItem[]? _leftShoulderToWristChain = null;
        public BoneChainItem[]? _rightShoulderToWristChain = null;

        private static BoneChainItem[] Link(BoneDef[] bones)
        {
            BoneChainItem[] chain = new BoneChainItem[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                chain[i] = bones[i];
                if (i > 0)
                {
                    chain[i].ParentPrev = chain[i - 1];
                    chain[i - 1].ChildNext = chain[i];
                }
            }
            return chain;
        }

        public BoneChainItem[] GetHipToHeadChain()
            => _hipToHeadChain ??= Link([Hips, Spine, Chest, Neck, Head]);

        public BoneChainItem[] GetLeftLegToAnkleChain()
            => _leftLegToAnkleChain ??= Link([Left.Leg, Left.Knee, Left.Foot]);

        public BoneChainItem[] GetRightLegToAnkleChain()
            => _rightLegToAnkleChain ??= Link([Right.Leg, Right.Knee, Right.Foot]);

        public BoneChainItem[] GetLeftShoulderToWristChain()
            => _leftShoulderToWristChain ??= Link([Left.Shoulder, Left.Arm, Left.Elbow, Left.Wrist]);

        public BoneChainItem[] GetRightShoulderToWristChain()
            => _rightShoulderToWristChain ??= Link([Right.Shoulder, Right.Arm, Right.Elbow, Right.Wrist]);

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

        public RenderInfo[] RenderedObjects { get; }

        public void SolveFullBodyIK()
        {
            //using var d = Engine.Profiler.Start();

            int maxIterations = 10;
            for (int i = 0; i < maxIterations; i++)
            {
                //if (HeadTarget is not null && HipsTarget is not null)
                //    SolveFABRIKWithFixedEnds(
                //        GetHipToHeadChain(),
                //        HipsTarget?.WorldTranslation ?? Vector3.Zero,
                //        HeadTarget?.WorldTranslation ?? Vector3.Zero);

                if (LeftHandTarget is not null)
                    SolveFABRIK(
                        GetLeftShoulderToWristChain(),
                        LeftHandTarget.WorldTranslation);

                //if (RightHandTarget is not null)
                //    SolveFABRIK(
                //        GetRightShoulderToWristChain(),
                //        RightHandTarget.WorldTranslation);

                //if (LeftFootTarget is not null)
                //    SolveFABRIK(
                //        GetLeftLegToAnkleChain(),
                //        LeftFootTarget.WorldTranslation);

                //if (RightFootTarget is not null)
                //    SolveFABRIK(
                //        GetRightLegToAnkleChain(),
                //        RightFootTarget.WorldTranslation);
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
            private Vector3 _worldAxis;
            private Vector3 _worldPosition = Vector3.Zero;

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

            //protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
            //{
            //    base.OnPropertyChanged(propName, prev, field);
            //    switch (propName)
            //    {
            //        case nameof(ChildNext):
            //            WorldAxis = (ChildPositionWorld - WorldPosition).Normalize();
            //            //Debug.Out($"Local axis set to {WorldAxis}");
            //            break;
            //    }
            //}

            public Vector3 LocalDirToChild
                => (ChildPositionLocal - LocalPosition).Normalized();

            public Vector3 WorldDirToChild
                => (ChildPositionWorld - WorldPosition).Normalized();

            public Vector3 ChildPositionLocal
                => Vector3.Transform(ChildNext?.WorldPosition ?? Vector3.Zero, Transform?.InverseWorldMatrix ?? Matrix4x4.Identity);

            public Vector3 ChildPositionWorld
                => ChildNext?.WorldPosition ?? Vector3.Zero;

            public Vector3 WorldPosition
            {
                get => _worldPosition;
                set => SetField(ref _worldPosition, value);
            }

            public Vector3 LocalPosition
            {
                get => Transform?.Translation ?? Vector3.Zero;
                set
                {
                    if (Transform is not null)
                        Transform.Translation = value;
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
            public float Length { get; set; }
            public Transform? Transform => SceneNode?.GetTransformAs<Transform>(true);
            public SceneNode? SceneNode => Def.Node;
            public Vector3 WorldAxis
            {
                get => _worldAxis;
                set => SetField(ref _worldAxis, value);
            }
        }
        public static void SolveFABRIK(
            BoneChainItem[] chain,
            Vector3 targetPosition,
            float tolerance = 0.001f,
            int maxIterations = 10)
        {
            if (chain.Length < 2)
            {
                Debug.LogWarning("The chain must have at least two bones.");
                return;
            }

            float totalLength = Init(chain);
            bool hasMovableRoot = chain[0].Def.IsMovable;
            Vector3 originalRootPosition = chain[0].WorldPosition;
            float distanceToTarget = Vector3.Distance(originalRootPosition, targetPosition);

            //if (distanceToTarget > totalLength)
            //{
            //    //TODO: if any bones are movable, move them as much as possible to reach the target.
            //}

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
                        parent.WorldPosition = child.WorldPosition - DirFromTo(parent.WorldPosition, child.WorldPosition) * parent.Length;
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
                    child.WorldPosition = parent.WorldPosition + DirFromTo(parent.WorldPosition, child.WorldPosition) * parent.Length;
                }

                diff = Vector3.Distance(chain[^1].WorldPosition, targetPosition);
                iterations++;
            }

            UpdateBones(chain);
        }

        private static Vector3 DirFromTo(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            return dir.Normalized();
        }

        private static float Init(BoneChainItem[] chain)
        {
            //Set the current positions of all bones
            for (int i = 0; i < chain.Length; i++)
            {
                var tfm = chain[i].Transform;
                if (tfm is not null)
                    chain[i].WorldPosition = tfm.WorldTranslation;
                else
                    Debug.LogWarning($"Bone {i} has no transform.");
            }

            foreach (var bone in chain)
                bone.WorldAxis = (bone.ChildPositionWorld - bone.WorldPosition).Normalized();

            // Calculate total length
            float totalLength = 0;
            chain[^1].Length = 0; // Last bone has no length
            for (int i = 0; i < chain.Length - 1; i++)
            {
                var parent = chain[i];
                var child = chain[i + 1];
                parent.WorldAxis = (child.WorldPosition - parent.WorldPosition).Normalized();
                totalLength += parent.Length = parent.WorldPosition.Distance(child?.WorldPosition ?? Vector3.Zero);
            }
            return totalLength;
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

            float totalLength = Init(chain);

            chain[0].WorldPosition = startPosition;
            chain[numBones - 1].WorldPosition = endPosition;

            // Distance between the fixed points
            if (Vector3.DistanceSquared(startPosition, endPosition) > totalLength * totalLength)
            {
                //Debug.LogWarning("The fixed points are too far apart to be connected by the chain.");
                //Stretch the chain to reach both points
                //TODO: implement stretching factors for each bone.
                for (int i = 1; i < numBones - 1; i++)
                    chain[i].WorldPosition = Vector3.Lerp(startPosition, endPosition, (float)i / (numBones - 1));
            }
            else
            {
                int iterations = 0;
                float diff = float.MaxValue;
                while (diff > tolerance && iterations < maxIterations)
                {
                    //Store positions to check for convergence
                    Vector3[] prevPositions = new Vector3[numBones];
                    for (int i = 0; i < numBones; i++)
                        prevPositions[i] = chain[i].WorldPosition;
                    
                    // **Forward Reaching Phase**
                    chain[0].WorldPosition = startPosition;
                    for (int i = 0; i < numBones - 1; i++)
                    {
                        float length = chain[i].Length;
                        Vector3 dir = (chain[i + 1].WorldPosition - chain[i].WorldPosition).Normalized();
                        chain[i + 1].WorldPosition = chain[i].WorldPosition + dir * length;
                    }

                    // **Backward Reaching Phase**
                    chain[numBones - 1].WorldPosition = endPosition;
                    for (int i = numBones - 2; i >= 0; i--)
                    {
                        float length = chain[i].Length;
                        Vector3 dir = (chain[i].WorldPosition - chain[i + 1].WorldPosition).Normalized();
                        chain[i].WorldPosition = chain[i + 1].WorldPosition + dir * length;
                    }

                    //Enforce the start position again
                    chain[0].WorldPosition = startPosition;

                    //Compute the maximum change in positions
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
            UpdateBones(chain);
        }

        public static Vector3 ConstrainPosition(BoneChainItem bone, Vector3 originalPosition)
            => ApplyDistanceCurve(originalPosition, bone.WorldPosition, 1.0f, AnimationCurve.EaseOut);

        public static void UpdateBones(BoneChainItem[] chain)
        {
            for (int i = 0; i < chain.Length - 1; i++)
            {
                BoneChainItem parent = chain[i];
                //parent.LocalPosition = parent.Transform?.WorldTranslation ?? Vector3.Zero;
                Quaternion worldRotation = XRMath.RotationBetweenVectors(parent.WorldAxis, parent.WorldDirToChild);
                Quaternion localRotation = Quaternion.Inverse((parent.Transform?.Parent as Transform)?.Rotation ?? Quaternion.Identity) * worldRotation;
                parent.LocalRotation = localRotation;
            }
        }
        //public static Quaternion ApplyJointConstraints(BoneChainItem bone, Quaternion desiredRotation)
        //{
        //    //Rotator euler = Rotator.FromQuaternion(desiredRotation);
        //    //euler.NormalizeRotations180();
            
        //    //var max = bone.Def.MaxRotation;
        //    //var min = bone.Def.MinRotation;

        //    //// Clamp each axis based on bone's rotation limits
        //    //if (min is not null && max is not null)
        //    //{
        //    //    euler.Yaw = euler.Yaw.Clamp(min.Value.Yaw, max.Value.Yaw);
        //    //    euler.Pitch = euler.Pitch.Clamp(min.Value.Pitch, max.Value.Pitch);
        //    //    euler.Roll = euler.Roll.Clamp(min.Value.Roll, max.Value.Roll);
        //    //}
        //    //else if (min is not null)
        //    //{
        //    //    euler.Yaw = euler.Yaw.ClampMin(min.Value.Yaw);
        //    //    euler.Pitch = euler.Pitch.ClampMin(min.Value.Pitch);
        //    //    euler.Roll = euler.Roll.ClampMin(min.Value.Roll);
        //    //}
        //    //else if (max is not null)
        //    //{
        //    //    euler.Yaw = euler.Yaw.ClampMax(max.Value.Yaw);
        //    //    euler.Pitch = euler.Pitch.ClampMax(max.Value.Pitch);
        //    //    euler.Roll = euler.Roll.ClampMax(max.Value.Roll);
        //    //}
        //    return desiredRotation;
        //}
        public static Vector3 ApplyDistanceCurve(Vector3 originalPosition, Vector3 currentPosition, float maxDistance, AnimationCurve curve)
            => originalPosition + (currentPosition - originalPosition) * curve.Evaluate(Vector3.Distance(originalPosition, currentPosition) / maxDistance);

        public void SetFromNode()
        {
            //Debug.Out(SceneNode.PrintTree());

            //Start at the hips
            Hips.Node = SceneNode.FindDescendantByName("Hips", StringComparison.InvariantCultureIgnoreCase);

            //Find middle bones
            FindChildrenFor(Hips, [
                (Spine, ByName("Spine")),
                (Chest, ByName("Chest")),
                (Left.Leg, ByPosition("Leg", x => x.X > 0.0f)),
                (Right.Leg, ByPosition("Leg", x => x.X < 0.0f)),
            ]);

            if (Spine.Node is not null && Chest.Node is null)
                FindChildrenFor(Spine, [
                    (Chest, ByName("Chest")),
                ]);

            if (Chest.Node is not null)
                FindChildrenFor(Chest, [
                    (Neck, ByName("Neck")),
                    (Head, ByName("Head")),
                    (Left.Shoulder, ByPosition("Shoulder", x => x.X > 0.0f)),
                    (Right.Shoulder, ByPosition("Shoulder", x => x.X < 0.0f)),
                ]);

            if (Neck.Node is not null && Head.Node is null)
                FindChildrenFor(Neck, [
                    (Head, ByName("Head")),
                ]);

            //Find shoulder bones
            if (Left.Shoulder.Node is not null)
                FindChildrenFor(Left.Shoulder, [
                    (Left.Arm, ByNameContainsAll("Arm")),
                    (Left.Elbow, ByNameContainsAll("Elbow")),
                    (Left.Wrist, ByNameContainsAll("Wrist")),
                ]);

            if (Right.Shoulder.Node is not null)
                FindChildrenFor(Right.Shoulder, [
                    (Right.Arm, ByNameContainsAll("Arm")),
                    (Right.Elbow, ByNameContainsAll("Elbow")),
                    (Right.Wrist, ByNameContainsAll("Wrist")),
                ]);

            if (Left.Arm.Node is not null && Left.Elbow.Node is null)
                FindChildrenFor(Left.Arm, [
                    (Left.Elbow, ByNameContainsAll("Elbow")),
                    (Left.Wrist, ByNameContainsAll("Wrist")),
                ]);

            if (Right.Arm.Node is not null && Right.Elbow.Node is null)
                FindChildrenFor(Right.Arm, [
                    (Right.Elbow, ByNameContainsAll("Elbow")),
                    (Right.Wrist, ByNameContainsAll("Wrist")),
                ]);

            if (Left.Elbow.Node is not null && Left.Wrist.Node is null)
                FindChildrenFor(Left.Elbow, [
                    (Left.Wrist, ByNameContainsAll("Wrist")),
                ]);

            if (Right.Elbow.Node is not null && Right.Wrist.Node is null)
                FindChildrenFor(Right.Elbow, [
                    (Right.Wrist, ByNameContainsAll("Wrist")),
                ]);

            //Find finger bones
            if (Left.Wrist.Node is not null)
                FindChildrenFor(Left.Wrist, [
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
                FindChildrenFor(Right.Wrist, [
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
                FindChildrenFor(Left.Leg, [
                    (Left.Knee, ByNameContainsAll("Knee")),
                    (Left.Foot, ByNameContainsAll("Foot")),
                    (Left.Toes, ByNameContainsAll("Toe")),
                ]);

            if (Right.Leg.Node is not null)
                FindChildrenFor(Right.Leg, [
                    (Right.Knee, ByNameContainsAll("Knee")),
                    (Right.Foot, ByNameContainsAll("Foot")),
                    (Right.Toes, ByNameContainsAll("Toe")),
                ]);

            if (Left.Knee.Node is not null && Left.Foot.Node is null)
                FindChildrenFor(Left.Knee, [
                    (Left.Foot, ByNameContainsAll("Foot")),
                    (Left.Toes, ByNameContainsAll("Toe")),
                ]);

            if (Right.Knee.Node is not null && Right.Foot.Node is null)
                FindChildrenFor(Right.Knee, [
                    (Right.Foot, ByNameContainsAll("Foot")),
                    (Right.Toes, ByNameContainsAll("Toe")),
                ]);

            if (Left.Foot.Node is not null && Left.Toes.Node is null)
                FindChildrenFor(Left.Foot, [
                    (Left.Toes, ByNameContainsAll("Toe")),
                ]);

            if (Right.Foot.Node is not null && Right.Toes.Node is null)
                FindChildrenFor(Right.Foot, [
                    (Right.Toes, ByNameContainsAll("Toe")),
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
            => node => (nameContains is null || (node.Name?.Contains(nameContains, comp) ?? false)) && posMatch(node.Transform.LocalTranslation);

        private static Func<SceneNode, bool> ByName(string name, StringComparison comp = StringComparison.InvariantCultureIgnoreCase)
            => node => node.Name?.Equals(name, comp) ?? false;

        private static void FindChildrenFor(BoneDef def, (BoneDef, Func<SceneNode, bool>)[] childSearch)
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
