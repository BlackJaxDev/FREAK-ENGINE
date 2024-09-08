﻿using XREngine.Data.Colors;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public abstract class XRTexture : GenericRenderObject, IFrameBufferAttachement
    {
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
        public int SmallestMipmapLevel => //Note: 3.321928f is approx 1 / (log base 10 of 2)
            Math.Min((int)Math.Floor(Math.Log10(MaxDimension) * 3.321928f), SmallestAllowedMipmapLevel);

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

        public bool Signed { get; set; } = false;
        public bool AlphaAsTransparency { get; set; } = false;
        public bool Compressed { get; set; } = false;

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

        //TODO: support all texture types
        public static XRTexture New(ETextureType type)
            => type switch
            {
                ETextureType.Tex1D => new XRTexture1D(),
                ETextureType.Tex2D => new XRTexture2D(),
                ETextureType.Tex3D => new XRTexture3D(),
                ETextureType.TexCube => new XRTextureCube(),
                ETextureType.Tex2DRect => new XRTexture2D() { Rectangle = true },
                ETextureType.Tex1DArray => new XRTexture1DArray(),
                ETextureType.Tex2DArray => new XRTexture2DArray(),
                //ETextureType.TexCubeArray => new XRTextureCubeArray(),
                ETextureType.TexBuffer => new XRTextureBuffer(),
                ETextureType.Tex2DMultisample => new XRTexture2D() { MultiSample = true },
                ETextureType.Tex2DMultisampleArray => new XRTexture2DArray() { MultiSample = true },
                _ => throw new InvalidOperationException($"Invalid texture type: {type}")
            };

        /// <summary>
        /// Returns the sampler name for this texture to bind into the shader.
        /// </summary>
        /// <param name="textureIndex">The index of the texture. Only used if the override parameter and the SamplerName property are null or invalid.</param>
        /// <param name="samplerNameOverride">The binding name to force bind to, if desired.</param>
        /// <returns></returns>
        public string ResolveSamplerName(int textureIndex, string? samplerNameOverride = null)
            => samplerNameOverride ?? SamplerName ?? ($"Texture{textureIndex}");

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