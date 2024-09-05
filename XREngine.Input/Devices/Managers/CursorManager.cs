using System.Drawing;

namespace XREngine.Input.Devices
{
    public delegate void DelCursorUpdate(float x, float y);
    [Serializable]
    public class CursorManager : InputManager
    {
        /// <summary>
        /// Determines if the mouse cursor will jump to the other side of the Cursor.Clip rectangle.
        /// Affects all cursors.
        /// </summary>
        public static bool GlobalWrapCursorWithinClip { get; set; } = false;
        /// <summary>
        /// Determines if the mouse cursor will jump to the other side of the Cursor.Clip rectangle.
        /// Affects only this cursor.
        /// </summary>
        public bool WrapCursorWithinClip { get; set; } = false;

        private float _lastX, _lastY;

        //relative, absolute, unbounded
        private readonly List<DelCursorUpdate?>?[] _onCursorUpdate = new List<DelCursorUpdate?>?[3];

        internal void Tick(float xPos, float yPos, float delta)
        {
            OnUnbounded(xPos, yPos);

            //Point absPt = Cursor.Position;
            //Rectangle bounds = Cursor.Clip;
            //float relX, relY;

            //if (GlobalWrapCursorWithinClip || WrapCursorWithinClip)
            //{
            //    absPt = WrapCursor(absPt, bounds, out relX, out relY);
            //    Cursor.Position = absPt;
            //}
            //else
            //{
            //    relX = absPt.X - _lastX;
            //    relY = _lastY - absPt.Y;
            //}

            //_lastX = absPt.X;
            //_lastY = absPt.Y;
            //OnRelative(relX, relY);

            //RenderContext pnl = RenderContext.Hovered;
            //if (pnl != null)
            //    absPt = pnl.PointToClient(absPt);

            //xPos = absPt.X;
            //yPos = absPt.Y;
            //OnAbsolute(xPos, yPos);
        }

        public void Register(DelCursorUpdate func, EMouseMoveType type, bool unregister)
        {
            int index = (int)type;
            if (unregister)
            {
                List<DelCursorUpdate?>? list = _onCursorUpdate[index];
                if (list is null)
                    return;

                list.Remove(func);
                if (list.Count == 0)
                    _onCursorUpdate[index] = null;
            }
            else
            {
                if (_onCursorUpdate[index] is null)
                    _onCursorUpdate[index] = [func];
                else
                    _onCursorUpdate[index]?.Add(func);
            }
        }
        private void OnAbsolute(float x, float y)
            => PerformAction(EMouseMoveType.Absolute, x, y);
        private void OnRelative(float x, float y)
            => PerformAction(EMouseMoveType.Relative, x, y);
        private void OnUnbounded(float x, float y)
            => PerformAction(EMouseMoveType.Unbounded, x, y);
        protected void PerformAction(EMouseMoveType type, float x, float y)
        {
            int index = (int)type;
            ExecuteList(x, y, _onCursorUpdate[index]);
            //Engine.DebugPrint(_name + ": " + type.ToString());
        }

        private static void ExecuteList(float x, float y, List<DelCursorUpdate?>? list)
        {
            if (list is null)
                return;

            try
            {
                foreach (var action in list)
                    action?.Invoke(x, y);
            }
            catch (Exception)
            {
                //Output.LogException(e);
            }
        }

        private Point WrapCursor(Point absPt, Rectangle bounds, out float relX, out float relY)
        {
            //Wrap the X-coord of the cursor
            if (absPt.X >= bounds.Right - 1)
            {
                while (absPt.X >= bounds.Right - 1)
                    absPt.X -= bounds.Width;

                absPt.X += 1;
                relX = (absPt.X - bounds.Left) + (bounds.Right - 1 - _lastX);
            }
            else if (absPt.X <= bounds.Left)
            {
                while (absPt.X <= bounds.Left)
                    absPt.X += bounds.Width;

                absPt.X -= 1;
                relX = (absPt.X - (bounds.Right - 1)) + (bounds.Left - _lastX);
            }
            else
            {
                relX = absPt.X - _lastX;
            }

            //Wrap the Y-coord of the cursor
            if (absPt.Y >= bounds.Bottom - 1)
            {
                while (absPt.Y >= bounds.Bottom - 1)
                    absPt.Y -= bounds.Height;

                absPt.Y += 1;
                relY = (absPt.Y - bounds.Top) + (bounds.Bottom - 1 - _lastY);
            }
            else if (absPt.Y <= bounds.Top)
            {
                while (absPt.Y <= bounds.Top)
                    absPt.Y += bounds.Height;

                absPt.Y -= 1;
                relY = (absPt.Y - (bounds.Bottom - 1)) + (bounds.Top - _lastY);
            }
            else
            {
                relY = _lastY - absPt.Y;
            }

            return absPt;
        }
    }
}
