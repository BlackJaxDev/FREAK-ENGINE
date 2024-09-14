using ImageMagick;
using Silk.NET.OpenGL;
using XREngine.Data.Rendering;
using static XREngine.Rendering.OpenGL.OpenGLRenderer;

namespace XREngine.Rendering.OpenGL
{
    public class GLTexture2D(OpenGLRenderer renderer, XRTexture2D data) : GLTexture<XRTexture2D>(renderer, data)
    {
        private bool _hasPushed = false;
        private bool _storageSet = false;

        public override ETextureTarget TextureTarget { get; }

        protected override void UnlinkData()
        {
            Data.Resized -= DataResized;
            base.UnlinkData();
        }
        protected override void LinkData()
        {
            Data.Resized += DataResized;
            base.LinkData();
        }

        private void DataResized()
        {
            _storageSet = false;
            _hasPushed = false;

            //Destroy();
            //Generate();
            Invalidate();
        }

        protected internal override void PostGenerated()
        {
            _hasPushed = false;
            _storageSet = false;
            base.PostGenerated();
        }
        protected internal override void PostDeleted()
        {
            _hasPushed = false;
            _storageSet = false;
            base.PostDeleted();
        }

        //TODO: use PBO per texture for quick data updates
        public override unsafe void PushData()
        {
            try
            {
                OnPrePushData(out bool shouldPush, out bool allowPostPushCallback);
                if (!shouldPush)
                {
                    if (allowPostPushCallback)
                        OnPostPushData();
                    return;
                }

                Bind();

                var glTarget = ToGLEnum(TextureTarget);

                if (Data.Mipmaps is null || Data.Mipmaps.Length == 0)
                    PushMipmap(glTarget, 0, null);
                else
                {
                    if (!Data.Resizable && !_storageSet)
                    {
                        GetFormat(Data.Mipmaps[0], out _, out GLEnum sizedInternalFormat);
                        Api.TexStorage2D(glTarget, (uint)Data.Mipmaps.Length, sizedInternalFormat, Data.Width, Data.Height);
                        _storageSet = true;
                    }

                    for (int i = 0; i < Data.Mipmaps.Length; ++i)
                        PushMipmap(glTarget, i, Data.Mipmaps[i]);

                    if (Data.AutoGenerateMipmaps)
                        GenerateMipmaps();
                }
                _hasPushed = true;

                //int max = _mipmaps is null || _mipmaps.Length == 0 ? 0 : _mipmaps.Length - 1;
                //Api.TexParameter(TextureTarget, ETexParamName.TextureBaseLevel, 0);
                //Api.TexParameter(TextureTarget, ETexParamName.TextureMaxLevel, max);
                //Api.TexParameter(TextureTarget, ETexParamName.TextureMinLod, 0);
                //Api.TexParameter(TextureTarget, ETexParamName.TextureMaxLod, max);

                if (allowPostPushCallback)
                    OnPostPushData();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {

            }
        }

        private unsafe void PushMipmap(GLEnum glTarget, int i, MagickImage? bmp)
        {
            if (bmp is null)
            {
                if (!_hasPushed && !_storageSet)
                    Api.TexImage2D(glTarget, i, (int)GLEnum.Rgb, Data.Width, Data.Height, 0, GLEnum.Rgb, GLEnum.UnsignedByte, in IntPtr.Zero);
                return;
            }

            GetFormat(bmp, out GLEnum pixelFormat, out GLEnum internalFormat);

            var bytes = bmp.GetPixelsUnsafe().ToByteArray(bmp.HasAlpha ? PixelMapping.RGBA : PixelMapping.RGB);
            //GCHandle pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            //IntPtr pointer = pinnedArray.AddrOfPinnedObject();
            //Using fixed avoids a secondary allocation
            fixed (byte* ptr = bytes)
            {
                if (_hasPushed || _storageSet)
                    Api.TexSubImage2D(glTarget, i, 0, 0, (uint)bmp.Width, (uint)bmp.Height, pixelFormat, GLEnum.Float, ptr);
                else
                    Api.TexImage2D(glTarget, i, (int)internalFormat, (uint)bmp.Width, (uint)bmp.Height, 0, pixelFormat, GLEnum.Float, ptr);
            }
            //pinnedArray.Free();
        }

        private unsafe void GetFormat(MagickImage bmp, out GLEnum pixelFormat, out GLEnum internalFormat)
        {
            //Internal format must match pixel format
            //GL_ALPHA, GL_LUMINANCE, GL_LUMINANCE_ALPHA, GL_RGB, GL_RGBA
            //bool hasAlpha = bmp.HasAlpha;
            uint channels = bmp.ChannelCount;
            switch (channels)
            {
                case 1:
                    pixelFormat = GLEnum.Red;
                    internalFormat = Data.PixelType switch
                    {
                        EPixelType.HalfFloat => GLEnum.R16f,
                        EPixelType.Int => GLEnum.R32i,
                        EPixelType.UnsignedInt => GLEnum.R32ui,
                        EPixelType.Short => GLEnum.R16i,
                        EPixelType.UnsignedShort => GLEnum.R16ui,
                        EPixelType.Byte => GLEnum.R8i,
                        EPixelType.UnsignedByte => GLEnum.R8ui,
                        _ => GLEnum.R32f,
                    };
                    break;
                case 2:
                    pixelFormat = GLEnum.RG;
                    internalFormat = Data.PixelType switch
                    {
                        EPixelType.HalfFloat => GLEnum.RG16f,
                        EPixelType.Int => GLEnum.RG32i,
                        EPixelType.UnsignedInt => GLEnum.RG32ui,
                        EPixelType.Short => GLEnum.RG16i,
                        EPixelType.UnsignedShort => GLEnum.RG16ui,
                        EPixelType.Byte => GLEnum.RG8i,
                        EPixelType.UnsignedByte => GLEnum.RG8ui,
                        _ => GLEnum.RG32f,
                    };
                    break;
                case 3:
                    pixelFormat = GLEnum.Rgb;
                    internalFormat = Data.PixelType switch
                    {
                        EPixelType.HalfFloat => GLEnum.Rgb16f,
                        EPixelType.Int => GLEnum.Rgb32i,
                        EPixelType.UnsignedInt => GLEnum.Rgb32ui,
                        EPixelType.Short => GLEnum.Rgb16i,
                        EPixelType.UnsignedShort => GLEnum.Rgb16ui,
                        EPixelType.Byte => GLEnum.Rgb8i,
                        EPixelType.UnsignedByte => GLEnum.Rgb8ui,
                        _ => GLEnum.Rgb32f,
                    };
                    break;
                default:
                case 4:
                    pixelFormat = GLEnum.Rgba;
                    internalFormat = Data.PixelType switch
                    {
                        EPixelType.HalfFloat => GLEnum.Rgba16f,
                        EPixelType.Int => GLEnum.Rgba32i,
                        EPixelType.UnsignedInt => GLEnum.Rgba32ui,
                        EPixelType.Short => GLEnum.Rgba16i,
                        EPixelType.UnsignedShort => GLEnum.Rgba16ui,
                        EPixelType.Byte => GLEnum.Rgba8i,
                        EPixelType.UnsignedByte => GLEnum.Rgba8ui,
                        _ => GLEnum.Rgba32f,
                    };
                    break;
            }
        }

        private static ESizedInternalFormat ToSizedInternalFormat(EPixelInternalFormat internalFormat)
            => internalFormat switch
            {
                EPixelInternalFormat.Rgba8 => ESizedInternalFormat.Rgba8,
                EPixelInternalFormat.Rgba16 => ESizedInternalFormat.Rgba16,
                EPixelInternalFormat.R8 => ESizedInternalFormat.R8,
                EPixelInternalFormat.R16 => ESizedInternalFormat.R16,
                EPixelInternalFormat.Rg8 => ESizedInternalFormat.Rg8,
                EPixelInternalFormat.Rg16 => ESizedInternalFormat.Rg16,
                EPixelInternalFormat.R16f => ESizedInternalFormat.R16f,
                EPixelInternalFormat.R32f => ESizedInternalFormat.R32f,
                EPixelInternalFormat.Rg16f => ESizedInternalFormat.Rg16f,
                EPixelInternalFormat.Rg32f => ESizedInternalFormat.Rg32f,
                EPixelInternalFormat.R8i => ESizedInternalFormat.R8i,
                EPixelInternalFormat.R8ui => ESizedInternalFormat.R8ui,
                EPixelInternalFormat.R16i => ESizedInternalFormat.R16i,
                EPixelInternalFormat.R16ui => ESizedInternalFormat.R16ui,
                EPixelInternalFormat.R32i => ESizedInternalFormat.R32i,
                EPixelInternalFormat.R32ui => ESizedInternalFormat.R32ui,
                EPixelInternalFormat.Rg8i => ESizedInternalFormat.Rg8i,
                EPixelInternalFormat.Rg8ui => ESizedInternalFormat.Rg8ui,
                EPixelInternalFormat.Rg16i => ESizedInternalFormat.Rg16i,
                EPixelInternalFormat.Rg16ui => ESizedInternalFormat.Rg16ui,
                EPixelInternalFormat.Rg32i => ESizedInternalFormat.Rg32i,
                EPixelInternalFormat.Rg32ui => ESizedInternalFormat.Rg32ui,
                EPixelInternalFormat.Rgba32f => ESizedInternalFormat.Rgba32f,
                EPixelInternalFormat.Rgba16f => ESizedInternalFormat.Rgba16f,
                EPixelInternalFormat.Rgba32ui => ESizedInternalFormat.Rgba32ui,
                EPixelInternalFormat.Rgba16ui => ESizedInternalFormat.Rgba16ui,
                EPixelInternalFormat.Rgba8ui => ESizedInternalFormat.Rgba8ui,
                EPixelInternalFormat.Rgba32i => ESizedInternalFormat.Rgba32i,
                EPixelInternalFormat.Rgba16i => ESizedInternalFormat.Rgba16i,
                EPixelInternalFormat.Rgba8i => ESizedInternalFormat.Rgba8i,
                _ => throw new ArgumentOutOfRangeException(nameof(internalFormat), internalFormat, null),
            };

        public override string ResolveSamplerName(int textureIndex, string? samplerNameOverride)
        {
            return samplerNameOverride ?? $"tex2d_{textureIndex}";
        }
    }
}