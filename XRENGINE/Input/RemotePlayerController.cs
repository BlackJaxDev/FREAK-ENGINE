using XREngine.Input.Devices;

namespace XREngine.Input
{
    //TODO: handle receiving controller input packets from the server
    public class RemotePlayerController(int serverPlayerIndex) : PlayerController<ServerInputInterface>(new ServerInputInterface(serverPlayerIndex))
    {
        //public override PawnComponent? ControlledPawn
        //{
        //    get => base.ControlledPawn;
        //    set
        //    {
        //        if (_controlledPawn == value)
        //            return;

        //        if (_controlledPawn != null)
        //        {
        //            //_controlledPawn.OnUnPossessing();
        //            _input.TryUnregisterInput();

        //            _input.InputRegistration -= _controlledPawn.RegisterInput;
        //            if (_controlledPawn != _controlledPawn.HUD)
        //                _input.InputRegistration -= _controlledPawn.HUD.RegisterInput;
        //        }

        //        _controlledPawn = value;

        //        if (_controlledPawn is null && _pawnPossessionQueue != null && _pawnPossessionQueue.Count != 0)
        //            _controlledPawn = _pawnPossessionQueue.Dequeue();

        //        //Engine.PrintLine("Assigned new controlled pawn to Player " + _serverPlayerIndex + ": " + (_controlledPawn is null ? "null" : _controlledPawn.GetType().GetFriendlyName()));

        //        if (_controlledPawn != null)
        //        {
        //            _input.InputRegistration += _controlledPawn.RegisterInput;
        //            if (_controlledPawn != _controlledPawn.HUD)
        //                _input.InputRegistration += _controlledPawn.HUD.RegisterInput;

        //            //_controlledPawn.OnPossessed(this);
        //            _input.TryRegisterInput();
        //        }
        //    }
        //}

        protected override void RegisterInput(InputInterface input)
        {
            //input.RegisterButtonEvent(EKey.Escape, ButtonInputType.Pressed, OnTogglePause);
            //input.RegisterButtonEvent(GamePadButton.SpecialRight, ButtonInputType.Pressed, OnTogglePause);
            //base.RegisterInput(input);
        }

        //internal void Destroy()
        //{
        //    UnlinkControlledPawn();
        //    _input.InputRegistration -= RegisterInput;
        //}
        //internal override void UnlinkControlledPawn()
        //{
        //    _pawnPossessionQueue?.Clear();
        //    ControlledPawn = null;
        //}
    }
}
