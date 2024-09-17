using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTextureCubeArrayView(
        XRTextureCubeArray viewedTexture,
        int minLevel,
        int numLevels,
        int minLayer,
        int numLayers,
        EPixelInternalFormat internalFormat,
        bool array,
        bool view2D) : XRTextureView<XRTextureCubeArray>(viewedTexture, minLevel, numLevels, minLayer, numLayers, internalFormat)
    {
        private bool _array = array;
        public bool Array
        {
            get => _array;
            set => SetField(ref _array, value);
        }
        private bool _view2D = view2D;
        public bool View2D
        {
            get => _view2D;
            set => SetField(ref _view2D, value);
        }
        public override uint MaxDimension { get; } = 2u;
    }
}
