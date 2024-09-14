using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTextureCube : XRTexture
    {
        public delegate void DelAttachFaceToFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, ECubemapFace face, int mipLevel);
        public delegate void DelDetachFaceFromFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, ECubemapFace face, int mipLevel);

        public event DelAttachFaceToFBO? AttachFaceToFBORequested;
        public event DelDetachFaceFromFBO? DetachFaceFromFBORequested;

        public XRTextureCube()
            : this(1u) { }

        public XRTextureCube(
            uint dim,
            int mipCount = 1)
        {
            _cubeExtent = dim;
            uint sDim = dim;
            Mipmaps = new CubeMipmap[mipCount];
            for (uint i = 0u, scale = 1u; i < mipCount; scale = 1u << (int)++i, sDim = dim / scale)
                Mipmaps[i] = new CubeMipmap(sDim, sDim);
        }

        public XRTextureCube(
            uint dim,
            EPixelInternalFormat internalFormat,
            EPixelFormat pixelFormat,
            EPixelType pixelType,
            int mipCount = 1)
            : this(dim, mipCount)
        {
            uint sDim = dim;
            Mipmaps = new CubeMipmap[mipCount];
            for (uint i = 0u, scale = 1u; i < mipCount; scale = 1u << (int)++i, sDim = dim / scale)
                Mipmaps[i] = new CubeMipmap(sDim, sDim, internalFormat, pixelFormat, pixelType);
            _internalFormat = internalFormat;
            _pixelFormat = pixelFormat;
            _pixelType = pixelType;
        }

        public XRTextureCube(params CubeMipmap[] mipmaps)
            => Mipmaps = mipmaps;

        public CubeMipmap[] Mipmaps { get; set; }

        private uint _dimension;
        public uint Dimension => Mipmaps is null ? _dimension : (Mipmaps.Length > 0 ? Mipmaps[0].Sides[0].Width : _dimension);

        public override uint MaxDimension => Dimension;

        private uint _cubeExtent;
        
        private ETexWrapMode _uWrapMode = ETexWrapMode.ClampToEdge;
        private ETexWrapMode _vWrapMode = ETexWrapMode.ClampToEdge;
        private ETexWrapMode _wWrapMode = ETexWrapMode.ClampToEdge;
        private ETexMinFilter _minFilter = ETexMinFilter.Nearest;
        private ETexMagFilter _magFilter = ETexMagFilter.Nearest;
        private float _lodBias = 0.0f;

        private EPixelFormat _pixelFormat = EPixelFormat.Rgba;
        private EPixelType _pixelType = EPixelType.UnsignedByte;
        private EPixelInternalFormat _internalFormat = EPixelInternalFormat.Rgba8;
        private bool _resizable = true;

        public bool Resizable
        {
            get => _resizable;
            set => SetField(ref _resizable, value);
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
            get => _uWrapMode;
            set => SetField(ref _uWrapMode, value);
        }

        public ETexWrapMode VWrap
        {
            get => _vWrapMode;
            set => SetField(ref _vWrapMode, value);
        }

        public ETexWrapMode WWrap
        {
            get => _wWrapMode;
            set => SetField(ref _wWrapMode, value);
        }

        public float LodBias
        {
            get => _lodBias;
            set => SetField(ref _lodBias, value);
        }

        public uint CubeExtent => _cubeExtent;

        public void AttachFaceToFBO(XRFrameBuffer fbo, ECubemapFace face, int mipLevel = 0)
        {
            if (FrameBufferAttachment.HasValue)
                AttachFaceToFBO(fbo, FrameBufferAttachment.Value, face, mipLevel);
        }
        public void DetachFaceFromFBO(XRFrameBuffer fbo, ECubemapFace face, int mipLevel = 0)
        {
            if (FrameBufferAttachment.HasValue)
                DetachFaceFromFBO(fbo, FrameBufferAttachment.Value, face, mipLevel);
        }

        public void AttachFaceToFBO(XRFrameBuffer fbo, EFrameBufferAttachment attachment, ECubemapFace face, int mipLevel = 0)
            => AttachFaceToFBORequested?.Invoke(fbo, attachment, face, mipLevel);
        public void DetachFaceFromFBO(XRFrameBuffer fbo, EFrameBufferAttachment attachment, ECubemapFace face, int mipLevel = 0)
            => DetachFaceFromFBORequested?.Invoke(fbo, attachment, face, mipLevel);

        ///// <summary>
        ///// Call if you want to load all mipmap texture files, in a background thread for example.
        ///// </summary>
        //public void LoadMipmaps()
        //{
        //    _isLoading = true;
        //    if (Mipmaps != null && Mipmaps.Length > 0)
        //    {
        //        _texture.Mipmaps = new TextureCubeMipmap[Mipmaps.Length];
        //        //Task.Run(() => Parallel.For(0, Mipmaps.Length, i =>
        //        for (int i = 0; i < Mipmaps.Length; ++i)
        //            _texture.Mipmaps[i] = Mipmaps[i].AsRenderMipmap(i);
        //    }

        //    _isLoading = false;
        //}
    }
}
