using System.Numerics;
using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;
using static XREngine.Scene.Components.Animation.InverseKinematics;

namespace XREngine.Scene.Components.Animation
{
    public class HumanoidComponent : XRComponent, IRenderable
    {
        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            if (SolveIK)
                RegisterTick(ETickGroup.PrePhysics, ETickOrder.Scene, SolveFullBodyIK);
        }
        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
            if (SolveIK)
                UnregisterTick(ETickGroup.PrePhysics, ETickOrder.Scene, SolveFullBodyIK);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(SolveIK):
                    if (IsActive)
                    {
                        if (SolveIK)
                            RegisterTick(ETickGroup.PrePhysics, ETickOrder.Scene, SolveFullBodyIK);
                        else
                            UnregisterTick(ETickGroup.PrePhysics, ETickOrder.Scene, SolveFullBodyIK);
                    }
                    break;
            }
        }

        private void SolveFullBodyIK()
        {
            InverseKinematics.SolveFullBodyIK(
                GetHipToHeadChain(),
                GetLeftLegToAnkleChain(),
                GetRightLegToAnkleChain(),
                GetLeftShoulderToWristChain(),
                GetRightShoulderToWristChain(),
                HeadTarget,
                HipsTarget,
                LeftHandTarget,
                RightHandTarget,
                LeftFootTarget,
                RightFootTarget,
                10);
        }

        protected internal override void AddedToSceneNode(SceneNode sceneNode)
        {
            base.AddedToSceneNode(sceneNode);
            SetFromNode();
        }

        private bool _solveIK = false;
        public bool SolveIK
        {
            get => _solveIK;
            set => SetField(ref _solveIK, value);
        }

        public HumanoidComponent() 
        {
            RenderedObjects = [RenderInfo3D.New(this, EDefaultRenderPass.OpaqueForward, Render)];
        }

        private void Render(bool shadowPass)
        {
            if (shadowPass)
                return;

            foreach (var bone in GetHipToHeadChain())
                Engine.Rendering.Debug.RenderPoint(bone.WorldPosSolve, ColorF4.Red, false);

            foreach (var bone in GetLeftShoulderToWristChain())
                Engine.Rendering.Debug.RenderPoint(bone.WorldPosSolve, ColorF4.Red, false);

            foreach (var bone in GetRightShoulderToWristChain())
                Engine.Rendering.Debug.RenderPoint(bone.WorldPosSolve, ColorF4.Red, false);

            foreach (var bone in GetLeftLegToAnkleChain())
                Engine.Rendering.Debug.RenderPoint(bone.WorldPosSolve, ColorF4.Red, false);

            foreach (var bone in GetRightLegToAnkleChain())
                Engine.Rendering.Debug.RenderPoint(bone.WorldPosSolve, ColorF4.Red, false);
        }

        public class BoneDef : XRBase
        {
            private SceneNode? _node;
            public SceneNode? Node
            {
                get => _node;
                set => SetField(ref _node, value);
            }

            private BoneIKConstraints? _constraints;
            public BoneIKConstraints? Constraints
            {
                get => _constraints;
                set => SetField(ref _constraints, value);
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
            => bones.Any(bone => bone.Node is null) 
            ? [] 
            : [.. bones.Select(bone => new BoneChainItem(bone.Node!, bone.Constraints))];

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

        public (TransformBase? tfm, Matrix4x4 offset) HeadTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) HipsTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LeftHandTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) RightHandTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LeftFootTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) RightFootTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LeftElbowTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) RightElbowTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LeftKneeTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) RightKneeTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) ChestTarget { get; set; } = (null, Matrix4x4.Identity);

        public RenderInfo[] RenderedObjects { get; }

        //public static void AdjustRootConstraints(BoneChainItem rootBone, Vector3 overallMovement)
        //{
        //    // Example: Allow more movement if the target is far away
        //    float distance = overallMovement.Length();

        //    rootBone.Def.MaxPositionOffset = new Vector3(distance * 0.1f, distance * 0.1f, distance * 0.1f);
        //    rootBone.Def.MinPositionOffset = -rootBone.Def.MaxPositionOffset;
        //}
        
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

        /// <summary>
        /// Removes all IK targets, effectively disabling IK.
        /// </summary>
        public void ClearIKTargets()
        {
            HeadTarget = (null, Matrix4x4.Identity);
            LeftHandTarget = (null, Matrix4x4.Identity);
            RightHandTarget = (null, Matrix4x4.Identity);
            HipsTarget = (null, Matrix4x4.Identity);
            LeftFootTarget = (null, Matrix4x4.Identity);
            RightFootTarget = (null, Matrix4x4.Identity);
            ChestTarget = (null, Matrix4x4.Identity);
            LeftElbowTarget = (null, Matrix4x4.Identity);
            RightElbowTarget = (null, Matrix4x4.Identity);
            LeftKneeTarget = (null, Matrix4x4.Identity);
            RightKneeTarget = (null, Matrix4x4.Identity);
        }

        /// <summary>
        /// Resets the pose of the humanoid to the default pose (T-pose).
        /// </summary>
        public void ResetPose()
        {

        }
    }
}
