using Silk.NET.GLFW;
using Silk.NET.Input;
using System.Numerics;

namespace XREngine.Input.Devices.Windows
{
    [Serializable]
    public class SilkNetMouse : BaseMouse
    {
        private readonly IMouse _mouse;

        public SilkNetMouse(IMouse mouse) : base(mouse.Index)
        {
            _mouse = mouse;
            _mouse.MouseMove += MouseMove;
            _mouse.MouseUp += MouseUp;
            _mouse.MouseDown += MouseDown;
            _mouse.Scroll += Scroll;
            _mouse.Click += Click;
            _mouse.DoubleClick += DoubleClick;
        }
        ~SilkNetMouse()
        {
            _mouse.MouseMove -= MouseMove;
            _mouse.MouseUp -= MouseUp;
            _mouse.MouseDown -= MouseDown;
            _mouse.Scroll -= Scroll;
            _mouse.Click -= Click;
            _mouse.DoubleClick -= DoubleClick;
        }

        private void DoubleClick(IMouse mouse, Silk.NET.Input.MouseButton button, Vector2 vector)
        {

        }

        private void Click(IMouse mouse, Silk.NET.Input.MouseButton button, Vector2 vector)
        {

        }

        private void Scroll(IMouse mouse, ScrollWheel wheel)
        {

        }

        private void MouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
        {

        }

        private void MouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
        {

        }

        private void MouseMove(IMouse mouse, Vector2 vector)
        {
            _cursor.MoveRelative(vector.X, vector.Y);
        }

        public override void SetCursorPosition(float x, float y)
        {
            _mouse.Position = new Vector2(x, y);
        }

        protected override void TickStates(float delta)
        {

        }
    }
}
