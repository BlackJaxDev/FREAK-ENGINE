using ImageMagick;
using Silk.NET.OpenGL;
using XREngine.Data.Rendering;
using static XREngine.Rendering.OpenGL.OpenGLRenderer;

namespace XREngine.Rendering.OpenGL
{
    public class GLTexture2D(OpenGLRenderer renderer, XRTexture2D data) : GLTexture<XRTexture2D>(renderer, data)
    {
        private bool _isPushing = false;
        private bool _hasPushed = false;
        private bool _storageSet = false;

        public override ETextureTarget TextureTarget { get; } = ETextureTarget.Texture2D;

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
            Invalidate();
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
            if (_isPushing)
                return;
            try
            {
                _isPushing = true;
                OnPrePushData(out bool shouldPush, out bool allowPostPushCallback);
                if (!shouldPush)
                {
                    if (allowPostPushCallback)
                        OnPostPushData();
                    _isPushing = false;
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
                        GetFormat(Data.Mipmaps[0], Data.InternalCompression, out GLEnum sizedInternalFormat, out GLEnum pixelFormat, out GLEnum pixelType);
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
                _isPushing = false;
                Unbind();
            }
        }

        private unsafe void PushMipmap(GLEnum glTarget, int i, MagickImage? bmp)
        {
            //Api.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            if (bmp is null)
            {
                if (!_hasPushed && !_storageSet)
                    Api.TexImage2D(glTarget, i, (int)GLEnum.Rgb, Data.Width >> i, Data.Height >> i, 0, GLEnum.Rgb, GLEnum.Float, in IntPtr.Zero);
            }
            else
            {
                // If a non-zero named buffer object is bound to the GL_PIXEL_UNPACK_BUFFER target (see glBindBuffer) while a texture image is specified, data is treated as a byte offset into the buffer object's data store. 
                GetFormat(bmp, Data.InternalCompression, out GLEnum internalPixelFormat, out GLEnum pixelFormat, out GLEnum pixelType);
                var bytes = bmp.GetPixelsUnsafe().GetAreaPointer(0, 0, bmp.Width, bmp.Height).ToPointer();
                if (_hasPushed || _storageSet)
                    Api.TexSubImage2D(glTarget, i, 0, 0, bmp.Width, bmp.Height, pixelFormat, pixelType, bytes);
                else
                    Api.TexImage2D(glTarget, i, (int)internalPixelFormat, bmp.Width, bmp.Height, 0, pixelFormat, pixelType, bytes);

                var error = Api.GetError();
                if (error != GLEnum.NoError)
                    Debug.LogWarning($"Error pushing texture data: {error}");
            }
        }

        private unsafe void GetFormat(MagickImage bmp, bool internalCompression, out GLEnum internalPixelFormat, out GLEnum pixelFormat, out GLEnum pixelType)
        {
            //Internal format must match pixel format
            //GL_ALPHA, GL_LUMINANCE, GL_LUMINANCE_ALPHA, GL_RGB, GL_RGBA
            //bool hasAlpha = bmp.HasAlpha;
            uint channels = bmp.ChannelCount;
            bool signed = bmp.Settings.ColorSpace == ColorSpace.sRGB;
            uint depth = bmp.Depth; //8 is s/byte, 16 is u/short, 32 is float
            pixelType = depth switch
            {
                8 => signed ? GLEnum.Byte : GLEnum.UnsignedByte,
                16 => signed ? GLEnum.Short : GLEnum.UnsignedShort,
                32 => GLEnum.Float,
                _ => throw new NotSupportedException($"Unsupported pixel depth: {depth}"),
            };
            switch (channels)
            {
                case 1:
                    internalPixelFormat = internalCompression ? GLEnum.CompressedRed : GLEnum.Red;
                    pixelFormat = GLEnum.Red;
                    break;
                case 2:
                    internalPixelFormat = internalCompression ? GLEnum.CompressedRG : GLEnum.RG;
                    pixelFormat = GLEnum.RG;
                    break;
                case 3:
                    internalPixelFormat = internalCompression ? GLEnum.CompressedRgb : GLEnum.Rgb;
                    pixelFormat = GLEnum.Rgb;
                    break;
                default:
                case 4:
                    internalPixelFormat = internalCompression ? GLEnum.CompressedRgba : GLEnum.Rgba;
                    pixelFormat = GLEnum.Rgba;
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
            => samplerNameOverride ?? $"Texture{textureIndex}";
    }
}