﻿using Silk.NET.Input;
using System.Diagnostics;
using System.Numerics;
using MouseButton = Silk.NET.Input.MouseButton;

namespace XREngine.Input.Devices.Glfw
{
    [Serializable]
    public class GlfwMouse : BaseMouse
    {
        private readonly IMouse _mouse;

        public override Vector2 CursorPosition
        {
            get => _mouse.Position;
            set => _mouse.Position = value;
        }
        private float _lastScroll = 0.0f;

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

        private void DoubleClick(IMouse mouse, MouseButton button, Vector2 vector)
        {

        }

        private void Click(IMouse mouse, MouseButton button, Vector2 vector)
        {

        }

        private void Scroll(IMouse mouse, ScrollWheel wheel)
        {
            _lastScroll += wheel.Y;
        }

        private void MouseDown(IMouse mouse, MouseButton button)
        {

        }

        private void MouseUp(IMouse mouse, MouseButton button)
        {

        }

        private void MouseMove(IMouse mouse, Vector2 position)
        {

        }

        public override void TickStates(float delta)
        {
            _cursor.Tick(_mouse.Position.X, _mouse.Position.Y);
            _wheel.Tick(_lastScroll);
            _lastScroll = 0.0f;
            LeftClick?.Tick(_mouse.IsButtonPressed(MouseButton.Left), delta);
            RightClick?.Tick(_mouse.IsButtonPressed(MouseButton.Right), delta);
            MiddleClick?.Tick(_mouse.IsButtonPressed(MouseButton.Middle), delta);
        }
    }
}
