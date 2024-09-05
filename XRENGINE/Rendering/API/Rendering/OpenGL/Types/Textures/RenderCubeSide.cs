using ImageMagick;
using XREngine.Data.Core;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class RenderCubeSide : XRBase, IDisposable
    {
        private int _width;
        private int _height;
        private MagickImage? _map;
        private EPixelFormat _pixelFormat;
        private EPixelType _pixelType;
        private EPixelInternalFormat _internalFormat;

        public RenderCubeSide(MagickImage map)
        {
            Map = map;
            Width = map.Width;
            Height = map.Height;
        }
        public RenderCubeSide(int width, int height, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
        {
            InternalFormat = internalFormat;
            PixelFormat = format;
            PixelType = type;
            Width = width;
            Height = height;
            Map = null;
        }

        public int Width
        {
            get => _width;
            set => SetField(ref _width, value);
        }
        public int Height
        {
            get => _height;
            set => SetField(ref _height, value);
        }
        public MagickImage? Map
        {
            get => _map;
            set => SetField(ref _map, value);
        }
        public EPixelFormat PixelFormat
        {
            get => _pixelFormat;
            set => SetField(ref _pixelFormat, value);
        }
        public EPixelType PixelType
        {
            get => _pixelType;
            set => SetField(ref _pixelType, value);
        }
        public EPixelInternalFormat InternalFormat
        {
            get => _internalFormat;
            set => SetField(ref _internalFormat, value);
        }

        public static implicit operator RenderCubeSide(MagickImage bitmap) => new(bitmap);

        public void Dispose()
        {
            Map?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
