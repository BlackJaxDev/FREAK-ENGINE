﻿using System.Drawing;

namespace Extensions
{
    public static partial class Ext
    {
        public static Rectangle AsIntRect(this RectangleF rect)
            => new((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        public static RectangleF AsFloatRect(this Rectangle rect)
            => new(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
