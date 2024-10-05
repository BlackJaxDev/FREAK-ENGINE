﻿using ImageMagick;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public abstract class XRTexture : GenericRenderObject
    {
        public static MagickImage NewImage(uint width, uint height, EPixelFormat format, EPixelType type)
        {
            byte[] data = AllocateBytes(width, height, format, type);
            MagickReadSettings settings = new()
            {
                Width = width,
                Height = height,
                FillColor = HasAlpha(format) ? MagickColor.FromRgba(0, 0, 0, 1) : MagickColor.FromRgb(0, 0, 0),
                Format = GetMagickFormat(format),
                ColorSpace = IsSigned(type) ? ColorSpace.sRGB : ColorSpace.RGB,
                Depth = type switch
                {
                    EPixelType.Byte => 8,
                    EPixelType.Short => 16,
                    EPixelType.Int => 32,
                    EPixelType.Float => 32,
                    EPixelType.HalfFloat => 16,
                    _ => 8,
                },
            };
            return new(data, settings);
        }

        public static byte[] AllocateBytes(uint width, uint height, EPixelFormat format, EPixelType type)
            => new byte[width * height * ComponentSize(type) * GetComponentCount(format)];

        public static void GetFormat(MagickImage bmp, bool internalCompression, out EPixelInternalFormat internalPixelFormat, out EPixelFormat pixelFormat, out EPixelType pixelType)
        {
            //Internal format must match pixel format
            //GL_ALPHA, GL_LUMINANCE, GL_LUMINANCE_ALPHA, GL_RGB, GL_RGBA
            //bool hasAlpha = bmp.HasAlpha;
            uint channels = bmp.ChannelCount;
            bool signed = bmp.Settings.ColorSpace == ColorSpace.sRGB;
            uint depth = bmp.Depth; //8 is s/byte, 16 is u/short, 32 is float
            pixelType = depth switch
            {
                8 => signed ? EPixelType.Byte : EPixelType.UnsignedByte,
                16 => signed ? EPixelType.Short : EPixelType.UnsignedShort,
                32 => EPixelType.Float,
                _ => throw new NotSupportedException($"Unsupported pixel depth: {depth}"),
            };
            switch (channels)
            {
                case 1:
                    internalPixelFormat = internalCompression ? EPixelInternalFormat.CompressedRed : EPixelInternalFormat.Red;
                    pixelFormat = EPixelFormat.Red;
                    break;
                case 2:
                    internalPixelFormat = internalCompression ? EPixelInternalFormat.CompressedRG : EPixelInternalFormat.RG;
                    pixelFormat = EPixelFormat.Rg;
                    break;
                case 3:
                    internalPixelFormat = internalCompression ? EPixelInternalFormat.CompressedRgb : EPixelInternalFormat.Rgb8;
                    pixelFormat = EPixelFormat.Rgb;
                    break;
                default:
                case 4:
                    internalPixelFormat = internalCompression ? EPixelInternalFormat.CompressedRgba : EPixelInternalFormat.Rgba;
                    pixelFormat = EPixelFormat.Rgba;
                    break;
            }
        }

        public static bool IsSigned(EPixelType type)
            => type switch
            {
                EPixelType.Byte or
                EPixelType.Short or
                EPixelType.Int or
                EPixelType.Float or
                EPixelType.HalfFloat => true,
                _ => false,
            };

        public static bool HasAlpha(EPixelFormat fmt)
            => fmt switch
            {
                EPixelFormat.Rgba or
                EPixelFormat.Bgra or
                EPixelFormat.Alpha => true,
                _ => false,
            };

        public static uint ComponentSize(EPixelType type)
            => type switch
            {
                EPixelType.Byte => 1u,
                EPixelType.Short => 2u,
                EPixelType.Int => 4u,
                EPixelType.Float => 4u,
                EPixelType.HalfFloat => 2u,
                _ => 1u,
            };

        public static MagickFormat GetMagickFormat(EPixelFormat fmt)
            => fmt switch
            {
                EPixelFormat.Rgba => MagickFormat.Rgba,
                EPixelFormat.Bgra => MagickFormat.Bgra,
                EPixelFormat.Red => MagickFormat.R,
                EPixelFormat.Green => MagickFormat.G,
                EPixelFormat.Blue => MagickFormat.B,
                EPixelFormat.Alpha => MagickFormat.A,
                EPixelFormat.Rgb => MagickFormat.Rgb,
                EPixelFormat.Bgr => MagickFormat.Bgr,
                _ => MagickFormat.Rgba,
            };

        public static int GetComponentCount(EPixelFormat fmt)
            => fmt switch
            {
                EPixelFormat.Rgba or EPixelFormat.Bgra => 4,
                EPixelFormat.Rgb or EPixelFormat.Bgr => 3,
                EPixelFormat.Red or EPixelFormat.Green or EPixelFormat.Blue or EPixelFormat.Alpha => 1,
                _ => 4,
            };

        public delegate void DelAttachToFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel);
        public delegate void DelDetachFromFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel);
        
        public event DelAttachToFBO? AttachToFBORequested;
        public event DelDetachFromFBO? DetachFromFBORequested;

        private EFrameBufferAttachment? _frameBufferAttachment;
        public EFrameBufferAttachment? FrameBufferAttachment
        {
            get => _frameBufferAttachment;
            set => SetField(ref _frameBufferAttachment, value);
        }

        private string? _samplerName = null;
        /// <summary>
        /// This is the name the texture will use to bind to in the shader.
        /// If <see langword="null"/>, empty or whitespace, uses Texture# as the sampler name, where # is the texture's index in the material.
        /// </summary>
        public string? SamplerName
        {
            get => _samplerName;
            set => SetField(ref _samplerName, value);
        }

        /// <summary>
        /// Returns the level of the smallest allowed mipmap based on the maximum dimension of the base texture.
        /// </summary>
        public int SmallestMipmapLevel =>
            Math.Min((int)Math.Floor(Math.Log10(MaxDimension) * 3.321928f), SmallestAllowedMipmapLevel);

        //Note: 3.321928f is approx 1 / (log base 10 of 2)
        public static int GetSmallestMipmapLevel(uint width, uint height, int smallestAllowedMipmapLevel = 1000)
            => Math.Min((int)Math.Floor(Math.Log10(Math.Max(width, height)) * 3.321928f), smallestAllowedMipmapLevel);

        public abstract uint MaxDimension { get; }

        private int _minLOD = -1000;
        public int MinLOD
        {
            get => _minLOD;
            set => SetField(ref _minLOD, value);
        }

        private int _maxLOD = 1000;
        public int MaxLOD
        {
            get => _maxLOD;
            set => SetField(ref _maxLOD, value);
        }

        private int _largestMipmapLevel = 0;
        public int LargestMipmapLevel
        {
            get => _largestMipmapLevel;
            set => SetField(ref _largestMipmapLevel, value);
        }

        private int _smallestAllowedMipmapLevel = 1000;
        public int SmallestAllowedMipmapLevel
        {
            get => _smallestAllowedMipmapLevel;
            set => SetField(ref _smallestAllowedMipmapLevel, value);
        }

        private bool _autoGenerateMipmaps = false;
        public bool AutoGenerateMipmaps
        {
            get => _autoGenerateMipmaps;
            set => SetField(ref _autoGenerateMipmaps, value);
        }

        private bool _alphaAsTransparency = false;
        public bool AlphaAsTransparency
        {
            get => _alphaAsTransparency;
            set => SetField(ref _alphaAsTransparency, value);
        }

        private bool _internalCompression = false;
        public bool InternalCompression
        {
            get => _internalCompression;
            set => SetField(ref _internalCompression, value);
        }

        public virtual bool IsResizeable { get; } = false;

        public void AttachToFBO(XRFrameBuffer target, int mipLevel = 0)
        {
            if (FrameBufferAttachment.HasValue)
                AttachToFBO(target, FrameBufferAttachment.Value, mipLevel);
        }

        public void DetachFromFBO(XRFrameBuffer target, int mipLevel = 0)
        {
            if (FrameBufferAttachment.HasValue)
                DetachFromFBO(target, FrameBufferAttachment.Value, mipLevel);
        }

        public void AttachToFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel = 0)
            => AttachToFBORequested?.Invoke(target, attachment, mipLevel);
        public void DetachFromFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel = 0)
            => DetachFromFBORequested?.Invoke(target, attachment, mipLevel);

        /// <summary>
        /// Returns the sampler name for this texture to bind into the shader.
        /// </summary>
        /// <param name="textureIndex">The index of the texture. Only used if the override parameter and the SamplerName property are null or invalid.</param>
        /// <param name="samplerNameOverride">The binding name to force bind to, if desired.</param>
        /// <returns></returns>
        public string ResolveSamplerName(int textureIndex, string? samplerNameOverride = null)
            => samplerNameOverride ?? SamplerName ?? $"Texture{textureIndex}";

        //public XREvent<XRTexture> SetParametersRequested { get; } = new XREvent<XRTexture>();
        //public void SetParameters() => SetParametersRequested.Invoke(this);

        public void SampleIn(XRRenderProgram program, int textureIndex)
            => program.Sampler(ResolveSamplerName(textureIndex, null), this, textureIndex);

        public event Action? PushDataRequested;
        public event Action? BindRequested;
        public event Action? UnbindRequested;
        public event DelClear? ClearRequested;
        public event Action? GenerateMipmapsRequested;

        public delegate void DelClear(ColorF4 color, int level = 0);

        public void PushData()
            => PushDataRequested?.Invoke();
        public void Bind()
            => BindRequested?.Invoke();
        public void Unbind()
            => UnbindRequested?.Invoke();
        public void Clear(ColorF4 color, int level = 0)
            => ClearRequested?.Invoke(color, level);

        /// <summary>
        /// Requests the GPU to generate mipmaps for this image.
        /// </summary>
        public void GenerateMipmapsGPU()
            => GenerateMipmapsRequested?.Invoke();
    }
}
