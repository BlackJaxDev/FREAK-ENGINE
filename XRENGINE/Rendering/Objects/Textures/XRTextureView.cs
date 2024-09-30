using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public abstract class XRTextureView<T>(
        T viewedTexture,
        int minLevel,
        int numLevels,
        int minLayer,
        int numLayers,
        EPixelInternalFormat internalFormat) 
        : XRTextureViewBase(minLevel, numLevels, minLayer, numLayers, internalFormat) where T : XRTexture
    {
        public T ViewedTexture { get; } = viewedTexture;
        public override XRTexture GetViewedTexture()
            => ViewedTexture;
    }

    public abstract class XRTextureViewBase(
        int minLevel,
        int numLevels,
        int minLayer,
        int numLayers,
        EPixelInternalFormat internalFormat) : XRTexture, IFrameBufferAttachement
    {
        public int MinLevel { get; set; } = minLevel;
        public int NumLevels { get; set; } = numLevels;
        public int MinLayer { get; set; } = minLayer;
        public int NumLayers { get; set; } = numLayers;
        public EPixelInternalFormat InternalFormat { get; set; } = internalFormat;

        public abstract XRTexture GetViewedTexture();

        //protected override void CreateRenderTexture()
        //{
        //    //base.CreateRenderTexture();
        //    //_texture.Generated += _texture_Generated;
        //    //_texture.PrePushData += _texture_PrePushData1;
        //    //_texture.PostPushData += _texture_PostPushData;
        //}

        private void _texture_PostPushData()
        {

        }

        //private void _texture_PrePushData1(PrePushDataCallback callback)
        //{
        //    callback.ShouldPush = false;
        //}

        //protected override void SetParameters()
        //{

        //}

        private void _texture_Generated()
        {
            //Textures.GLTexture vtex = _viewedTexture.GetRenderTextureGeneric(true);
            //Api.TextureView(
            //    BindingId, ETextureTarget.Texture2D, vtex.BindingId, InternalFormat,
            //    _minLevel, _numLevels, _minLayer, _numLayers);

            //_texture.Bind();
            //int dsmode = DepthStencilFormat == EDepthStencilFmt.Stencil ? 6401 : 6402;
            //Api.TexParameter(ETextureTarget.Texture2D, ETexParamName.DepthStencilTextureMode, dsmode);
            //Api.TexParameter(ETextureTarget.Texture2D, ETexParamName.TextureLodBias, LodBias);
            //Api.TexParameter(ETextureTarget.Texture2D, ETexParamName.TextureMagFilter, (int)MagFilter);
            //Api.TexParameter(ETextureTarget.Texture2D, ETexParamName.TextureMinFilter, (int)MinFilter);
            //Api.TexParameter(ETextureTarget.Texture2D, ETexParamName.TextureWrapS, (int)UWrap);
            //Api.TexParameter(ETextureTarget.Texture2D, ETexParamName.TextureWrapT, (int)VWrap);
        }

        public void AttachToFBO(XRFrameBuffer target, int mipLevel = 0)
        {
            throw new NotImplementedException();
        }

        public void DetachFromFBO(XRFrameBuffer target, int mipLevel = 0)
        {
            throw new NotImplementedException();
        }

        public void AttachToFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel = 0)
        {
            throw new NotImplementedException();
        }

        public void DetachFromFBO(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel = 0)
        {
            throw new NotImplementedException();
        }
    }
}
