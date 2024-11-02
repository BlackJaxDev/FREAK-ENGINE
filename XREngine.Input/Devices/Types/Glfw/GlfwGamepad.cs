using Silk.NET.Input;

namespace XREngine.Input.Devices.Glfw
{
    public class GlfwGamepad : BaseGamePad
    {
        private readonly IGamepad _gamepad;

        public GlfwGamepad(IGamepad gamepad) : base(gamepad.Index)
        {
            _gamepad = gamepad;
            _gamepad.ButtonDown += ButtonDown;
            _gamepad.ButtonUp += ButtonUp;
            _gamepad.ThumbstickMoved += ThumbstickMoved;
            _gamepad.TriggerMoved += TriggerMoved;
        }

        private void TriggerMoved(IGamepad gamepad, Trigger trigger)
        {
            
        }

        private void ThumbstickMoved(IGamepad gamepad, Thumbstick thumbstick)
        {

        }

        private void ButtonUp(IGamepad gamepad, Button button)
        {

        }

        private void ButtonDown(IGamepad gamepad, Button button)
        {

        }

        public override void Vibrate(float lowFreq, float highFreq)
        {
            if (_gamepad.VibrationMotors.Count <= 0)
                return;

            _gamepad.VibrationMotors[0].Speed = lowFreq;

            if (_gamepad.VibrationMotors.Count <= 1)
                return;

            _gamepad.VibrationMotors[1].Speed = highFreq;
        }

        protected override List<bool> AxesExist(IEnumerable<EGamePadAxis> axes)
        {
            List<bool> exists = [];
            foreach (EGamePadAxis axis in axes)
                exists.Add(AxisExists(axis));
            return exists;
        }

        protected override bool AxisExists(EGamePadAxis axis) => axis switch
        {
            EGamePadAxis.LeftTrigger => _gamepad.Triggers.Count > 0,
            EGamePadAxis.RightTrigger => _gamepad.Triggers.Count > 1,
            EGamePadAxis.LeftThumbstickX or EGamePadAxis.LeftThumbstickY => _gamepad.Thumbsticks.Count > 0,
            EGamePadAxis.RightThumbstickX or EGamePadAxis.RightThumbstickY => _gamepad.Thumbsticks.Count > 1,
            _ => false,
        };

        protected override bool ButtonExists(EGamePadButton button) => button switch
        {
            EGamePadButton.DPadUp => _gamepad.Buttons.Any(x => x.Name == ButtonName.DPadUp),
            EGamePadButton.DPadDown => _gamepad.Buttons.Any(x => x.Name == ButtonName.DPadDown),
            EGamePadButton.DPadLeft => _gamepad.Buttons.Any(x => x.Name == ButtonName.DPadLeft),
            EGamePadButton.DPadRight => _gamepad.Buttons.Any(x => x.Name == ButtonName.DPadRight),
            EGamePadButton.FaceUp => _gamepad.Buttons.Any(x => x.Name == ButtonName.Y),
            EGamePadButton.FaceDown => _gamepad.Buttons.Any(x => x.Name == ButtonName.A),
            EGamePadButton.FaceLeft => _gamepad.Buttons.Any(x => x.Name == ButtonName.X),
            EGamePadButton.FaceRight => _gamepad.Buttons.Any(x => x.Name == ButtonName.B),
            EGamePadButton.LeftStick => _gamepad.Buttons.Any(x => x.Name == ButtonName.LeftStick),
            EGamePadButton.RightStick => _gamepad.Buttons.Any(x => x.Name == ButtonName.RightStick),
            EGamePadButton.SpecialLeft => _gamepad.Buttons.Any(x => x.Name == ButtonName.Home),
            EGamePadButton.SpecialRight => _gamepad.Buttons.Any(x => x.Name == ButtonName.Start),
            EGamePadButton.LeftBumper => _gamepad.Buttons.Any(x => x.Name == ButtonName.LeftBumper),
            EGamePadButton.RightBumper => _gamepad.Buttons.Any(x => x.Name == ButtonName.RightBumper),
            _ => false,
        };

        protected override List<bool> ButtonsExist(IEnumerable<EGamePadButton> buttons)
        {
            List<bool> exists = [];
            foreach (EGamePadButton button in buttons)
                exists.Add(ButtonExists(button));
            return exists;
        }

        public override void TickStates(float delta)
        {

        }
    }
}