using Silk.NET.GLFW;
using Silk.NET.Input;
using System.Numerics;
using MouseButton = Silk.NET.Input.MouseButton;

namespace XREngine.Input.Devices.Glfw
{
    [Serializable]
    public class GlfwMouse : BaseMouse
    {
        private readonly IMouse _mouse;

        public GlfwMouse(IMouse mouse) : base(mouse.Index)
        {
            _mouse = mouse;
            _mouse.MouseMove += MouseMove;
            _mouse.MouseUp += MouseUp;
            _mouse.MouseDown += MouseDown;
            _mouse.Scroll += Scroll;
            _mouse.Click += Click;
            _mouse.DoubleClick += DoubleClick;
        }
        ~GlfwMouse()
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
            _relative += vector;
        }

        public override void SetCursorPosition(float x, float y)
        {
            _mouse.Position = new Vector2(x, y);
        }

        private Vector2 _relative = Vector2.Zero;


        public override void TickStates(float delta)
        {
            _cursor.TickRelative(_relative.X, _relative.Y);
            _relative = Vector2.Zero;
            _cursor.TickAbsolute(_mouse.Position.X, _mouse.Position.Y);
            _wheel.Tick(_mouse.ScrollWheels[0].Y, delta);
            LeftClick?.Tick(_mouse.IsButtonPressed(MouseButton.Left), delta);
            RightClick?.Tick(_mouse.IsButtonPressed(MouseButton.Right), delta);
            MiddleClick?.Tick(_mouse.IsButtonPressed(MouseButton.Middle), delta);
        }
    }
}
