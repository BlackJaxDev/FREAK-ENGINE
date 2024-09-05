//using Extensions;
//using XREngine.Components.Scene;
//using XREngine.Components.Scene.Transforms;
//using XREngine.Core.Maths;
//using XREngine.Core.Maths.Transforms;
//using XREngine.Rendering.Cameras;

//namespace XREngine.Components
//{
//    public class FlyingCameraPawnLagged : FlyingCameraPawnBase<TranslationRotationLaggedTransform>
//    {
//        public FlyingCameraPawnLagged() : base() { }
//        public FlyingCameraPawnLagged(ELocalPlayerIndex possessor) : base(possessor) { }
//        protected override TranslationRotationLaggedTransform OnConstructRoot()
//        {
//            TranslationRotationLaggedTransform root = new TranslationRotationLaggedTransform();
//            root.ChildSockets.Add(CameraComp = new CameraComponent(new PerspectiveCamera()));
//            return root;
//        }
//        protected override void OnScrolled(bool up)
//            => RootComponent.TranslateRelative(0.0f, 0.0f, up ? ScrollSpeed : -ScrollSpeed);
//        protected override void MouseMove(float x, float y)
//        {
//            if (Rotating)
//            {
//                AddYawPitch(
//                    -x * MouseRotateSpeed,
//                    -y * MouseRotateSpeed);
//            }
//            else if (Translating)
//            {
//                RootComponent.TranslateRelative(
//                    -x * MouseTranslateSpeed,
//                    y * MouseTranslateSpeed,
//                    0.0f);
//            }
//        }
//        public void Pivot(float pitch, float yaw, float distance)
//            => ArcBallRotate(pitch, yaw, RootComponent.DesiredTranslation + RootComponent.GetDesiredForwardDir() * distance);
//        public void ArcBallRotate(float pitch, float yaw, Vector3 focusPoint)
//        {
//            //"Arcball" rotation
//            //All rotation is done within local component space

//            RootComponent.DesiredTranslation = TMath.ArcballTranslation(
//                pitch, yaw, focusPoint,
//                RootComponent.DesiredTranslation,
//                RootComponent.GetDesiredRightDir());

//            AddYawPitch(yaw, pitch);
//        }
//        protected override void Tick(float delta)
//        {
//            bool translate = !(_incRight.IsZero() && _incUp.IsZero() && _incForward.IsZero());
//            bool rotate = !(_incPitch.IsZero() && _incYaw.IsZero());
//            if (translate)
//                RootComponent.TranslateRelative(new Vector3(_incRight, _incUp, -_incForward) * delta);
//            if (rotate)
//                AddYawPitch(_incYaw * delta, _incPitch * delta);
//        }
//        protected override void YawPitchUpdated()
//        {
//            RootComponent.DesiredRotation.SetRotations(Pitch, Yaw, 0.0f);
//        }
//    }
//}
