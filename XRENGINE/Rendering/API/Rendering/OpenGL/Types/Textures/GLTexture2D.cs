using Extensions;
using ImageMagick;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
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
            base.UnlinkData();

            Data.Resized -= DataResized;
        }
        protected override void LinkData()
        {
            base.LinkData();

            Data.Resized += DataResized;
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
            //Invalidate();
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

                Api.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                bool setStorage = !Data.Resizable && !_storageSet;
                if (setStorage)
                {
                    //TODO: convert internal to sized using pixel format, update ToGLEnum
                    GLEnum sizedInternalFormat = ToGLEnum(Data.SizedInternalFormat);
                    Api.TexStorage2D(glTarget, Math.Max((uint)Data.Mipmaps.Length, 1u), sizedInternalFormat, Data.Width, Data.Height);
                    _storageSet = true;
                }

                if (!_hasPushed)
                {
                    if (Data.Mipmaps is null || Data.Mipmaps.Length == 0)
                        PushMipmap(glTarget, 0, null);
                    else
                    {
                        for (int i = 0; i < Data.Mipmaps.Length; ++i)
                            PushMipmap(glTarget, i, Data.Mipmaps[i]);

                        if (Data.AutoGenerateMipmaps)
                            GenerateMipmaps();
                    }
                    _hasPushed = true;
                }

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

        private unsafe void PushMipmap(GLEnum glTarget, int i, Mipmap? mip)
        {
            if (!Data.Resizable && !_storageSet)
            {
                Debug.LogWarning("Texture storage not set on non-resizable texture, can't push mipmaps.");
                return;
            }

            GLEnum pixelFormat;
            GLEnum pixelType;
            InternalFormat internalPixelFormat;

            MagickImage? bmp;
            if (mip is null)
            {
                internalPixelFormat = InternalFormat.Rgb;
                pixelFormat = GLEnum.Rgb;
                pixelType = GLEnum.UnsignedByte;
                bmp = null;
            }
            else
            {
                pixelFormat = ToGLEnum(mip.PixelFormat);
                pixelType = ToGLEnum(mip.PixelType);
                internalPixelFormat = ToInternalFormat(mip.InternalFormat);
                bmp = mip.Image;
            }

            if (bmp is null)
                Push(glTarget, i, Data.Width >> i, Data.Height >> i, pixelFormat, pixelType, internalPixelFormat);
            else
                Push(glTarget, i, mip!.Width, mip.Height, pixelFormat, pixelType, internalPixelFormat, bmp);
        }

        private unsafe void Push(GLEnum glTarget, int i, uint w, uint h, GLEnum pixelFormat, GLEnum pixelType, InternalFormat internalPixelFormat)
        {
            // If a non-zero named buffer object is bound to the GL_PIXEL_UNPACK_BUFFER target (see glBindBuffer) while a texture image is specified,
            // the data ptr is treated as a byte offset into the buffer object's data store. 

            if (_hasPushed || !Data.Resizable)
                Api.TexSubImage2D(glTarget, i, 0, 0, w, h, pixelFormat, pixelType, null);
            else
                Api.TexImage2D(glTarget, i, internalPixelFormat, w, h, 0, pixelFormat, pixelType, null);
            
            var error = Api.GetError();
            if (error != GLEnum.NoError)
                Debug.LogWarning($"Error pushing texture data: {error}");
        }
        private unsafe void Push(GLEnum glTarget, int i, uint w, uint h, GLEnum pixelFormat, GLEnum pixelType, InternalFormat internalPixelFormat, MagickImage bmp)
        {
            // If a non-zero named buffer object is bound to the GL_PIXEL_UNPACK_BUFFER target (see glBindBuffer) while a texture image is specified,
            // the data ptr is treated as a byte offset into the buffer object's data store. 
            var pBytes = bmp.GetPixels().ToByteArray(PixelMapping.RGB);
            fixed (byte* pBytesPtr = pBytes)
            {
                if (_hasPushed || !Data.Resizable)
                    Api.TexSubImage2D(glTarget, i, 0, 0, w, h, pixelFormat, pixelType, pBytesPtr);
                else
                    Api.TexImage2D(glTarget, i, internalPixelFormat, w, h, 0, pixelFormat, pixelType, pBytesPtr);
            }

            var error = Api.GetError();
            if (error != GLEnum.NoError)
                Debug.LogWarning($"Error pushing texture data: {error}");
        }

        private static InternalFormat ToInternalFormat(EPixelInternalFormat internalFormat)
            => (InternalFormat)internalFormat.ConvertByName(typeof(InternalFormat));

        private static ESizedInternalFormat ToSizedInternalFormat(EPixelInternalFormat internalFormat)
            => internalFormat switch
            {
                EPixelInternalFormat.Rgb8 => ESizedInternalFormat.Rgb8,
                EPixelInternalFormat.Rgba8 => ESizedInternalFormat.Rgba8,
                EPixelInternalFormat.Rgba16 => ESizedInternalFormat.Rgba16,
                EPixelInternalFormat.R8 => ESizedInternalFormat.R8,
                EPixelInternalFormat.R16 => ESizedInternalFormat.R16,
                EPixelInternalFormat.RG8 => ESizedInternalFormat.Rg8,
                EPixelInternalFormat.RG16 => ESizedInternalFormat.Rg16,
                EPixelInternalFormat.R16f => ESizedInternalFormat.R16f,
                EPixelInternalFormat.R32f => ESizedInternalFormat.R32f,
                EPixelInternalFormat.RG16f => ESizedInternalFormat.Rg16f,
                EPixelInternalFormat.RG32f => ESizedInternalFormat.Rg32f,
                EPixelInternalFormat.R8i => ESizedInternalFormat.R8i,
                EPixelInternalFormat.R8ui => ESizedInternalFormat.R8ui,
                EPixelInternalFormat.R16i => ESizedInternalFormat.R16i,
                EPixelInternalFormat.R16ui => ESizedInternalFormat.R16ui,
                EPixelInternalFormat.R32i => ESizedInternalFormat.R32i,
                EPixelInternalFormat.R32ui => ESizedInternalFormat.R32ui,
                EPixelInternalFormat.RG8i => ESizedInternalFormat.Rg8i,
                EPixelInternalFormat.RG8ui => ESizedInternalFormat.Rg8ui,
                EPixelInternalFormat.RG16i => ESizedInternalFormat.Rg16i,
                EPixelInternalFormat.RG16ui => ESizedInternalFormat.Rg16ui,
                EPixelInternalFormat.RG32i => ESizedInternalFormat.Rg32i,
                EPixelInternalFormat.RG32ui => ESizedInternalFormat.Rg32ui,
                EPixelInternalFormat.Rgb16f => ESizedInternalFormat.Rgb16f,
                EPixelInternalFormat.Rgb32f => ESizedInternalFormat.Rgb32f,
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
    }
}