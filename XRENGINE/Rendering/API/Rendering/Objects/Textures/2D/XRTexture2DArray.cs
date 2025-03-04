﻿using ImageMagick;
using XREngine.Data;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    [XR3rdPartyExtensions("gif")]
    public class XRTexture2DArray : XRTexture
    {
        private bool _multiSample;
        private XRTexture2D[] _textures = [];
        private bool _resizable = true;
        private ESizedInternalFormat _sizedInternalFormat = ESizedInternalFormat.Rgba8;

        public XRTexture2DArray(params XRTexture2D[] textures)
        {
            Textures = textures;
        }

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
        public override uint MaxDimension => Math.Max(Width, Height);
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
        public ESizedInternalFormat SizedInternalFormat
        {
            get => _sizedInternalFormat;
            set => SetField(ref _sizedInternalFormat, value);
        }

        public uint Width => Textures.Length > 0 ? Textures[0].Width : 0u;
        public uint Height => Textures.Length > 0 ? Textures[0].Height : 0u;
        public uint Depth => (uint)Textures.Length;

        public Mipmap2D[]? Mipmaps => Textures.Length > 0 ? Textures[0].Mipmaps : null;

        public class OVRMultiView(int offset, uint numViews)
        {
            public uint NumViews = numViews;
            public int Offset = offset;
        }

        private OVRMultiView? _ovrMultiViewParameters;
        public OVRMultiView? OVRMultiViewParameters
        {
            get => _ovrMultiViewParameters;
            set => SetField(ref _ovrMultiViewParameters, value);
        }

        public event Action? Resized = null;

        private void TextureResized()
        {
            Resized?.Invoke();
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Textures):
                        if (Textures != null)
                        {
                            foreach (XRTexture2D texture in Textures)
                                texture.Resized -= TextureResized;
                        }
                        break;
                }
            }
            return change;
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Textures):
                    if (Textures != null)
                    {
                        foreach (XRTexture2D texture in Textures)
                            texture.Resized += TextureResized;
                    }
                    break;
            }
        }

        protected override void Reload3rdParty(string path)
            => Load3rdParty(path);
        public override bool Load3rdParty(string filePath)
        {
            using MagickImageCollection collection = new(filePath);
            Textures = new XRTexture2D[collection.Count];
            for (int i = 0; i < collection.Count; i++)
                if (collection[i] is MagickImage mi)
                    Textures[i] = new(mi);
            AutoGenerateMipmaps = true;
            return true;
        }

        public delegate void DelAttachToFBO_OVRMultiView(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel, int offset, uint numViews);
        public delegate void DelDetachFromFBO_OVRMultiView(XRFrameBuffer target, EFrameBufferAttachment attachment, int mipLevel, int offset, uint numViews);

        public event DelAttachToFBO_OVRMultiView? AttachToFBORequested_OVRMultiView;
        public event DelDetachFromFBO_OVRMultiView? DetachFromFBORequested_OVRMultiView;

        public void AttachToFBO_OVRMultiView(XRFrameBuffer fbo, EFrameBufferAttachment attachment, int mipLevel, int offset, uint numViews)
            => AttachToFBORequested_OVRMultiView?.Invoke(fbo, attachment, mipLevel, offset, numViews);
        public void DetachFromFBO_OVRMultiView(XRFrameBuffer fbo, EFrameBufferAttachment attachment, int mipLevel, int offset, uint numViews)
            => DetachFromFBORequested_OVRMultiView?.Invoke(fbo, attachment, mipLevel, offset, numViews);
    }
}