namespace XREngine.Input.Devices
{
    public class ServerInputInterface : InputInterface
    {
        public override bool HideCursor { get; set; }

        public ServerInputInterface(int serverPlayerIndex) : base(serverPlayerIndex)
        {

        }

        public override bool GetAxisState(EGamePadAxis axis, EButtonInputType type)
        {
            throw new NotImplementedException();
        }

        public override float GetAxisValue(EGamePadAxis axis)
        {
            throw new NotImplementedException();
        }

        public override bool GetButtonState(EGamePadButton button, EButtonInputType type)
        {
            throw new NotImplementedException();
        }

        public override bool GetKeyState(EKey key, EButtonInputType type)
        {
            throw new NotImplementedException();
        }

        public override bool GetMouseButtonState(EMouseButton button, EButtonInputType type)
        {
            throw new NotImplementedException();
        }

        public override void RegisterAxisButtonEvent(EGamePadAxis button, EButtonInputType type, Action func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterAxisButtonEventAction(string actionName, Action func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterAxisButtonPressed(EGamePadAxis axis, DelButtonState func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterAxisButtonPressedAction(string actionName, DelButtonState func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterAxisUpdate(EGamePadAxis axis, DelAxisValue func, bool continuousUpdate)
        {
            throw new NotImplementedException();
        }

        public override void RegisterAxisUpdateAction(string actionName, DelAxisValue func, bool continuousUpdate)
        {
            throw new NotImplementedException();
        }

        public override void RegisterMouseButtonEvent(EMouseButton button, EButtonInputType type, Action func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterButtonEvent(EGamePadButton button, EButtonInputType type, Action func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterButtonEventAction(string actionName, Action func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterMouseButtonContinuousState(EMouseButton button, DelButtonState func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterButtonPressed(EGamePadButton button, DelButtonState func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterButtonPressedAction(string actionName, DelButtonState func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterKeyEvent(EKey button, EButtonInputType type, Action func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterKeyStateChange(EKey button, DelButtonState func)
        {
            throw new NotImplementedException();
        }

        public override void RegisterMouseMove(DelCursorUpdate func, EMouseMoveType type)
        {
            throw new NotImplementedException();
        }

        public override void RegisterMouseScroll(DelMouseScroll func)
        {
            throw new NotImplementedException();
        }

        public override void TryRegisterInput()
        {
            throw new NotImplementedException();
        }

        public override void TryUnregisterInput()
        {
            throw new NotImplementedException();
        }
    }
}
