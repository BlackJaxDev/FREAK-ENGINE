using ImageMagick;
using ImageMagick.Drawing;
using XREngine.Data;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;

namespace XREngine.Rendering
{
    [XR3rdPartyExtensions("png", "jpg", "jpeg", "tif", "tiff", "tga", "exr", "hdr")]
    public class XRTexture2D : XRTexture, IFrameBufferAttachement
    {
        public override void Load3rdParty(string filePath)
        {
            Mipmap2D[] mips = [new Mipmap2D(1, 1, EPixelInternalFormat.Red, EPixelFormat.Red, EPixelType.UnsignedByte, false)];
            //mips = GetMipmapsFromImage(image);
            Mipmaps = mips;
            AutoGenerateMipmaps = true;

            //TODO: load async and ensure the texture is pushed after the black default texture
            Task.Run(() => mips[0].SetFromImage(new MagickImage(filePath)));
        }

        public static Mipmap2D[] GetMipmapsFromImage(MagickImage image)
        {
            Mipmap2D[] mips = new Mipmap2D[GetSmallestMipmapLevel(image.Width, image.Height)];
            mips[0] = new Mipmap2D(image);
            uint w = image.Width;
            uint h = image.Height;
            for (int i = 1; i < mips.Length; ++i)
            {
                var clone = image.Clone();
                clone.Resize(w >> i, h >> i);
                mips[i] = new Mipmap2D(clone as MagickImage);
            }
            return mips;
        }

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

        private ESizedInternalFormat _sizedInternalFormat = ESizedInternalFormat.Rgba32f;
        private EDepthStencilFmt _depthStencilFormat = EDepthStencilFmt.None;
        private ETexMagFilter _magFilter = ETexMagFilter.Nearest;
        private ETexMinFilter _minFilter = ETexMinFilter.Nearest;
        private ETexWrapMode _uWrap = ETexWrapMode.Repeat;
        private ETexWrapMode _vWrap = ETexWrapMode.Repeat;
        private float _lodBias = 0.0f;
        private bool _resizable = true;
        private bool _exclusiveSharing = true;

        public override bool IsResizeable => Resizable;

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

        public XRTexture2D() : this(1u, 1u, EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte, true) { }
        public XRTexture2D(uint width, uint height, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type, int mipmapCount)
        {
            Mipmap2D[] mips = new Mipmap2D[mipmapCount];
            for (uint i = 0; i < mipmapCount; ++i)
            {
                byte[] data = AllocateBytes(width, height, format, type);
                Mipmap2D mipmap = new()
                {
                    InternalFormat = internalFormat,
                    PixelFormat = format,
                    PixelType = type,
                    Width = width,
                    Height = height,
                    Data = new DataSource(data)
                };
                width >>= 1;
                height >>= 1;
                mips[i] = mipmap;
            }
            Mipmaps = mips;
        }

        public XRTexture2D(params string[] mipMapPaths)
        {
            Mipmap2D[] mips = new Mipmap2D[mipMapPaths.Length];
            for (int i = 0; i < mipMapPaths.Length; ++i)
            {
                string path = mipMapPaths[i];
                if (path.StartsWith("file://"))
                    path = path[7..];
                try
                {
                    mips[i] = new Mipmap2D(new MagickImage(path));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to load texture from path: {path}{Environment.NewLine}{e.Message}");
                }
            }
            Mipmaps = mips;
        }
        public XRTexture2D(uint width, uint height, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type, bool allocateData = false)
        {
            Mipmaps = [new Mipmap2D() 
            {
                InternalFormat = internalFormat,
                PixelFormat = format,
                PixelType = type,
                Width = width,
                Height = height,
                Data = allocateData ? new DataSource(AllocateBytes(width, height, format, type)) : null
            }];
        }
        public XRTexture2D(uint width, uint height, params MagickImage?[] mipmaps)
        {
            Mipmap2D[] mips = new Mipmap2D[mipmaps.Length];
            for (int i = 0; i < mipmaps.Length; ++i)
            {
                var image = mipmaps[i];
                image?.Resize(width >> i, height >> i);
                mips[i] = new Mipmap2D(image);
            }
            Mipmaps = mips;
        }
        public XRTexture2D(MagickImage? image)
        {
            Mipmaps = [new Mipmap2D(image)];
        }

        public Mipmap2D[] _mipmaps = [];
        public Mipmap2D[] Mipmaps
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
            set => SetField(ref _lodBias, value);
        }
        public ESizedInternalFormat SizedInternalFormat
        {
            get => _sizedInternalFormat;
            set => SetField(ref _sizedInternalFormat, value);
        }

        public uint Width => Mipmaps.Length > 0 ? Mipmaps[0].Width : 0;
        public uint Height => Mipmaps.Length > 0 ? Mipmaps[0].Height : 0;

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
            if (Width == width && 
                Height == height)
                return;

            if (_mipmaps is null || _mipmaps.Length <= 0)
                return;

            for (int i = 0; i < _mipmaps.Length && width > 0u && height > 0u; ++i)
            {
                if (_mipmaps[i] is null)
                    continue;

                _mipmaps[i]?.Resize(width, height);

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

            Mipmaps = GetMipmapsFromImage(_mipmaps[0].GetImage());
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
            => new(width, height, internalFmt, format, type, false)
            {
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                AutoGenerateMipmaps = false,
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
            => new(width, height, internalFormat, format, type, false)
            {
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                AutoGenerateMipmaps = false,
            };

        private XRDataBuffer? _pbo;

        public bool ShouldLoadDataFromPBO
        {
            get => _pbo != null;
            set
            {
                if (value)
                {
                    if (_pbo == null)
                    {
                        _pbo = new XRDataBuffer(EBufferTarget.PixelUnpackBuffer, true);
                        _pbo.Generate();
                    }
                }
                else
                {
                    if (_pbo != null)
                    {
                        _pbo.Destroy();
                        _pbo = null;
                    }
                }
            }
        }

        public unsafe void LoadFromPBO(int mipIndex)
        {
            if (_pbo is null)
                return;

            var mipmap = Mipmaps[mipIndex].Data;
            if (mipmap is null)
                return;

            _pbo.MapBufferData();
            _pbo.SetDataPointer(mipmap.Address);
            _pbo.UnmapBufferData();
        }
    }
}
