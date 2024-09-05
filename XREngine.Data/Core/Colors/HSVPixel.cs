using System.Drawing;
using System.Runtime.InteropServices;

namespace XREngine.Data.Colors
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct HSVPixel(ushort h, byte s, byte v)
    {
        public ushort H = h;
        public byte S = s, V = v;

        public static explicit operator HSVPixel(ARGBPixel p)
        {
            HSVPixel outp;

            int min = Math.Min(Math.Min(p.R, p.G), p.B);
            int max = Math.Max(Math.Max(p.R, p.G), p.B);
            int diff = max - min;

            if (diff == 0)
            {
                outp.H = 0;
                outp.S = 0;
            }
            else
            {
                if (max == p.R)
                    outp.H = (ushort)((60 * ((float)(p.G - p.B) / diff) + 360) % 360);
                else if (max == p.G)
                    outp.H = (ushort)(60 * ((float)(p.B - p.R) / diff) + 120);
                else
                    outp.H = (ushort)(60 * ((float)(p.R - p.G) / diff) + 240);

                if (max == 0)
                    outp.S = 0;
                else
                    outp.S = (byte)(diff * 100 / max);
            }

            outp.V = (byte)(max * 100 / 255);

            return outp;
        }
        public static explicit operator ARGBPixel(HSVPixel pixel)
        {
            ARGBPixel newPixel;

            byte v = (byte)(pixel.V * 255 / 100);
            if (pixel.S == 0)
                newPixel = new ARGBPixel(255, v, v, v);
            else
            {
                int h = (pixel.H / 60) % 6;
                float f = (pixel.H / 60.0f) - (pixel.H / 60);

                byte p = (byte)(pixel.V * (100 - pixel.S) * 255 / 10000);
                byte q = (byte)(pixel.V * (100 - (int)(f * pixel.S)) * 255 / 10000);
                byte t = (byte)(pixel.V * (100 - (int)((1.0f - f) * pixel.S)) * 255 / 10000);

                newPixel = h switch
                {
                    0 => new ARGBPixel(255, v, t, p),
                    1 => new ARGBPixel(255, q, v, p),
                    2 => new ARGBPixel(255, p, v, t),
                    3 => new ARGBPixel(255, p, q, v),
                    4 => new ARGBPixel(255, t, p, v),
                    _ => new ARGBPixel(255, v, p, q),
                };
            }
            return newPixel;
        }
        public static explicit operator Color(HSVPixel p)
        {
            ARGBPixel np = (ARGBPixel)p;
            return Color.FromArgb(np);
        }
        public static explicit operator HSVPixel(Color c)
        {
            return (HSVPixel)(ARGBPixel)c;
        }
    }
}
