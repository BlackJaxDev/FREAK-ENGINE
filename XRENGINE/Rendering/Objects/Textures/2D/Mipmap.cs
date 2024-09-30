using ImageMagick;
using XREngine.Data.Core;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class Mipmap : XRBase
    {
        public Mipmap(MagickImage? image) => Image = image;
        public Mipmap() { }

        private MagickImage? _image;
        public MagickImage? Image
        {
            get => _image;
            set => SetField(ref _image, value);
        }

        public static implicit operator Mipmap(MagickImage image)
            => new(image);
        public static implicit operator MagickImage?(Mipmap mipmap)
            => mipmap.Image;

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            if (propName == nameof(Image))
            {
                if (field is not MagickImage image)
                    return;

                XRTexture.GetFormat(image, false, out EPixelInternalFormat internalFormat, out EPixelFormat format, out EPixelType type);
                InternalFormat = internalFormat;
                PixelFormat = format;
                PixelType = type;
            }
        }

        private EPixelType _pixelType = EPixelType.UnsignedByte;
        public EPixelType PixelType
        {
            get => _pixelType;
            set => SetField(ref _pixelType, value);
        }

        private EPixelFormat _pixelFormat = EPixelFormat.Rgba;
        public EPixelFormat PixelFormat
        {
            get => _pixelFormat;
            set => SetField(ref _pixelFormat, value);
        }

        private EPixelInternalFormat _internalFormat = EPixelInternalFormat.Rgba8;
        public EPixelInternalFormat InternalFormat
        {
            get => _internalFormat;
            set => SetField(ref _internalFormat, value);
        }

        public uint Width => Image?.Width ?? 0u;
        public uint Height => Image?.Height ?? 0u;
        public void Resize(uint width, uint height)
            => Image?.Resize(width, height);

        public Mipmap Clone(bool cloneImage)
            => new(cloneImage ? Image?.Clone() as MagickImage : Image)
            {
                InternalFormat = InternalFormat,
                PixelFormat = PixelFormat,
                PixelType = PixelType
            };
    }
}
