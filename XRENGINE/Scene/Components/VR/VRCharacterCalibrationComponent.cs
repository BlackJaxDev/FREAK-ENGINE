using Extensions;
using OpenVR.NET.Devices;
using System.Numerics;
using XREngine.Components;
using XREngine.Components.Scene.Mesh;
using XREngine.Data.Components.Scene;
using XREngine.Rendering.Models;
using XREngine.Scene.Components.Animation;
using XREngine.Scene.Transforms;

namespace XREngine.Scene.Components.VR
{
    public class VRCharacterCalibrationComponent : XRComponent
    {
        private float _calibrationRadius = 0.3f;
        /// <summary>
        /// The maximum distance from a tracker to a bone for it to be considered a match during calibration.
        /// </summary>
        public float CalibrationRadius
        {
            get => _calibrationRadius;
            set => SetField(ref _calibrationRadius, value);
        }

        private Matrix4x4 _leftControllerOffset = Matrix4x4.Identity;
        /// <summary>
        /// The manually-set offset from the left controller to the left hand bone.
        /// </summary>
        public Matrix4x4 LeftControllerOffset
        {
            get => _leftControllerOffset;
            set => SetField(ref _leftControllerOffset, value);
        }

        private Matrix4x4 _rightControllerOffset = Matrix4x4.Identity;
        /// <summary>
        /// The manually-set offset from the right controller to the right hand bone.
        /// </summary>
        public Matrix4x4 RightControllerOffset
        {
            get => _rightControllerOffset;
            set => SetField(ref _rightControllerOffset, value);
        }

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

        private ModelComponent? _eyesModel;
        /// <summary>
        /// Used to calculate floor-to-eye height.
        /// Eye meshes should be rigged to bones that contain the word "eye" in their name.
        /// Or you can set the bone names manually with EyeLBoneName and EyeRBoneName.
        /// </summary>
        public ModelComponent? EyesModel
        {
            get => _eyesModel;
            set => SetField(ref _eyesModel, value);
        }

        private string? _eyeLBoneName;
        public string? EyeLBoneName
        {
            get => _eyeLBoneName;
            set => SetField(ref _eyeLBoneName, value);
        }

        private string? _eyeRBoneName;
        public string? EyeRBoneName
        {
            get => _eyeRBoneName;
            set => SetField(ref _eyeRBoneName, value);
        }

        private Vector3 _eyeOffsetFromHead = Vector3.Zero;
        public Vector3 EyeOffsetFromHead
        {
            get => _eyeOffsetFromHead;
            set => SetField(ref _eyeOffsetFromHead, value);
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

            EyeOffsetFromHead = CalculateEyeOffsetFromHead();

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

        /// <summary>
        /// Calculates the average position of all vertices rigged to bones that contain the word "eye" in their name and returns the difference from the head bone.
        /// </summary>
        /// <returns></returns>
        private Vector3 CalculateEyeOffsetFromHead()
        {
            if (EyesModel is null)
                return Vector3.Zero;

            var h = GetHumanoid();
            if (h is null)
                return Vector3.Zero;

            var headNode = h.Head.Node;
            if (headNode is null)
                return Vector3.Zero;

            //Collect all vertices rigged to bones that contain the word "eye"
            EventList<SubMesh>? meshes = EyesModel.Model?.Meshes; //TODO: use submeshes, or renderable meshes?
            if (meshes is null || meshes.Count == 0)
                return Vector3.Zero;

            var lod = meshes[0].LODs.FirstOrDefault(); //TODO: verify first is the highest LOD
            if (lod is null)
                return Vector3.Zero;

            var bones = lod.Mesh?.UtilizedBones;
            if (bones is null || bones.Length == 0)
                return Vector3.Zero;

            if (!SumEyeVertexPositions(lod, out Vector3 eyePosWorldAvg) && 
                !SumEyeBonePositions(bones, out eyePosWorldAvg))
                return Vector3.Zero;

            return eyePosWorldAvg - headNode.Transform.WorldTranslation;
        }

        private bool SumEyeBonePositions((TransformBase tfm, Matrix4x4 invBindWorldMtx)[] bones, out Vector3 eyePosWorldAvg)
        {
            int counted = 0;
            eyePosWorldAvg = Vector3.Zero;

            foreach (var (tfm, invBindWorldMtx) in bones)
            {
                if (!IsEyeBone(tfm))
                    continue;

                eyePosWorldAvg += tfm.WorldTranslation;
                counted++;
            }

            bool any = counted > 0;
            if (any)
                eyePosWorldAvg /= counted;
            return any;
        }

        // Helper method for atomic addition on float
        private static float AtomicAdd(ref float target, float value)
        {
            float initialValue, computedValue;
            do
            {
                initialValue = target;
                computedValue = initialValue + value;
            }
            while (Interlocked.CompareExchange(ref target, computedValue, initialValue) != initialValue);
            return computedValue;
        }

        private bool SumEyeVertexPositions(SubMeshLOD? lod, out Vector3 eyePosWorldAvg)
        {
            eyePosWorldAvg = Vector3.Zero;
            if (lod?.Mesh?.Vertices is null)
                return false;

            float sumX = 0f, sumY = 0f, sumZ = 0f;
            int counted = 0;

            Parallel.ForEach(lod.Mesh.Vertices, vertex =>
            {
                var weights = vertex.Weights;
                if (weights is null)
                    return;

                bool hasEyeBone = weights.Any(w => IsEyeBone(w.Key));
                if (!hasEyeBone)
                    return;

                Vector3 pos = vertex.GetWorldPosition();
                AtomicAdd(ref sumX, pos.X);
                AtomicAdd(ref sumY, pos.Y);
                AtomicAdd(ref sumZ, pos.Z);
                Interlocked.Increment(ref counted);
            });

            eyePosWorldAvg = new Vector3(sumX, sumY, sumZ);
            bool any = counted > 0;
            if (any)
                eyePosWorldAvg /= counted;
            return any;
        }

        private bool IsEyeBone(TransformBase tfm)
        {
            string? name = tfm.Name;
            if (name is null)
                return false;

            if (EyeLBoneName is not null && name.Contains(EyeLBoneName, StringComparison.OrdinalIgnoreCase))
                return true;

            if (EyeRBoneName is not null && name.Contains(EyeRBoneName, StringComparison.OrdinalIgnoreCase))
                return true;

            return name.Contains("eye", StringComparison.OrdinalIgnoreCase);
        }

        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();

            var h = GetHumanoid();
            if (h is null)
                return;

            h.ClearIKTargets();
        }

        private void UpdateRootTransform(TransformBase headsetEyeTfm)
        {
            var h = GetHumanoid();
            if (h is null)
                return;
            var headNode = h.Head.Node;
            if (headNode is null)
                return;
            var rootNode = h.SceneNode;
            var rootToEye = headNode.Transform.WorldTranslation + EyeOffsetFromHead - rootNode.Transform.WorldTranslation;
            Matrix4x4 rootPos = Matrix4x4.CreateTranslation(headsetEyeTfm.WorldTranslation - rootToEye);
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
            rootNode.Transform.DeriveWorldMatrix(Matrix4x4.CreateRotationY(yaw) * rootPos);
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

            //Tell the humanoid to stop solving IK while we calibrate

            //Root transform should follow the headset
            Headset.WorldMatrixChanged += UpdateRootTransform;

            //Save the current targets before they're cleared
            LastHeadTarget = h.HeadTarget;
            LastHipsTarget = h.HipsTarget;
            LastLeftHandTarget = h.LeftHandTarget;
            LastRightHandTarget = h.RightHandTarget;
            LastLeftFootTarget = h.LeftFootTarget;
            LastRightFootTarget = h.RightFootTarget;
            LastChestTarget = h.ChestTarget;
            LastLeftElbowTarget = h.LeftElbowTarget;
            LastRightElbowTarget = h.RightElbowTarget;
            LastLeftKneeTarget = h.LeftKneeTarget;
            LastRightKneeTarget = h.RightKneeTarget;

            //Stop solving
            h.SolveIK = false;

            //Clear all targets
            h.ClearIKTargets();

            //Reset the pose to T-pose
            h.ResetPose();

            return true;
        }

        #region Last state for canceling calibration
        public (TransformBase? tfm, Matrix4x4 offset) LastHeadTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) LastHipsTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LastLeftHandTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) LastRightHandTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LastLeftFootTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) LastRightFootTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LastLeftElbowTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) LastRightElbowTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LastLeftKneeTarget { get; set; } = (null, Matrix4x4.Identity);
        public (TransformBase? tfm, Matrix4x4 offset) LastRightKneeTarget { get; set; } = (null, Matrix4x4.Identity);

        public (TransformBase? tfm, Matrix4x4 offset) LastChestTarget { get; set; } = (null, Matrix4x4.Identity);
        #endregion

        public void EndCalibration()
        {
            if (Headset is not null)
                Headset.WorldMatrixChanged -= UpdateRootTransform;

            var h = GetHumanoid();
            if (h is null)
                return;

            h.HeadTarget = (Headset, Matrix4x4.CreateTranslation(-EyeOffsetFromHead));
            h.LeftHandTarget = (LeftController, LeftControllerOffset);
            h.RightHandTarget = (RightController, RightControllerOffset);

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

            h.HeadTarget = LastHeadTarget;
            h.LeftHandTarget = LastLeftHandTarget;
            h.RightHandTarget = LastRightHandTarget;
            h.HipsTarget = LastHipsTarget;
            h.LeftFootTarget = LastLeftFootTarget;
            h.RightFootTarget = LastRightFootTarget;
            h.ChestTarget = LastChestTarget;
            h.LeftElbowTarget = LastLeftElbowTarget;
            h.RightElbowTarget = LastRightElbowTarget;
            h.LeftKneeTarget = LastLeftKneeTarget;
            h.RightKneeTarget = LastRightKneeTarget;
            h.SolveIK = true;
        }
    }
}
