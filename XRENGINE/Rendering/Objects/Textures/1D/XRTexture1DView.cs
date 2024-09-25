﻿using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTexture1DView(
        XRTexture1D viewedTexture,
        int minLevel,
        int numLevels,
        int minLayer,
        int numLayers,
        EPixelInternalFormat internalFormat,
        bool array) : XRTextureView<XRTexture1D>(viewedTexture, minLevel, numLevels, minLayer, numLayers, internalFormat)
    {
        private bool _array = array;
        public bool Array 
        {
            get => _array;
            set => SetField(ref _array, value);
        }
        public override uint MaxDimension { get; } = 1u;
    }
}