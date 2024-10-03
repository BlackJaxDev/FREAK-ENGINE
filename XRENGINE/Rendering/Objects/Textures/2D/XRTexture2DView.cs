using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTexture2DView(
        XRTexture2D viewedTexture,
        int minLevel,
        int numLevels,
        int minLayer,
        int numLayers,
        EPixelInternalFormat internalFormat,
        bool array,
        bool multisample) : XRTextureView<XRTexture2D>(viewedTexture, minLevel, numLevels, minLayer, numLayers, internalFormat), IFrameBufferAttachement
    {
        public override uint MaxDimension { get; } = 2;

        private bool _array = array;
        public bool Array
        {
            get => _array;
            set => SetField(ref _array, value);
        }
        private bool _multisample = multisample;
        public bool Multisample 
        {
            get => _multisample;
            set => SetField(ref _multisample, value);
        }

        private EDepthStencilFmt _depthStencilFormat = EDepthStencilFmt.None;
        public EDepthStencilFmt DepthStencilFormat 
        {
            get => _depthStencilFormat;
            set => SetField(ref _depthStencilFormat, value);
        }

        public uint Width => ViewedTexture.Width;
        public uint Height => ViewedTexture.Height;
    }
}
