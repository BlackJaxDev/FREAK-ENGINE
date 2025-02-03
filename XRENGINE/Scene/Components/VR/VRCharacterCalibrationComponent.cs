using Extensions;
using FFmpeg.AutoGen;
using OpenVR.NET.Devices;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Components.Scene;
using XREngine.Scene.Components.Animation;
using XREngine.Scene.Transforms;

namespace XREngine.Scene.Components.VR
{
    public class VRCharacterCalibrationComponent : XRComponent
    {
        private HumanoidComponent? _humanoidComponent;
        public HumanoidComponent? HumanoidComponent
        {
            get => _humanoidComponent;
            set => SetField(ref _humanoidComponent, value);
        }

        private VRTrackerCollectionComponent? _trackerCollection;
        public VRTrackerCollectionComponent? TrackerCollection
        {
            get => _trackerCollection;
            set => SetField(ref _trackerCollection, value);
        }

        private VRHeadsetTransform? _headset;
        public VRHeadsetTransform? Headset
        {
            get => _headset;
            set => SetField(ref _headset, value);
        }

        private VRControllerTransform? _leftController;
        public VRControllerTransform? LeftController
        {
            get => _leftController;
            set => SetField(ref _leftController, value);
        }

        private VRControllerTransform? _rightController;
        public VRControllerTransform? RightController
        {
            get => _rightController;
            set => SetField(ref _rightController, value);
        }

        public HumanoidComponent? GetHumanoid()
            => HumanoidComponent ?? GetSiblingComponent<HumanoidComponent>();

        public VRTrackerCollectionComponent? GetTrackerCollection()
            => TrackerCollection ?? GetSiblingComponent<VRTrackerCollectionComponent>();
        
        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            var h = GetHumanoid();
            if (h is null)
                return;

            h.HeadTarget = (Headset, Matrix4x4.Identity);
            h.LeftHandTarget = (LeftController, Matrix4x4.Identity);
            h.RightHandTarget = (RightController, Matrix4x4.Identity);
            h.HipsTarget = (null, Matrix4x4.Identity);
            h.LeftFootTarget = (null, Matrix4x4.Identity);
            h.RightFootTarget = (null, Matrix4x4.Identity);
            h.ChestTarget = (null, Matrix4x4.Identity);
            h.LeftElbowTarget = (null, Matrix4x4.Identity);
            h.RightElbowTarget = (null, Matrix4x4.Identity);
            h.LeftKneeTarget = (null, Matrix4x4.Identity);
            h.RightKneeTarget = (null, Matrix4x4.Identity);
        }

        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();

            var h = GetHumanoid();
            if (h is null)
                return;

            h.ResetPose();
        }

        private void UpdateRootTransform(TransformBase t)
        {
            var h = GetHumanoid();
            if (h is null)
                return;
            var headNode = h.Head.Node;
            if (headNode is null)
                return;
            var rootNode = h.SceneNode;
            var rootToHead = headNode.Transform.WorldTranslation - rootNode.Transform.WorldTranslation;
            Matrix4x4 translation = Matrix4x4.CreateTranslation(t.WorldTranslation - rootToHead);
            //yaw to face the direction of the headset
            var fwd = headNode.Transform.WorldForward;
            switch (fwd.Dot(Globals.Up))
            {
                case > 0.5f:
                    fwd = -headNode.Transform.WorldUp;
                    break;
                case < -0.5f:
                    fwd = headNode.Transform.WorldUp;
                    break;
            }
            fwd.Y = 0.0f;
            fwd = Vector3.Normalize(fwd);
            float yaw = (float)Math.Atan2(fwd.X, fwd.Z);
            rootNode.Transform.DeriveWorldMatrix(Matrix4x4.CreateRotationY(yaw) * translation);
        }

        public bool BeginCalibration()
        {
            if (Headset is null)
                return false;

            var h = GetHumanoid();
            if (h is null)
                return false;

            var headNode = h.Head.Node;
            if (headNode is null)
                return false;

            var trackers = GetTrackerCollection();
            if (trackers is null) //No need to calibrate if there are no trackers
                return false;

            Headset.WorldMatrixChanged += UpdateRootTransform;
            HeadTarget = h.HeadTarget;
            HipsTarget = h.HipsTarget;
            LeftHandTarget = h.LeftHandTarget;
            RightHandTarget = h.RightHandTarget;
            LeftFootTarget = h.LeftFootTarget;
            RightFootTarget = h.RightFootTarget;
            ChestTarget = h.ChestTarget;
            LeftElbowTarget = h.LeftElbowTarget;
            RightElbowTarget = h.RightElbowTarget;
            LeftKneeTarget = h.LeftKneeTarget;
            RightKneeTarget = h.RightKneeTarget;
            h.ResetPose();
            h.SolveIK = false;
            return true;
        }

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

        public float CalibrationRadius { get; set; } = 0.3f;

        public void EndCalibration()
        {
            if (Headset is not null)
                Headset.WorldMatrixChanged -= UpdateRootTransform;

            var h = GetHumanoid();
            if (h is null)
                return;

            h.HeadTarget = (Headset, Matrix4x4.Identity);
            h.LeftHandTarget = (LeftController, Matrix4x4.Identity);
            h.RightHandTarget = (RightController, Matrix4x4.Identity);

            var t = GetTrackerCollection();
            if (t is not null)
            {
                var waistTfm = h.Hips.Node?.Transform;
                var leftFootTfm = h.Left.Foot.Node?.Transform;
                var rightFootTfm = h.Right.Foot.Node?.Transform;

                var chestTfm = h.Chest.Node?.Transform;
                var leftElbowTfm = h.Left.Elbow.Node?.Transform;
                var rightElbowTfm = h.Right.Elbow.Node?.Transform;
                var leftKneeTfm = h.Left.Knee.Node?.Transform;
                var rightKneeTfm = h.Right.Knee.Node?.Transform;

                FindClosestTracker(t, waistTfm, out var WaistTracker, out Matrix4x4 offset);
                h.HipsTarget = (WaistTracker, offset);

                FindClosestTracker(t, leftFootTfm, out var LeftFootTracker, out offset);
                h.LeftFootTarget = (LeftFootTracker, offset);

                FindClosestTracker(t, rightFootTfm, out var RightFootTracker, out offset);
                h.RightFootTarget = (RightFootTracker, offset);

                FindClosestTracker(t, chestTfm, out var ChestTracker, out offset);
                h.ChestTarget = (ChestTracker, offset);

                FindClosestTracker(t, leftElbowTfm, out var LeftElbowTracker, out offset);
                h.LeftElbowTarget = (LeftElbowTracker, offset);

                FindClosestTracker(t, rightElbowTfm, out var RightElbowTracker, out offset);
                h.RightElbowTarget = (RightElbowTracker, offset);

                FindClosestTracker(t, leftKneeTfm, out var LeftKneeTracker, out offset);
                h.LeftKneeTarget = (LeftKneeTracker, offset);

                FindClosestTracker(t, rightKneeTfm, out var RightKneeTracker, out offset);
                h.RightKneeTarget = (RightKneeTracker, offset);
            }

            h.SolveIK = true;
        }

        private void FindClosestTracker(
            VRTrackerCollectionComponent trackerCollection,
            TransformBase? humanoidTfm,
            out TransformBase? closestTracker,
            out Matrix4x4 offset)
        {
            closestTracker = null;
            offset = Matrix4x4.Identity;
            if (humanoidTfm is null)
                return;
            var bodyPos = humanoidTfm.WorldTranslation;
            float closestDist = float.MaxValue;
            foreach ((VrDevice dev, VRTrackerTransform tracker) in trackerCollection.Trackers.Values)
            {
                var trackerPos = tracker.WorldTranslation;
                var dist = Vector3.DistanceSquared(bodyPos, trackerPos);
                if (dist < closestDist && float.Sqrt(dist) < CalibrationRadius)
                {
                    closestDist = dist;
                    closestTracker = tracker;
                }
            }
            offset = closestTracker is not null ? humanoidTfm.WorldMatrix * closestTracker.InverseWorldMatrix : Matrix4x4.Identity;
        }

        public void CancelCalibration()
        {
            if (Headset is not null)
                Headset.WorldMatrixChanged -= UpdateRootTransform;

            var h = GetHumanoid();
            if (h is null)
                return;

            h.HeadTarget = HeadTarget;
            h.LeftHandTarget = LeftHandTarget;
            h.RightHandTarget = RightHandTarget;
            h.HipsTarget = HipsTarget;
            h.LeftFootTarget = LeftFootTarget;
            h.RightFootTarget = RightFootTarget;
            h.ChestTarget = ChestTarget;
            h.LeftElbowTarget = LeftElbowTarget;
            h.RightElbowTarget = RightElbowTarget;
            h.LeftKneeTarget = LeftKneeTarget;
            h.RightKneeTarget = RightKneeTarget;
            h.SolveIK = true;
        }
    }
}
