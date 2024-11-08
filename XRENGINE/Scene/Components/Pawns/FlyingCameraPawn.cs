using Extensions;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Core;
using XREngine.Scene.Transforms;

namespace XREngine.Components
{
    [RequireComponents(typeof(CameraComponent))]
    [RequiresTransform(typeof(Transform))]
    public class FlyingCameraPawnComponent : FlyingCameraPawnBaseComponent
    {
        public float ScrollSpeedModifier { get; set; } = 1.0f;

        protected override void OnScrolled(float diff)
        {
            if (ShiftPressed)
                diff *= ShiftSpeedModifier;

            //if (CtrlPressed)
            //{
            //    if (diff > 0.0f)
            //        ScrollSpeedModifier *= 0.5f;
            //    else if (diff < 0.0f)
            //        ShiftSpeedModifier *= 1.5f;
            //}

            TransformAs<Transform>().TranslateRelative(0.0f, 0.0f, diff * -ScrollSpeed * ScrollSpeedModifier);
        }

        protected override void MouseMove(float x, float y)
        {
            if (Rotating)
                MouseRotate(x, y);

            if (Translating)
            {
                if (ShiftPressed)
                {
                    x *= ShiftSpeedModifier;
                    y *= ShiftSpeedModifier;
                }
                MouseTranslate(x, y);
            }
        }

        private float _shiftSpeedModifier = 3.0f;
        public float ShiftSpeedModifier 
        {
            get => _shiftSpeedModifier; 
            set => SetField(ref _shiftSpeedModifier, value);
        }

        protected virtual void MouseTranslate(float x, float y)
            => TransformAs<Transform>().TranslateRelative(
                -x * MouseTranslateSpeed,
                -y * MouseTranslateSpeed,
                0.0f);

        protected virtual void MouseRotate(float x, float y)
            => AddYawPitch(-x * MouseRotateSpeed, y * MouseRotateSpeed);

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

            if (_incRight.IsZero() && 
                _incUp.IsZero() && 
                _incForward.IsZero())
                return;

            float incRight = _incRight;
            float incUp = _incUp;
            float incForward = _incForward;

            if (ShiftPressed)
            {
                incRight *= ShiftSpeedModifier;
                incUp *= ShiftSpeedModifier;
                incForward *= ShiftSpeedModifier;
            }

            //Don't time dilate user inputs
            float delta = Engine.UndilatedDelta;
            //Vector2 dir = Vector2.Normalize(new(_incRight, _incUp));
            TransformAs<Transform>().TranslateRelative(
                incRight * delta,
                incUp * delta,
                -incForward * delta);
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
