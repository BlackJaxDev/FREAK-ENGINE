using OpenVR.NET.Devices;
using System.Drawing;
using System.Numerics;
using Valve.VR;
using XREngine.Components;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Components.Scene
{
    public class VRTrackerCollectionComponent : XRComponent, IRenderable
    {
        public VRTrackerCollectionComponent() 
        {
            RenderedObjects = [new RenderInfo3D(this)];
            _rcDevice.Mesh = TrackerMesh;

            Engine.VRState.Api.DeviceDetected += OnDeviceDetected;
        }

        public Dictionary<int, VrDevice> Trackers { get; } = [];

        private void OnDeviceDetected(VrDevice device)
            => ReverifyTrackedDevices();

        private static void ReverifyTrackedDevices()
        {
            var devices = Engine.VRState.Api.TrackedDevices;
            foreach (var dev in devices)
            {
                if (!dev.IsEnabled)
                    continue;

                var c = Engine.VRState.Api.CVR.GetTrackedDeviceClass(dev.DeviceIndex);
                if (c == ETrackedDeviceClass.GenericTracker)
                {

                }
            }
        }

        static VRTrackerCollectionComponent()
        {
            XRMesh mesh = XRMesh.CreatePoints(Vector3.Zero);

            XRMaterial mat = XRMaterial.CreateUnlitColorMaterialForward(Color.Red);
            mat.RenderOptions = new RenderingParameters { PointSize = 20.0f };

            TrackerMesh = new XRMeshRenderer(mesh, mat);
        }

        private readonly RenderCommandMesh3D _rcDevice = new();
        private static readonly XRMeshRenderer TrackerMesh;
        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);
            _rcDevice.WorldMatrix = Transform.WorldMatrix;
        }

        public RenderInfo[] RenderedObjects { get; }
        public void AddRenderCommands(RenderCommandCollection passes, XRCamera camera)
        {
            passes.Add(_rcDevice);
        }
    }
}
