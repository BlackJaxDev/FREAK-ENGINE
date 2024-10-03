using ImageMagick;
using XREngine.Data;
using XREngine.Data.Core;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    /// <summary>
    /// Defines raw image data for a 2D texture mipmap.
    /// Has support for resizing with and converting to/from MagickImage.
    /// </summary>
    public class Mipmap2D : XRBase
    {
        public Mipmap2D() { }
        public Mipmap2D(MagickImage? image)
        {
            if (image != null)
                SetFromImage(image);
        }
        public Mipmap2D(Mipmap2D mipmap)
        {
            InternalFormat = mipmap.InternalFormat;
            PixelFormat = mipmap.PixelFormat;
            PixelType = mipmap.PixelType;
            Data = mipmap.Data;
            Width = mipmap.Width;
            Height = mipmap.Height;
        }
        public Mipmap2D(uint width, uint height, EPixelInternalFormat internalFormat, EPixelFormat pixelFormat, EPixelType pixelType)
        {
            Width = width;
            Height = height;
            InternalFormat = internalFormat;
            PixelFormat = pixelFormat;
            PixelType = pixelType;
            Data = new DataSource(XRTexture.AllocateBytes(width, height, pixelFormat, pixelType));
        }

        public DataSource? Data
        {
            get => _bytes;
            set => SetField(ref _bytes, value);
        }
        public uint Width
        {
            get => _width;
            set => SetField(ref _width, value);
        }
        public uint Height 
        {
            get => _height;
            set => SetField(ref _height, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change && propName == nameof(Data) && Data is not null)
                Data.Dispose();
            return change;
        }
        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {

        }

        public static explicit operator Mipmap2D(MagickImage image)
        {
            Mipmap2D mip = new();
            mip.SetFromImage(image);
            return mip;
        }

        public static explicit operator MagickImage(Mipmap2D mipmap)
            => mipmap.GetImage();

        public void SetFromImage(MagickImage image)
        {
            XRTexture.GetFormat(image, false, out EPixelInternalFormat internalFormat, out EPixelFormat format, out EPixelType type);
            InternalFormat = internalFormat;
            PixelFormat = format;
            PixelType = type;

            byte[]? bytes = image.GetPixelsUnsafe().ToByteArray(PixelMapping.RGB);
            Data = bytes is null ? null : new DataSource(bytes);

            Width = image.Width;
            Height = image.Height;
        }

        public MagickImage GetImage()
        {
            MagickImage image = XRTexture.NewImage(Width, Height, PixelFormat, PixelType);
            byte[]? bytes = Data?.GetBytes();
            if (bytes != null)
                image.Read(bytes);
            return image;
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
        private DataSource? _bytes = null;
        private uint _width = 0;
        private uint _height = 0;

        public EPixelInternalFormat InternalFormat
        {
            get => _internalFormat;
            set => SetField(ref _internalFormat, value);
        }

        public void Resize(uint width, uint height)
        {
            if (Data is not null && Data.Length != 0 && Width != 0u && Height != 0u)
            {
                using var img = GetImage();
                img.Resize(width, height);
                SetFromImage(img);
            }
            else
            {
                Width = width;
                Height = height;
            }
        }
        public void InterpolativeResize(uint width, uint height, PixelInterpolateMethod method)
        {
            if (Data is not null && Data.Length != 0 && Width != 0u && Height != 0u)
            {
                using var img = GetImage();
                img.InterpolativeResize(width, height, method);
                SetFromImage(img);
            }
            else
            {
                Width = width;
                Height = height;
            }
        }
        public void AdaptiveResize(uint width, uint height)
        {
            if (Data is not null && Data.Length != 0 && Width != 0u && Height != 0u)
            {
                using var img = GetImage();
                img.AdaptiveResize(width, height);
                SetFromImage(img);
            }
            else
            {
                Width = width;
                Height = height;
            }
        }
        public async Task ResizeAsync(uint width, uint height)
        {
            await Task.Run(() => Resize(width, height));
        }
        public async Task InterpolativeResizeAsync(uint width, uint height, PixelInterpolateMethod method)
        {
            await Task.Run(() => InterpolativeResize(width, height, method));
        }
        public async Task AdaptiveResizeAsync(uint width, uint height)
        {
            await Task.Run(() => AdaptiveResize(width, height));
        }

        public Mipmap2D Clone(bool cloneImage)
            => new()
            {
                InternalFormat = InternalFormat,
                PixelFormat = PixelFormat,
                PixelType = PixelType,
                Data = cloneImage ? Data?.Clone() : Data,
                Width = Width,
                Height = Height
            };
    }
}
