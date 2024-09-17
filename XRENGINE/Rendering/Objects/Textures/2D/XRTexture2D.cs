using ImageMagick;
using ImageMagick.Drawing;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;

namespace XREngine.Rendering
{
    public class XRTexture2D : XRTexture
    {
        private static MagickImage? _fillerImage = null;
        public static MagickImage FillerImage => _fillerImage ??= GetFillerBitmap();

        private static MagickImage GetFillerBitmap()
        {
            string path = Path.Combine(Engine.GameSettings.TexturesFolder, "Filler.png");
            if (File.Exists(path))
                return new MagickImage(path);
            else
            {
                const int squareExtent = 4;
                const int dim = squareExtent * 2;

                // Create a checkerboard pattern image without using bitmap
                MagickImage img = new(MagickColors.Blue, dim, dim);
                img.Draw(new Drawables()
                    .FillColor(MagickColors.Red)
                    .Rectangle(0, 0, squareExtent, squareExtent)
                    .Rectangle(squareExtent, squareExtent, dim, dim));
                return img;
            }
        }

        protected uint _width = 0;
        protected uint _height = 0;

        private EDepthStencilFmt _depthStencilFormat = EDepthStencilFmt.None;
        private ETexMagFilter _magFilter = ETexMagFilter.Nearest;
        private ETexMinFilter _minFilter = ETexMinFilter.Nearest;
        private ETexWrapMode _uWrap = ETexWrapMode.Repeat;
        private ETexWrapMode _vWrap = ETexWrapMode.Repeat;
        private float _lodBias = 0.0f;
        private bool _resizable = true;
        private bool _exclusiveSharing = true;

        /// <summary>
        /// If false, calling resize will do nothing.
        /// Useful for repeating textures that must always be a certain size or textures that never need to be dynamically resized during the game.
        /// False by default.
        /// </summary>
        public bool Resizable
        {
            get => _resizable;
            set => SetField(ref _resizable, value);
        }

        public override uint MaxDimension => Math.Max(Width, Height);

        public XRTexture2D() : this(1u, 1u, EPixelFormat.Rgb, EPixelType.UnsignedByte) { }
        public XRTexture2D(uint width, uint height, EPixelFormat format, EPixelType type, int mipmapCount = 1)
        {
            _mipmaps = new MagickImage[mipmapCount];
            for (uint i = 0, scale = 1; i < mipmapCount; scale = 1u << (int)++i)
                _mipmaps[i] = NewImage(width / scale, height / scale, format, type);
            _width = width;
            _height = height;
        }

        public XRTexture2D(params string[] mipMapPaths)
        {
            _mipmaps = new MagickImage[mipMapPaths.Length];
            for (int i = 0; i < mipMapPaths.Length; ++i)
            {
                string path = mipMapPaths[i];
                if (path.StartsWith("file://"))
                    path = path[7..];
                _mipmaps[i] = new(path);
            }
            if (_mipmaps.Length > 0)
            {
                _width = _mipmaps[0].Width;
                _height = _mipmaps[0].Height;
            }
        }
        public XRTexture2D(uint width, uint height, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
        {
            _mipmaps = new MagickImage[1];
            _mipmaps[0] = NewImage(width, height, format, type);
            _width = width;
            _height = height;
            InternalFormat = internalFormat;
            PixelFormat = format;
        }
        public XRTexture2D(uint width, uint height, params MagickImage[] mipmaps)
        {
            _mipmaps = new MagickImage[mipmaps.Length];
            for (int i = 0; i < mipmaps.Length; ++i)
                _mipmaps[i] = mipmaps[i];
            Resize(width, height);
        }

        public MagickImage[] _mipmaps = [];
        public MagickImage[] Mipmaps
        {
            get => _mipmaps;
            set => SetField(ref _mipmaps, value);
        }

        public EDepthStencilFmt DepthStencilFormat
        {
            get => _depthStencilFormat;
            set => SetField(ref _depthStencilFormat, value);
        }
        public ETexMagFilter MagFilter
        {
            get => _magFilter;
            set => SetField(ref _magFilter, value);
        }
        public ETexMinFilter MinFilter
        {
            get => _minFilter;
            set => SetField(ref _minFilter, value);
        }
        public ETexWrapMode UWrap
        {
            get => _uWrap;
            set => SetField(ref _uWrap, value);
        }
        public ETexWrapMode VWrap
        {
            get => _vWrap;
            set => SetField(ref _vWrap, value);
        }
        public float LodBias
        {
            get => _lodBias;
            set => _lodBias = value;
        }

        public uint Width => _width;
        public uint Height => _height;

        /// <summary>
        /// Set on construction
        /// </summary>
        public bool Rectangle { get; internal set; } = false;
        /// <summary>
        /// Set on construction
        /// </summary>
        public bool MultiSample { get; internal set; } = false;

        public bool ExclusiveSharing
        {
            get => _exclusiveSharing;
            set => SetField(ref _exclusiveSharing, value);
        }

        /// <summary>
        /// Resizes the textures stored in memory.
        /// Does nothing if Resizeable is false.
        /// </summary>
        public void Resize(uint width, uint height)
        {
            if (_width == width && 
                _height == height)
                return;

            if (!Resizable)
            {
                Debug.Out("Tried to resize texture that is immutable (storage size is non-resizable).");
                return;
            }

            _width = width;
            _height = height;

            if (_mipmaps is null || _mipmaps.Length <= 0)
                return;

            for (int i = 0; i < _mipmaps.Length && width > 0u && height > 0u; ++i)
            {
                if (_mipmaps[i] is null)
                    continue;

                _mipmaps[i].Resize(width, height);

                width >>= 1;
                height >>= 1;
            }

            Resized?.Invoke();
        }

        public event Action? Resized;

        /// <summary>
        /// Generates mipmaps from the base texture.
        /// </summary>
        public void GenerateMipmapsCPU()
        {
            if (_mipmaps is null || _mipmaps.Length <= 0)
                return;

            var baseTexture = _mipmaps[0];
            if (baseTexture is null)
                return;

            _mipmaps = new MagickImage[SmallestMipmapLevel];
            _mipmaps[0] = baseTexture;
            
            for (int i = 1; i < _mipmaps.Length; ++i)
            {
                var clone = _mipmaps[i - 1].Clone();
                clone.Resize(_width >> i, _height >> i);
                _mipmaps[i] = (MagickImage)clone;
            }
        }

        /// <summary>
        /// Creates a new texture specifically for attaching to a framebuffer.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="width">The texture's width.</param>
        /// <param name="height">The texture's height.</param>
        /// <param name="internalFmt">The internal texture storage format.</param>
        /// <param name="format">The format of the texture's pixels.</param>
        /// <param name="pixelType">How pixels are stored.</param>
        /// <param name="bufAttach">Where to attach to the framebuffer for rendering to.</param>
        /// <returns>A new 2D texture reference.</returns>
        public static XRTexture2D CreateFrameBufferTexture(uint width, uint height,
            EPixelInternalFormat internalFmt, EPixelFormat format, EPixelType type, EFrameBufferAttachment bufAttach)
            => new(width, height, internalFmt, format, type)
            {
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                FrameBufferAttachment = bufAttach,
            };

        /// <summary>
        /// Creates a new texture specifically for attaching to a framebuffer.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bounds"></param>
        /// <param name="internalFormat"></param>
        /// <param name="format"></param>
        /// <param name="pixelType"></param>
        /// <returns></returns>
        public static XRTexture2D CreateFrameBufferTexture(IVector2 bounds, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
            => CreateFrameBufferTexture((uint)bounds.X, (uint)bounds.Y, internalFormat, format, type);
        /// <summary>
        /// Creates a new texture specifically for attaching to a framebuffer.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="width">The texture's width.</param>
        /// <param name="height">The texture's height.</param>
        /// <param name="internalFmt">The internal texture storage format.</param>
        /// <param name="format">The format of the texture's pixels.</param>
        /// <param name="pixelType">How pixels are stored.</param>
        /// <returns>A new 2D texture reference.</returns>
        public static XRTexture2D CreateFrameBufferTexture(uint width, uint height, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
            => new(width, height, internalFormat, format, type)
            {
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
            };
    }
}
