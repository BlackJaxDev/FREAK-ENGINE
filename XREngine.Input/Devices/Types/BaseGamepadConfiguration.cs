namespace XREngine.Input.Devices
{
    public abstract class BaseGamepadConfiguration
    {
        public BaseGamepadConfiguration()
        {
            ButtonMap = new Dictionary<EGamePadButton, EGamePadButton>();
            AxisMap = new Dictionary<EGamePadAxis, EGamePadAxis>();

            for (int i = 0; i < 14; ++i)
                ButtonMap.Add((EGamePadButton)i, (EGamePadButton)i);

            for (int i = 0; i < 6; ++i)
                AxisMap.Add((EGamePadAxis)i, (EGamePadAxis)i);
        }

        public Dictionary<EGamePadButton, EGamePadButton> ButtonMap { get; set; }
        public Dictionary<EGamePadAxis, EGamePadAxis> AxisMap { get; set; }
    }
}
