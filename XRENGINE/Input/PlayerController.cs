using XREngine.Components;
using XREngine.Input.Devices;
using XREngine.Players;

namespace XREngine.Input
{
    public abstract class PlayerController<T> : PlayerControllerBase where T : InputInterface
    {
        public PlayerController(T input) : base()
        {
            _input = input;
            _input.InputRegistration += RegisterInput;
        }

        private T _input;
        public T Input
        {
            get => _input;
            internal set => SetField(ref _input, value);
        }

        protected override bool OnPropertyChanging<T2>(string? propName, T2 field, T2 @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Input):
                        _input.InputRegistration -= RegisterInput;
                        break;
                }
            }
            return change;
        }

        protected override void OnPropertyChanged<T2>(string? propName, T2 prev, T2 field)
        {
            switch (propName)
            {
                case nameof(Input):
                    _input.InputRegistration += RegisterInput;
                    break;
            }
            base.OnPropertyChanged(propName, prev, field);
        }

        public override PawnComponent? ControlledPawn
        {
            get => base.ControlledPawn;
            set
            {
                var c = base.ControlledPawn;

                if (c == value)
                    return;

                if (c != null)
                    UnregisterController(c);

                base.ControlledPawn = value;

                c = value;

                if (c != null)
                    RegisterController(c);
            }
        }

        protected virtual void RegisterController(PawnComponent c)
        {
            if (Input is null)
                return;

            Input.InputRegistration += c.RegisterInput;
            if (c.HUD != null && c != c.HUD)
                Input.InputRegistration += c.HUD.RegisterInput;

            //c.OnPossessed(this);
            Input.TryRegisterInput();
            
            if (PlayerInfo.LocalIndex is not null)
                Debug.Out($"Local player {PlayerInfo.LocalIndex} gained control of {_controlledPawn}");
            else
                Debug.Out($"Server player {PlayerInfo.ServerIndex} gained control of {_controlledPawn}");
        }

        protected virtual void UnregisterController(PawnComponent c)
        {
            if (Input is null)
                return;

            Input.TryUnregisterInput();
            Input.InputRegistration -= c.RegisterInput;

            if (c.HUD != null && c != c.HUD)
                Input.InputRegistration -= c.HUD.RegisterInput;

            if (PlayerInfo.LocalIndex is not null)
                Debug.Out($"Local player {PlayerInfo.LocalIndex} is releasing control of {_controlledPawn}");
            else
                Debug.Out($"Server player {PlayerInfo.ServerIndex} gained control of {_controlledPawn}");
        }

        protected abstract void RegisterInput(InputInterface input);

        protected override void OnDestroying()
        {
            base.OnDestroying();
            _input.InputRegistration -= RegisterInput;
        }
    }
}
