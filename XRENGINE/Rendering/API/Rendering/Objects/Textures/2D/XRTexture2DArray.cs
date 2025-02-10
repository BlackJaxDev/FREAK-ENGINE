using ImageMagick;
using XREngine.Data;

namespace XREngine.Rendering
{
    [XR3rdPartyExtensions("gif")]
    public class XRTexture2DArray : XRTexture
    {
        private bool _multiSample;
        private XRTexture2D[] _textures = [];
        private bool _resizable = true;

        public override bool IsResizeable => Resizable;

        /// <summary>
        /// If false, calling resize will do nothing.
        /// Useful for repeating textures that must always be a certain size or textures that never need to be dynamically resized during the game.
        /// False by default.
        /// </summary>
        public bool Resizable
        {
            get => _resizable;
            set => SetField(ref _resizable, value);
        }
        public override uint MaxDimension { get; } = 2u;
        public bool MultiSample
        {
            get => _multiSample;
            set => SetField(ref _multiSample, value);
        }
        public XRTexture2D[] Textures
        {
            get => _textures;
            set => SetField(ref _textures, value);
        }

        protected override void Reload3rdParty(string path)
        {
            Load3rdParty(path);
        }
        public override bool Load3rdParty(string filePath)
        {
            using MagickImageCollection collection = new(filePath);
            Textures = new XRTexture2D[collection.Count];
            for (int i = 0; i < collection.Count; i++)
            {
                IMagickImage<float>? image = collection[i];
                if (image is null)
                    continue;

                XRTexture2D texture = new(image as MagickImage);
                Textures[i] = texture;
            }
            AutoGenerateMipmaps = true;
            return true;
        }
    }
}