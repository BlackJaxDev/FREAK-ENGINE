using XREngine.Data.Rendering;
using XREngine.Rendering.OpenGL;
using static XREngine.Rendering.OpenGL.OpenGLRenderer;

namespace XREngine.Rendering.Models.Materials.Textures
{
    public class GLTextureCube(OpenGLRenderer renderer, XRTextureCube data) : GLTexture<XRTextureCube>(renderer, data)
    {
        private CubeMipmap[]? _mipmaps;

        private bool _hasPushed = false;
        private bool _storageSet = false;

        public override ETextureTarget TextureTarget => ETextureTarget.TextureCubeMap;

        public CubeMipmap[]? Mipmaps
        {
            get => _mipmaps;
            set => _mipmaps = value;
        }

        protected override void UnlinkData()
        {
            base.UnlinkData();

            Data.AttachFaceToFBORequested -= AttachFaceToFBO;
            Data.DetachFaceFromFBORequested -= DetachFaceFromFBO;
        }

        protected override void LinkData()
        {
            base.LinkData();

            Data.AttachFaceToFBORequested += AttachFaceToFBO;
            Data.DetachFaceFromFBORequested += DetachFaceFromFBO;
        }

        private void DetachFaceFromFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, ECubemapFace face, int mipLevel)
        {
            if (Renderer.GetOrCreateAPIRenderObject(target) is not GLObjectBase glObj)
                return;

            Api.NamedFramebufferTextureLayer(glObj.BindingId, ToGLEnum(attachment), BindingId, mipLevel, 0);
        }

        public void AttachFaceToFBO(XRFrameBuffer fbo, EFrameBufferAttachment attachment, ECubemapFace face, int mipLevel)
        {
            if (Renderer.GetOrCreateAPIRenderObject(fbo) is not GLObjectBase glObj)
                return;

            Api.NamedFramebufferTextureLayer(glObj.BindingId, ToGLEnum(attachment), 0, mipLevel, (int)face);
        }

        public unsafe override void PushData()
        {
            OnPrePushData(out bool shouldPush, out bool allowPostPushCallback);
            if (!shouldPush)
            {
                if (allowPostPushCallback)
                    OnPostPushData();
                return;
            }

            Bind();

            //TODO: copy from teximage2d
            //ESizedInternalFormat sizedInternalFormat = (ESizedInternalFormat)(int)InternalFormat;

            if (_mipmaps is null || _mipmaps.Length == 0)
            {
                //int mipCount = SmallestMipmapLevel + 1;
                //if (!Resizable && !_storageSet)
                //{
                //    Api.SetTextureStorage(BindingId, mipCount, sizedInternalFormat, _dimension, _dimension);
                //    _storageSet = true;
                //}
                //else if (!_storageSet)
                //{
                //    int dim = _dimension;
                //    for (int i = 0; i < mipCount; ++i)
                //    {
                //        for (int x = 0; x < 6; ++x)
                //        {
                //            ETextureTarget target = ETextureTarget.TextureCubeMapPositiveX + x;
                //            Api.PushTextureData(target, i, InternalFormat, dim, dim, PixelFormat, PixelType, IntPtr.Zero);
                //        }
                //        dim /= 2;
                //    }
                //}

                //if (Data.AutoGenerateMipmaps)
                //    GenerateMipmaps();
            }
            else
            {
                //bool setStorage = !Data.Resizable && !_storageSet;
                //if (setStorage)
                //{
                //    Api.TextureStorage2D(
                //        BindingId,
                //        _mipmaps.Length,
                //        sizedInternalFormat,
                //        _mipmaps[0].Sides[0].Width,
                //        _mipmaps[0].Sides[0].Height);

                //    _storageSet = true;
                //}

                //for (int i = 0; i < _mipmaps.Length; ++i)
                //{
                //    TextureCubeMipmap mip = _mipmaps[i];
                //    for (int x = 0; x < mip.Sides.Length; ++x)
                //    {
                //        RenderCubeSide side = mip.Sides[x];
                //        MagickImage? bmp = side.Map;
                //        ETextureTarget target = ETextureTarget.TextureCubeMapPositiveX + x;
                //        GLEnum glTarget = ToGLEnum(target);

                //        if (bmp != null)
                //        {
                //            //get the pointer to the imagemagick data
                //            var data = bmp.GetPixelsUnsafe();
                //            nint ptr = data.GetAreaPointer(0, 0, bmp.Width, bmp.Height);

                //            if (_hasPushed || setStorage)
                //                Api.TexSubImage2D(glTarget, i, 0, 0, bmp.Width, bmp.Height, side.PixelFormat, side.PixelType, ptr);
                //            else
                //                Api.TexImage2D(glTarget, i, side.InternalFormat, bmp.Width, bmp.Height, side.PixelFormat, side.PixelType, data.Scan0);
                //        }
                //        else if (!_hasPushed && !setStorage)
                //            Api.TexImage2D(glTarget, i, side.InternalFormat, side.Width, side.Height, side.PixelFormat, side.PixelType, IntPtr.Zero);
                //    }
                //}
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

        private bool _disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Destroy();
                }

                if (_mipmaps != null)
                    Array.ForEach(_mipmaps, x => x.Dispose());
                
                _disposedValue = true;
            }
        }
    }
}
