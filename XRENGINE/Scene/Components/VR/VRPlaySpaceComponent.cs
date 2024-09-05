//using XREngine.Data.Rendering;
//using XREngine.Rendering;
//using XREngine.Scene.Transforms;

//namespace XREngine.Data.Components
//{
//    public class VRPlaySpaceComponent : XRComponent
//    {
//        public VRPlaySpaceComponent()
//        {
//            LeftEye = new XRCamera();
//            RightEye = new XRCamera();

//            if (EngineVR.Devices != null)
//                for (int i = 0; i < EngineVR.Devices.Length; ++i)
//                    EngineVR_DeviceSet(i);

//            EngineVR.PostDeviceSet += EngineVR_DeviceSet;
//        }

//        public Dictionary<ETrackedDeviceClass, List<VRDeviceComponent>> Devices { get; }
//            = new Dictionary<ETrackedDeviceClass, List<VRDeviceComponent>>();

//        private void EngineVR_DeviceSet(int index)
//        {
//            var device = EngineVR.Devices[index];
//            if (device is null)
//                return;

//            VRDeviceComponent comp = new VRDeviceComponent()
//            {
//                AllowRemoval = false,
//                DeviceIndex = index
//            };

//            var dclass = device.Class;
//            if (Devices.ContainsKey(dclass))
//                Devices[dclass].Add(comp);
//            else
//                Devices.Add(dclass, new List<VRDeviceComponent>() { comp });

//            switch (dclass)
//            {
//                case ETrackedDeviceClass.HMD:
//                    {
//                        comp.ChildSockets.Add(LeftEye);
//                        comp.ChildSockets.Add(RightEye);
//                        HMD = comp;
//                        Engine.Out("Created VR HMD component.");
//                    }
//                    break;
//                case ETrackedDeviceClass.Controller:
//                    {
//                        switch (device.ControllerType)
//                        {
//                            default:
//                            case ETrackedControllerRole.Invalid:
//                                break;

//                            case ETrackedControllerRole.LeftHand:
//                                LeftHand = comp;
//                                Engine.Out("Created VR left hand component.");
//                                break;

//                            case ETrackedControllerRole.RightHand:
//                                RightHand = comp;
//                                Engine.Out("Created VR right hand component.");
//                                break;
//                        }
//                    }
//                    break;
//                case ETrackedDeviceClass.GenericTracker:
//                    _trackers.Add(comp);
//                    Engine.Out("Created VR tracker component.");
//                    break;
//            }

//            ChildSockets.Add(comp);
//        }

//        public VRDeviceComponent HMD { get; private set; }
//        public XRCamera LeftEye { get; private set; }
//        public XRCamera RightEye { get; private set; }
//        public Transform LeftEyeTransform { get; private set; }
//        public Transform RightEyeTransform { get; private set; }
//        public VRDeviceComponent LeftHand { get; private set; }
//        public VRDeviceComponent RightHand { get; private set; }

//        private readonly List<VRDeviceComponent> _trackers = new List<VRDeviceComponent>();
//        public IReadOnlyList<VRDeviceComponent> Trackers => _trackers;

//        protected internal override void OnOriginRebased(Vector3 newOrigin)
//        {

//        }
//    }
//}
