﻿namespace XREngine.Input.Devices
{
    public enum EInputType
    {
        XInput,
    }
    [Serializable]
    public abstract class InputDevice
    {
        public static IReadOnlyDictionary<EInputDeviceType, List<InputDevice>> CurrentDevices => _currentDevices;
        private static readonly Dictionary<EInputDeviceType, List<InputDevice>> _currentDevices = new()
        {
            { EInputDeviceType.Gamepad, new List<InputDevice>() },
            { EInputDeviceType.Keyboard,new List<InputDevice>() },
            { EInputDeviceType.Mouse, new List<InputDevice>() },
        };

        protected ButtonManager?[] _buttonStates = [];
        protected AxisManager?[] _axisStates = [];

        protected int _index;
        protected bool _isConnected;

        public ConnectedStateChange? ConnectionStateChanged;

        public bool IsConnected => _isConnected;
        public int Index => _index;
        public abstract EInputDeviceType DeviceType { get; }
        public InputInterface? InputInterface { get; internal set; }

        protected InputDevice(int index)
        {
            _index = index;
            ResetStates();
        }
        protected abstract int GetButtonCount();
        protected abstract int GetAxisCount();
        private void ResetStates()
        {
            _buttonStates = new ButtonManager[GetButtonCount()];
            _axisStates = new AxisManager[GetAxisCount()];
        }
        protected abstract void TickStates(float delta);
        /// <summary>
        /// Returns true if connected.
        /// </summary>
        protected bool UpdateConnected(bool isConnected)
        {
            if (_isConnected != isConnected)
            {
                _isConnected = isConnected;
                ConnectionStateChanged?.Invoke(_isConnected);
            }
            return _isConnected;
        }
        public static void RegisterButtonEvent(ButtonManager? m, EButtonInputType type, Action func, bool unregister)
        {
            m?.Register(func, type, unregister);
        }

        protected ButtonManager MakeButtonManager(string name, int index)
        {
            var man = new ButtonManager(index, name);
            man.ActionExecuted += SendButtonAction;
            man.StatePressed += SendButtonPressedState;
            return man;
        }

        protected AxisManager MakeAxisManager(string name, int index)
        {
            var man = new AxisManager(index, name);
            man.ActionExecuted += SendButtonAction;
            man.StatePressed += SendButtonPressedState;
            man.ListExecuted += SendAxisValue;
            return man;
        }

        private bool CanSend() => false;
            //=> Engine.Network != null &&
            //!Engine.Network.IsServer &&
            //InputInterface != null;
            ////&&
            ////Engine.LocalPlayers.IndexInRange(InputInterface.LocalPlayerIndex);
        
        protected void SendButtonAction(int buttonIndex, EButtonInputType type)
        {
            if (!CanSend())
                return;

            //TPacketInput packet = new TPacketInput();
            //packet.Header.PacketType = EPacketType.Input;
            //packet.DeviceType = DeviceType;
            //packet.InputType = EInputType.ButtonAction;
            //packet.InputIndex = (byte)buttonIndex;
            //packet.ListIndex = (byte)listIndex;
            //packet.PlayerIndex = (byte)GetServerIndex();

            //Engine.Network.SendPacket(packet);
        }
        protected void SendButtonPressedState(int buttonIndex, EButtonInputType type, bool pressed)
        {
            if (!CanSend())
                return;

            //TPacketPressedInput packet = new TPacketPressedInput();
            //packet.Header.Header.PacketType = EPacketType.Input;
            //packet.Header.DeviceType = DeviceType;
            //packet.Header.InputType = EInputType.ButtonPressedState;
            //packet.Header.InputIndex = (byte)buttonIndex;
            //packet.Header.ListIndex = (byte)listIndex;
            //packet.Header.PlayerIndex = (byte)GetServerIndex();
            //packet.Pressed = (byte)(pressed ? 1 : 0);

            //Engine.Network.SendPacket(packet);
        }
        protected void SendAxisButtonAction(int axisIndex, int listIndex)
        {
            if (!CanSend())
                return;

            //TPacketInput packet = new TPacketInput();
            //packet.Header.PacketType = EPacketType.Input;
            //packet.DeviceType = DeviceType;
            //packet.InputType = EInputType.AxisButtonAction;
            //packet.InputIndex = (byte)axisIndex;
            //packet.ListIndex = (byte)listIndex;
            //packet.PlayerIndex = (byte)GetServerIndex();

            //Engine.Network.SendPacket(packet);
        }
        protected void SendAxisButtonPressedState(int axisIndex, int listIndex, bool pressed)
        {
            if (!CanSend())
                return;

            //TPacketPressedInput packet = new TPacketPressedInput();
            //packet.Header.Header.PacketType = EPacketType.Input;
            //packet.Header.DeviceType = DeviceType;
            //packet.Header.InputType = EInputType.AxisButtonPressedState;
            //packet.Header.InputIndex = (byte)axisIndex;
            //packet.Header.ListIndex = (byte)listIndex;
            //packet.Header.PlayerIndex = (byte)GetServerIndex();
            //packet.Pressed = (byte)(pressed ? 1 : 0);

            //Engine.Network.SendPacket(packet);
        }
        protected void SendAxisValue(int axisIndex, bool continuous, float value)
        {
            if (!CanSend())
                return;

            //TPacketAxisInput packet = new TPacketAxisInput();
            //packet.Header.Header.PacketType = EPacketType.Input;
            //packet.Header.DeviceType = DeviceType;
            //packet.Header.InputType = EInputType.AxisValue;
            //packet.Header.InputIndex = (byte)axisIndex;
            //packet.Header.ListIndex = (byte)listIndex;
            //packet.Header.PlayerIndex = (byte)GetServerIndex();
            //packet.Value = value;

            //Engine.Network.SendPacket(packet);
        }
    }
}
