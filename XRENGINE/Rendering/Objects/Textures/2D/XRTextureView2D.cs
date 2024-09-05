using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTextureView2D : XRTexture2D
    {
        private XRTexture2D _viewedTexture;
        private int _minLevel;
        private int _numLevels;
        private int _minLayer;
        private int _numLayers;

        public XRTextureView2D(
            XRTexture2D viewedTexture,
            int minLevel, 
            int numLevels,
            int minLayer, 
            int numLayers, 
            EPixelType type,
            EPixelFormat format,
            EPixelInternalFormat internalFormat)
        {
            if (viewedTexture is null)
                throw new InvalidOperationException("Viewed texture cannot be null.");

            _viewedTexture = viewedTexture;
            _minLevel = minLevel;
            _numLevels = numLevels;
            _minLayer = minLayer;
            _numLayers = numLayers;
            PixelType = type;
            PixelFormat = format;
            InternalFormat = internalFormat;
            _width = viewedTexture.Width;
            _height = viewedTexture.Height;
            MinFilter = viewedTexture.MinFilter;
            MagFilter = viewedTexture.MagFilter;
            _mipmaps = null;
        }
        
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
            //    _texture.BindingId, ETextureTarget.Texture2D, vtex.BindingId, InternalFormat,
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
    }
}
