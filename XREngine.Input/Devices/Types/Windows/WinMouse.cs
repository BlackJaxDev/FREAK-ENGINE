namespace XREngine.Input.Devices.Windows
{
    [Serializable]
    public class WinMouse : BaseMouse
    {
        public WinMouse(int index) : base(index) { }
        
        public override void SetCursorPosition(float x, float y)
        {
            _cursor.Tick(x, y, 0.0f);
            //Cursor.Position = new System.Drawing.Point((int)x, (int)y);
        }

        protected override void TickStates(float delta)
        {

        }
    }
}
