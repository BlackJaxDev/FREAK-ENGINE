using OpenVR.NET.Devices;
using System.Numerics;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Components.Scene
{
    /// <summary>
    /// The transform for the left or right VR controller.
    /// </summary>
    /// <param name="parent"></param>
    public class VRControllerTransform : TransformBase
    {
        public VRControllerTransform()
        { 
            Engine.VRState.RecalcMatrixOnDraw += VRState_RecalcMatrixOnDraw;
        }

        public VRControllerTransform(TransformBase parent)
            : base(parent)
        {
            Engine.VRState.RecalcMatrixOnDraw += VRState_RecalcMatrixOnDraw;
        }

        private void VRState_RecalcMatrixOnDraw()
        {
            RecalculateMatrices(true);
        }

        private bool _leftHand;
        public bool LeftHand
        {
            get => _leftHand;
            set => SetField(ref _leftHand, value);
        }

        public Controller? Controller => LeftHand 
            ? Engine.VRState.Api.LeftController 
            : Engine.VRState.Api.RightController;

        protected override Matrix4x4 CreateLocalMatrix()
        {
            //MarkLocalModified();
            var controller = Controller;
            return controller is null
                ? Matrix4x4.Identity
                : controller.RenderDeviceToAbsoluteTrackingMatrix;
        }
    }
    /// <summary>
    /// The transform for a VR tracker.
    /// </summary>
    /// <param name="parent"></param>
    public class VRTrackerTransform : TransformBase
    {
        public VRTrackerTransform()
        {
            Engine.VRState.RecalcMatrixOnDraw += VRState_RecalcMatrixOnDraw;
        }
        
        public VRTrackerTransform(TransformBase parent)
            : base(parent)
        {
            Engine.VRState.RecalcMatrixOnDraw += VRState_RecalcMatrixOnDraw;
        }

        private void VRState_RecalcMatrixOnDraw()
        {
            RecalculateMatrices(true);
        }

        private uint? _deviceIndex;
        public uint? DeviceIndex
        {
            get => _deviceIndex;
            set => SetField(ref _deviceIndex, value);
        }

        private VrDevice? _tracker = null;
        public VrDevice? Tracker
        {
            get => _tracker ?? SetFieldReturn(ref _tracker, DeviceIndex is null ? null : Engine.VRState.Api.TrackedDevices.FirstOrDefault(d => d.DeviceIndex == DeviceIndex && Engine.VRState.Api.CVR.GetTrackedDeviceClass(d.DeviceIndex) == Valve.VR.ETrackedDeviceClass.GenericTracker));
            set => SetField(ref _tracker, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Tracker):
                    DeviceIndex = _tracker?.DeviceIndex;
                    break;
            }
        }

        protected override Matrix4x4 CreateLocalMatrix()
        {
            //MarkLocalModified();
            var tracker = Tracker;
            return tracker is null
                ? Matrix4x4.Identity
                : tracker.RenderDeviceToAbsoluteTrackingMatrix;
        }
    }
}
