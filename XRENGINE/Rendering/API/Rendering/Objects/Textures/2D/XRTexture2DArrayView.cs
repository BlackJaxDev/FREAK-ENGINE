using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTexture2DArrayView(
        XRTexture2DArray viewedTexture,
        uint minLevel,
        uint numLevels,
        uint minLayer,
        uint numLayers,
        EPixelInternalFormat internalFormat,
        bool view2D,
        bool multisample) : XRTextureView<XRTexture2DArray>(viewedTexture, minLevel, numLevels, minLayer, numLayers, internalFormat)
    {
        private bool _view2D = view2D;
        public bool View2D
        {
            get => _view2D;
            set => SetField(ref _view2D, value);
        }
        private bool _multisample = multisample;
        public bool Multisample
        {
            get => _multisample;
            set => SetField(ref _multisample, value);
        }
        public override uint MaxDimension { get; } = 2u;
    }
}
