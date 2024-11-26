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
        public VRControllerTransform() { MarkLocalModified(); }
        public VRControllerTransform(TransformBase parent)
            : base(parent) { MarkLocalModified(); }

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
            MarkLocalModified();
            var controller = Controller;
            return controller is null
                ? Matrix4x4.Identity
                : Matrix4x4.CreateFromQuaternion(controller.Rotation) * Matrix4x4.CreateTranslation(controller.Position);
        }

    }
}
