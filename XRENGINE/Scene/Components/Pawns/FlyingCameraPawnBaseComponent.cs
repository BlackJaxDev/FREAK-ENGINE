using Extensions;
using MathNet.Numerics.Random;
using System.ComponentModel;
using XREngine.Input.Devices;

namespace XREngine.Components
{
    public abstract class FlyingCameraPawnBaseComponent : PawnComponent
    {
        protected float
            _incRight = 0.0f,
            _incForward = 0.0f,
            _incUp = 0.0f,
            _incPitch = 0.0f,
            _incYaw = 0.0f;

        public float Yaw
        {
            get => _yaw;
            set => SetField(ref _yaw, value.RemapToRange(-180.0f, 180.0f));
        }
        public float Pitch
        {
            get => _pitch;
            set => SetField(ref _pitch, value.RemapToRange(-180.0f, 180.0f));
        }

        public void SetYawPitch(float yaw, float pitch)
        {
            _yaw = yaw.RemapToRange(-180.0f, 180.0f);
            _pitch = pitch.RemapToRange(-180.0f, 180.0f);
            YawPitchUpdated();
        }

        public void AddYawPitch(float yawDiff, float pitchDiff)
            => SetYawPitch(Yaw + yawDiff, Pitch + pitchDiff);

        protected abstract void YawPitchUpdated();

        public bool ShiftPressed
        {
            get => _shiftPressed;
            private set => SetField(ref _shiftPressed, value);
        }

        public bool CtrlPressed
        {
            get => _ctrlPressed;
            private set => SetField(ref _ctrlPressed, value);
        }

        public bool RightClickPressed
        {
            get => _rightClickPressed;
            private set => SetField(ref _rightClickPressed, value);
        }

        protected bool
            _ctrlPressed = false,
            _shiftPressed = false,
            _rightClickPressed = false;
        private float _yaw;
        private float _pitch;

        [Browsable(false)]
        public bool Rotating => _rightClickPressed && _ctrlPressed;

        [Browsable(false)]
        public bool Translating => _rightClickPressed && !_ctrlPressed;

        [Browsable(false)]
        public bool Moving => Rotating || Translating;

        [Category("Movement")]
        public float ScrollSpeed { get; set; } = 0.7f;

        [Category("Movement")]
        public float MouseRotateSpeed { get; set; } = 0.0075f;

        [Category("Movement")]
        public float MouseTranslateSpeed { get; set; } = 0.01f;

        [Category("Movement")]
        public float GamepadRotateSpeed { get; set; } = 150.0f;

        [Category("Movement")]
        public float GamepadTranslateSpeed { get; set; } = 30.0f;

        [Category("Movement")]
        public float KeyboardTranslateSpeed { get; set; } = 10.0f;

        [Category("Movement")]
        public float KeyboardRotateSpeed { get; set; } = 0.01f;

        public override void RegisterInput(InputInterface input)
        {
            input.RegisterMouseScroll(OnScrolled);
            input.RegisterMouseMove(MouseMove, EMouseMoveType.Relative);

            input.RegisterMouseButtonContinuousState(EMouseButton.RightClick, OnRightClick);

            input.RegisterKeyContinuousState(EKey.A, MoveLeft);
            input.RegisterKeyContinuousState(EKey.W, MoveForward);
            input.RegisterKeyContinuousState(EKey.S, MoveBackward);
            input.RegisterKeyContinuousState(EKey.D, MoveRight);
            input.RegisterKeyContinuousState(EKey.Q, MoveDown);
            input.RegisterKeyContinuousState(EKey.E, MoveUp);

            input.RegisterKeyContinuousState(EKey.Up, PitchUp);
            input.RegisterKeyContinuousState(EKey.Down, PitchDown);
            input.RegisterKeyContinuousState(EKey.Left, YawLeft);
            input.RegisterKeyContinuousState(EKey.Right, YawRight);

            input.RegisterKeyContinuousState(EKey.ControlLeft, OnControl);
            input.RegisterKeyContinuousState(EKey.ControlRight, OnControl);
            input.RegisterKeyContinuousState(EKey.ShiftLeft, OnShift);
            input.RegisterKeyContinuousState(EKey.ShiftRight, OnShift);

            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickX, OnLeftStickX, false);
            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickY, OnLeftStickY, false);
            input.RegisterAxisUpdate(EGamePadAxis.RightThumbstickX, OnRightStickX, false);
            input.RegisterAxisUpdate(EGamePadAxis.RightThumbstickY, OnRightStickY, false);

            input.RegisterButtonPressed(EGamePadButton.RightBumper, MoveUp);
            input.RegisterButtonPressed(EGamePadButton.LeftBumper, MoveDown);
        }

        protected virtual void MoveDown(bool pressed)
            => _incUp += KeyboardTranslateSpeed * (pressed ? -1.0f : 1.0f);
        protected virtual void MoveUp(bool pressed)
            => _incUp += KeyboardTranslateSpeed * (pressed ? 1.0f : -1.0f);
        protected virtual void MoveLeft(bool pressed)
            => _incRight += KeyboardTranslateSpeed * (pressed ? -1.0f : 1.0f);
        protected virtual void MoveRight(bool pressed)
            => _incRight += KeyboardTranslateSpeed * (pressed ? 1.0f : -1.0f);
        protected virtual void MoveBackward(bool pressed)
            => _incForward += KeyboardTranslateSpeed * (pressed ? -1.0f : 1.0f);
        protected virtual void MoveForward(bool pressed)
            => _incForward += KeyboardTranslateSpeed * (pressed ? 1.0f : -1.0f);

        protected virtual void OnLeftStickX(float value)
            => _incRight = value * GamepadTranslateSpeed;
        protected virtual void OnLeftStickY(float value)
            => _incForward = value * GamepadTranslateSpeed;
        protected virtual void OnRightStickX(float value)
            => _incYaw = -value * GamepadRotateSpeed;
        protected virtual void OnRightStickY(float value)
            => _incPitch = value * GamepadRotateSpeed;

        protected virtual void YawRight(bool pressed)
            => _incYaw -= KeyboardRotateSpeed * (pressed ? 1.0f : -1.0f);
        protected virtual void YawLeft(bool pressed)
            => _incYaw += KeyboardRotateSpeed * (pressed ? 1.0f : -1.0f);
        protected virtual void PitchDown(bool pressed)
            => _incPitch -= KeyboardRotateSpeed * (pressed ? 1.0f : -1.0f);
        protected virtual void PitchUp(bool pressed)
            => _incPitch += KeyboardRotateSpeed * (pressed ? 1.0f : -1.0f);

        protected void OnShift(bool pressed)
            => ShiftPressed = pressed;
        private void OnControl(bool pressed)
            => CtrlPressed = pressed;
        protected virtual void OnRightClick(bool pressed)
            => RightClickPressed = pressed;

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            RegisterTick(ETickGroup.Normal, ETickOrder.Input, Tick);
        }
        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
            UnregisterTick(ETickGroup.Normal, ETickOrder.Input, Tick);
        }

        protected abstract void OnScrolled(float diff);
        protected abstract void MouseMove(float x, float y);
        protected abstract void Tick();

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Yaw):
                case nameof(Pitch):
                    YawPitchUpdated();
                    break;
            }
        }

        //Dictionary<ComboModifier, Action<bool>> _combos = new Dictionary<ComboModifier, Action<bool>>();

        //private void ExecuteCombo(EMouseButton button, bool pressed)
        //{
        //    //ComboModifier mod = GetModifier(button, _alt, _ctrl, _shift);
        //    //if (_combos.ContainsKey(mod))
        //    //    _combos[mod](pressed);
        //}

        //private ComboModifier GetModifier(EMouseButton button, bool alt, bool ctrl, bool shift)
        //{
        //    ComboModifier mod = ComboModifier.None;

        //    if (button == EMouseButton.LeftClick)
        //        mod |= ComboModifier.LeftClick;
        //    else if (button == EMouseButton.RightClick)
        //        mod |= ComboModifier.RightClick;
        //    else if (button == EMouseButton.MiddleClick)
        //        mod |= ComboModifier.MiddleClick;

        //    if (_ctrl)
        //        mod |= ComboModifier.Ctrl;
        //    if (_alt)
        //        mod |= ComboModifier.Alt;
        //    if (_shift)
        //        mod |= ComboModifier.Shift;

        //    return mod;
        //}
        //public void SetInputCombo(Action<bool> func, EMouseButton button, bool alt, bool ctrl, bool shift)
        //{
        //    ComboModifier mod = GetModifier(button, alt, ctrl, shift);
        //    if (mod != ComboModifier.None)
        //        if (_combos.ContainsKey(mod))
        //            _combos[mod] = func;
        //        else
        //            _combos.Add(mod, func);
        //}
    }
}
