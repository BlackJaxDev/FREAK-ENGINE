using Extensions;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Core;
using XREngine.Scene.Transforms;

namespace XREngine.Components
{
    [RequireComponents(typeof(CameraComponent))]
    [RequiresTransform(typeof(Transform))]
    public class FlyingCameraPawn : FlyingCameraPawnBase
    {
        protected override void OnScrolled(bool up)
            => TransformAs<Transform>().TranslateRelative(0.0f, 0.0f, up ? ScrollSpeed : -ScrollSpeed);

        protected override void MouseMove(float x, float y)
        {
            if (Rotating)
            {
                AddYawPitch(
                    -x * MouseRotateSpeed,
                    -y * MouseRotateSpeed);
            }
            else if (Translating)
            {
                TransformAs<Transform>().TranslateRelative(
                    -x * MouseTranslateSpeed,
                    -y * MouseTranslateSpeed,
                    0.0f);
            }
        }
        public void Pivot(float pitch, float yaw, float distance)
        {
            var tfm = TransformAs<Transform>();
            ArcBallRotate(pitch, yaw, tfm.Translation + tfm.LocalForward * distance);
        }
        public void ArcBallRotate(float pitch, float yaw, Vector3 focusPoint)
        {
            //"Arcball" rotation
            //All rotation is done within local component space

            var tfm = TransformAs<Transform>();
            tfm.Translation = XRMath.ArcballTranslation(
                pitch, yaw, focusPoint,
                tfm.Translation,
                tfm.LocalRight);

            AddYawPitch(yaw, pitch);
        }
        protected override void Tick()
        {
            IncrementRotation();

            if (!(_incRight.IsZero() && _incUp.IsZero() && _incForward.IsZero()))
                TransformAs<Transform>().TranslateRelative(
                    _incRight * Engine.Delta,
                    _incUp * Engine.Delta,
                    -_incForward * Engine.Delta);
        }

        private void IncrementRotation()
        {
            if (!_incPitch.IsZero())
            {
                if (!_incYaw.IsZero())
                    AddYawPitch(_incYaw, _incPitch);
                else
                    Pitch += _incPitch;
            }
            else if (!_incYaw.IsZero())
                Yaw += _incYaw;
        }

        protected override void YawPitchUpdated()
            => TransformAs<Transform>().Rotation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0.0f);
    }
}
