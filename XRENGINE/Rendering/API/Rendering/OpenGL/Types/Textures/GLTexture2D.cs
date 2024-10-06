using Extensions;
using Silk.NET.OpenGL;
using System.ComponentModel;
using XREngine.Data;
using XREngine.Data.Core;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.OpenGL
{
    public class GLTexture2D(OpenGLRenderer renderer, XRTexture2D data) : GLTexture<XRTexture2D>(renderer, data)
    {
        private bool _storageSet = false;

        public class MipmapInfo : XRBase
        {
            private bool _hasPushedResizedData = false;
            private bool _hasPushedUpdateData = false;
            private readonly Mipmap2D _mipmap;
            private readonly GLTexture2D _texture;

            public Mipmap2D Mipmap => _mipmap;

            public MipmapInfo(GLTexture2D texture, Mipmap2D mipmap)
            {
                _texture = texture;
                _mipmap = mipmap;
                _mipmap.PropertyChanged += MipmapPropertyChanged;
            }
            ~MipmapInfo()
            {
                _mipmap.PropertyChanged -= MipmapPropertyChanged;
            }

            private void MipmapPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(Mipmap.Data):
                        //HasPushedUpdateData = false;
                        _texture.Invalidate();
                        break;
                    case nameof(Mipmap.Width):
                    case nameof(Mipmap.Height):
                    case nameof(Mipmap.InternalFormat):
                    case nameof(Mipmap.PixelFormat):
                    case nameof(Mipmap.PixelType):
                        HasPushedResizedData = false;
                        //HasPushedUpdateData = false;
                        _texture.Invalidate();
                        break;
                }
            }

            public bool HasPushedResizedData
            {
                get => _hasPushedResizedData;
                set => SetField(ref _hasPushedResizedData, value);
            }
            //public bool HasPushedUpdateData
            //{
            //    get => _hasPushedUpdateData;
            //    set => SetField(ref _hasPushedUpdateData, value);
            //}
        }

        public MipmapInfo[] Mipmaps { get; private set; } = [];

        protected override void DataPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.DataPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case nameof(XRTexture2D.Mipmaps):
                    {
                        UpdateMipmaps();
                        break;
                    }
            }
        }

        private void UpdateMipmaps()
        {
            Mipmaps = new MipmapInfo[Data.Mipmaps.Length];
            for (int i = 0; i < Data.Mipmaps.Length; ++i)
                Mipmaps[i] = new MipmapInfo(this, Data.Mipmaps[i]);
            Invalidate();
        }

        public override ETextureTarget TextureTarget { get; } = ETextureTarget.Texture2D;

        protected override void UnlinkData()
        {
            base.UnlinkData();

            Data.Resized -= DataResized;
            Mipmaps = [];
        }
        protected override void LinkData()
        {
            base.LinkData();

            Data.Resized += DataResized;
            UpdateMipmaps();
        }

        private void DataResized()
        {
            _storageSet = false;
            Mipmaps.ForEach(m =>
            {
                m.HasPushedResizedData = false;
                //m.HasPushedUpdateData = false;
            });
            Invalidate();
        }

        protected internal override void PostGenerated()
        {
            Mipmaps.ForEach(m =>
            {
                m.HasPushedResizedData = false;
                //m.HasPushedUpdateData = false;
            });
            _storageSet = false;
            base.PostGenerated();
        }
        protected internal override void PostDeleted()
        {
            _storageSet = false;
            base.PostDeleted();
        }

        //TODO: use PBO per texture for quick data updates
        public override unsafe void PushData()
        {
            if (IsPushing)
                return;
            try
            {
                IsPushing = true;
                Debug.Out($"Pushing texture: {GetDescribingName()}");
                OnPrePushData(out bool shouldPush, out bool allowPostPushCallback);
                if (!shouldPush)
                {
                    if (allowPostPushCallback)
                        OnPostPushData();
                    IsPushing = false;
                    return;
                }

                Bind();

                var glTarget = ToGLEnum(TextureTarget);

                Api.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                EPixelInternalFormat? internalFormatForce = null;
                if (!Data.Resizable && !_storageSet)
                {
                    Api.TexStorage2D(glTarget, Math.Max((uint)Data.Mipmaps.Length, 1u), ToGLEnum(Data.SizedInternalFormat), Data.Width, Data.Height);
                    internalFormatForce = ToBaseInternalFormat(Data.SizedInternalFormat);
                    _storageSet = true;
                }

                if (Mipmaps is null || Mipmaps.Length == 0)
                    PushMipmap(glTarget, 0, null, internalFormatForce);
                else
                {
                    for (int i = 0; i < Mipmaps.Length; ++i)
                        PushMipmap(glTarget, i, Mipmaps[i], internalFormatForce);

                    if (Data.AutoGenerateMipmaps)
                        GenerateMipmaps();
                }

                int max = Mipmaps is null || Mipmaps.Length == 0 ? 0 : Mipmaps.Length - 1;
                Api.TexParameter(glTarget, GLEnum.TextureBaseLevel, 0);
                Api.TexParameter(glTarget, GLEnum.TextureMaxLevel, max);
                Api.TexParameter(glTarget, GLEnum.TextureMinLod, 0);
                Api.TexParameter(glTarget, GLEnum.TextureMaxLod, max);
                
                if (allowPostPushCallback)
                    OnPostPushData();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                IsPushing = false;
                Unbind();
            }
        }

        private unsafe void PushMipmap(GLEnum glTarget, int i, MipmapInfo? info, EPixelInternalFormat? internalFormatForce)
        {
            if (!Data.Resizable && !_storageSet)
            {
                Debug.LogWarning("Texture storage not set on non-resizable texture, can't push mipmaps.");
                return;
            }

            GLEnum pixelFormat;
            GLEnum pixelType;
            InternalFormat internalPixelFormat;

            DataSource? data;
            bool hasPushedResized;
            Mipmap2D? mip = info?.Mipmap;
            if (mip is null)
            {
                internalPixelFormat = ToInternalFormat(internalFormatForce ?? EPixelInternalFormat.Rgb);
                pixelFormat = GLEnum.Rgb;
                pixelType = GLEnum.UnsignedByte;
                data = null;
                hasPushedResized = false;
            }
            else
            {
                pixelFormat = ToGLEnum(mip.PixelFormat);
                pixelType = ToGLEnum(mip.PixelType);
                internalPixelFormat = ToInternalFormat(internalFormatForce ?? mip.InternalFormat);
                data = mip.Data;
                hasPushedResized = info!.HasPushedResizedData;
            }

            if (data is null || data.Length == 0)
                Push(glTarget, i, Data.Width >> i, Data.Height >> i, pixelFormat, pixelType, internalPixelFormat, hasPushedResized);
            else
            {
                Push(glTarget, i, mip!.Width, mip.Height, pixelFormat, pixelType, internalPixelFormat, data, hasPushedResized);
                //data?.Dispose();
            }

            if (info != null)
            {
                info.HasPushedResizedData = true;
                //info.HasPushedUpdateData = true;
            }
        }

        private unsafe void Push(GLEnum glTarget, int i, uint w, uint h, GLEnum pixelFormat, GLEnum pixelType, InternalFormat internalPixelFormat, bool hasPushedResized)
        {
            if (!hasPushedResized && Data.Resizable)
                Api.TexImage2D(glTarget, i, internalPixelFormat, w, h, 0, pixelFormat, pixelType, IntPtr.Zero.ToPointer());
        }
        private unsafe void Push(GLEnum glTarget, int i, uint w, uint h, GLEnum pixelFormat, GLEnum pixelType, InternalFormat internalPixelFormat, DataSource bmp, bool hasPushedResized)
        {
            // If a non-zero named buffer object is bound to the GL_PIXEL_UNPACK_BUFFER target (see glBindBuffer) while a texture image is specified,
            // the data ptr is treated as a byte offset into the buffer object's data store.
            void* ptr = bmp.Address.Pointer;
            if (ptr is null)
            {
                Debug.LogWarning("Texture data source is null.");
                return;
            }

            if (hasPushedResized || _storageSet)
                Api.TexSubImage2D(glTarget, i, 0, 0, w, h, pixelFormat, pixelType, ptr);
            else
                Api.TexImage2D(glTarget, i, internalPixelFormat, w, h, 0, pixelFormat, pixelType, ptr);
        }

        protected override void SetParameters()
        {
            Api.TextureParameter(BindingId, GLEnum.TextureLodBias, Data.LodBias);

            //int dsmode = Data.DepthStencilFormat == EDepthStencilFmt.Stencil ? (int)GLEnum.StencilIndex : (int)GLEnum.DepthComponent;
            //Api.TextureParameterI(BindingId, GLEnum.DepthStencilTextureMode, in dsmode);

            int magFilter = (int)ToGLEnum(Data.MagFilter);
            Api.TextureParameterI(BindingId, GLEnum.TextureMagFilter, in magFilter);

            int minFilter = (int)ToGLEnum(Data.MinFilter);
            Api.TextureParameterI(BindingId, GLEnum.TextureMinFilter, in minFilter);

            int uWrap = (int)ToGLEnum(Data.UWrap);
            Api.TextureParameterI(BindingId, GLEnum.TextureWrapS, in uWrap);

            int vWrap = (int)ToGLEnum(Data.VWrap);
            Api.TextureParameterI(BindingId, GLEnum.TextureWrapT, in vWrap);

            base.SetParameters();
        }

        private static InternalFormat ToInternalFormat(EPixelInternalFormat internalFormat)
            => (InternalFormat)internalFormat.ConvertByName(typeof(InternalFormat));

        private static EPixelInternalFormat ToBaseInternalFormat(ESizedInternalFormat sizedInternalFormat)
            => sizedInternalFormat switch
            {
                ESizedInternalFormat.R8 or
                ESizedInternalFormat.R8Snorm or
                ESizedInternalFormat.R16 or
                ESizedInternalFormat.R16Snorm or
                ESizedInternalFormat.R16f or
                ESizedInternalFormat.R32f or
                ESizedInternalFormat.R8i or
                ESizedInternalFormat.R8ui or
                ESizedInternalFormat.R16i or
                ESizedInternalFormat.R16ui or
                ESizedInternalFormat.R32i or
                ESizedInternalFormat.R32ui => EPixelInternalFormat.Red,

                ESizedInternalFormat.Rg8 or
                ESizedInternalFormat.Rg8Snorm or
                ESizedInternalFormat.Rg16 or
                ESizedInternalFormat.Rg16Snorm or
                ESizedInternalFormat.Rg16f or
                ESizedInternalFormat.Rg32f or
                ESizedInternalFormat.Rg8i or
                ESizedInternalFormat.Rg8ui or
                ESizedInternalFormat.Rg16i or
                ESizedInternalFormat.Rg16ui or
                ESizedInternalFormat.Rg32i or
                ESizedInternalFormat.Rg32ui => EPixelInternalFormat.RG,

                ESizedInternalFormat.R3G3B2 or
                ESizedInternalFormat.Rgb4 or
                ESizedInternalFormat.Rgb5 or
                ESizedInternalFormat.Rgb8 or
                ESizedInternalFormat.Rgb8Snorm or
                ESizedInternalFormat.Rgb10 or
                ESizedInternalFormat.Rgb12 or
                ESizedInternalFormat.Rgb16Snorm or
                ESizedInternalFormat.Rgba2 or
                ESizedInternalFormat.Rgba4 or
                ESizedInternalFormat.Srgb8 or
                ESizedInternalFormat.Rgb16f or
                ESizedInternalFormat.Rgb32f or
                ESizedInternalFormat.R11fG11fB10f or
                ESizedInternalFormat.Rgb9E5 or
                ESizedInternalFormat.Rgb8i or
                ESizedInternalFormat.Rgb8ui or
                ESizedInternalFormat.Rgb16i or
                ESizedInternalFormat.Rgb16ui or
                ESizedInternalFormat.Rgb32i or
                ESizedInternalFormat.Rgb32ui => EPixelInternalFormat.Rgb,

                ESizedInternalFormat.Rgb5A1 or
                ESizedInternalFormat.Rgba8 or
                ESizedInternalFormat.Rgba8Snorm or
                ESizedInternalFormat.Rgb10A2 or
                ESizedInternalFormat.Rgba12 or
                ESizedInternalFormat.Rgba16 or
                ESizedInternalFormat.Srgb8Alpha8 or
                ESizedInternalFormat.Rgba16f or
                ESizedInternalFormat.Rgba32f or
                ESizedInternalFormat.Rgba8i or
                ESizedInternalFormat.Rgba8ui or
                ESizedInternalFormat.Rgba16i or
                ESizedInternalFormat.Rgba16ui or
                ESizedInternalFormat.Rgba32i or
                ESizedInternalFormat.Rgba32ui => EPixelInternalFormat.Rgba,

                ESizedInternalFormat.DepthComponent16 or
                ESizedInternalFormat.DepthComponent24 or
                ESizedInternalFormat.DepthComponent32f => EPixelInternalFormat.DepthComponent,

                ESizedInternalFormat.Depth24Stencil8 or
                ESizedInternalFormat.Depth32fStencil8 => EPixelInternalFormat.DepthStencil,

                ESizedInternalFormat.StencilIndex8 => EPixelInternalFormat.StencilIndex,

                _ => throw new ArgumentOutOfRangeException(nameof(sizedInternalFormat), sizedInternalFormat, null),
            };

        //private static ESizedInternalFormat ToSizedInternalFormat(EPixelInternalFormat internalFormat)
        //    => internalFormat switch
        //    {
        //        EPixelInternalFormat.Rgb8 => ESizedInternalFormat.Rgb8,
        //        EPixelInternalFormat.Rgba8 => ESizedInternalFormat.Rgba8,
        //        EPixelInternalFormat.Rgba16 => ESizedInternalFormat.Rgba16,
        //        EPixelInternalFormat.R8 => ESizedInternalFormat.R8,
        //        EPixelInternalFormat.R16 => ESizedInternalFormat.R16,
        //        EPixelInternalFormat.RG8 => ESizedInternalFormat.Rg8,
        //        EPixelInternalFormat.RG16 => ESizedInternalFormat.Rg16,
        //        EPixelInternalFormat.R16f => ESizedInternalFormat.R16f,
        //        EPixelInternalFormat.R32f => ESizedInternalFormat.R32f,
        //        EPixelInternalFormat.RG16f => ESizedInternalFormat.Rg16f,
        //        EPixelInternalFormat.RG32f => ESizedInternalFormat.Rg32f,
        //        EPixelInternalFormat.R8i => ESizedInternalFormat.R8i,
        //        EPixelInternalFormat.R8ui => ESizedInternalFormat.R8ui,
        //        EPixelInternalFormat.R16i => ESizedInternalFormat.R16i,
        //        EPixelInternalFormat.R16ui => ESizedInternalFormat.R16ui,
        //        EPixelInternalFormat.R32i => ESizedInternalFormat.R32i,
        //        EPixelInternalFormat.R32ui => ESizedInternalFormat.R32ui,
        //        EPixelInternalFormat.RG8i => ESizedInternalFormat.Rg8i,
        //        EPixelInternalFormat.RG8ui => ESizedInternalFormat.Rg8ui,
        //        EPixelInternalFormat.RG16i => ESizedInternalFormat.Rg16i,
        //        EPixelInternalFormat.RG16ui => ESizedInternalFormat.Rg16ui,
        //        EPixelInternalFormat.RG32i => ESizedInternalFormat.Rg32i,
        //        EPixelInternalFormat.RG32ui => ESizedInternalFormat.Rg32ui,
        //        EPixelInternalFormat.Rgb16f => ESizedInternalFormat.Rgb16f,
        //        EPixelInternalFormat.Rgb32f => ESizedInternalFormat.Rgb32f,
        //        EPixelInternalFormat.Rgba32f => ESizedInternalFormat.Rgba32f,
        //        EPixelInternalFormat.Rgba16f => ESizedInternalFormat.Rgba16f,
        //        EPixelInternalFormat.Rgba32ui => ESizedInternalFormat.Rgba32ui,
        //        EPixelInternalFormat.Rgba16ui => ESizedInternalFormat.Rgba16ui,
        //        EPixelInternalFormat.Rgba8ui => ESizedInternalFormat.Rgba8ui,
        //        EPixelInternalFormat.Rgba32i => ESizedInternalFormat.Rgba32i,
        //        EPixelInternalFormat.Rgba16i => ESizedInternalFormat.Rgba16i,
        //        EPixelInternalFormat.Rgba8i => ESizedInternalFormat.Rgba8i,
        //        _ => throw new ArgumentOutOfRangeException(nameof(internalFormat), internalFormat, null),
        //    };
    }
}