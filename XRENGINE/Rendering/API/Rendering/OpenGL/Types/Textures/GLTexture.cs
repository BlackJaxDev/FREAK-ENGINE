using Silk.NET.OpenGL;
using System.ComponentModel;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using static XREngine.Rendering.OpenGL.OpenGLRenderer;

namespace XREngine.Rendering.OpenGL
{
    public abstract class GLTexture<T>(OpenGLRenderer renderer, T data) : GLObject<T>(renderer, data), IGLTexture where T : XRTexture
    {
        public override GLObjectType Type => GLObjectType.Texture;

        public XREvent<PreBindCallback> PreBind;
        public XREvent<PrePushDataCallback> PrePushData;
        public XREvent<GLTexture<T>> PostPushData;

        private bool _isPushing = false;
        public bool IsPushing
        {
            get => _isPushing;
            protected set => SetField(ref _isPushing, value);
        }

        protected override void UnlinkData()
        {
            Data.AttachToFBORequested -= AttachToFBO;
            Data.DetachFromFBORequested -= DetachFromFBO;
            Data.PushDataRequested -= PushData;
            Data.BindRequested -= Bind;
            Data.UnbindRequested -= Unbind;
            Data.ClearRequested -= Clear;
            Data.GenerateMipmapsRequested -= GenerateMipmaps;
            Data.PropertyChanged -= DataPropertyChanged;
            Data.PropertyChanging -= DataPropertyChanging;
        }

        protected override void LinkData()
        {
            Data.AttachToFBORequested += AttachToFBO;
            Data.DetachFromFBORequested += DetachFromFBO;
            Data.PushDataRequested += PushData;
            Data.BindRequested += Bind;
            Data.UnbindRequested += Unbind;
            Data.ClearRequested += Clear;
            Data.GenerateMipmapsRequested += GenerateMipmaps;
            Data.PropertyChanged += DataPropertyChanged;
            Data.PropertyChanging += DataPropertyChanging;
        }

        protected virtual void DataPropertyChanging(object? sender, PropertyChangingEventArgs e)
        {

        }

        protected virtual void DataPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(XRTexture.MinLOD):
                    {
                        int param = Data.MinLOD;
                        Api.TextureParameterI(BindingId, TextureParameterName.TextureMinLod, ref param);
                        break;
                    }
                case nameof(XRTexture.MaxLOD):
                    {
                        int param = Data.MaxLOD;
                        Api.TextureParameterI(BindingId, TextureParameterName.TextureMaxLod, ref param);
                        break;
                    }
                case nameof(XRTexture.LargestMipmapLevel):
                    {
                        int param = Data.LargestMipmapLevel;
                        Api.TextureParameterI(BindingId, TextureParameterName.TextureBaseLevel, ref param);
                        break;
                    }
                case nameof(XRTexture.SmallestAllowedMipmapLevel):
                    {
                        int param = Data.SmallestAllowedMipmapLevel;
                        Api.TextureParameterI(BindingId, TextureParameterName.TextureMaxLevel, ref param);
                        break;
                    }
            }
        }

        protected virtual void SetParameters()
        {
            int param = Data.MinLOD;
            Api.TextureParameterI(BindingId, TextureParameterName.TextureMinLod, ref param);

            param = Data.MaxLOD;
            Api.TextureParameterI(BindingId, TextureParameterName.TextureMaxLod, ref param);

            param = Data.LargestMipmapLevel;
            Api.TextureParameterI(BindingId, TextureParameterName.TextureBaseLevel, ref param);

            param = Data.SmallestAllowedMipmapLevel;
            Api.TextureParameterI(BindingId, TextureParameterName.TextureMaxLevel, ref param);
        }

        protected virtual bool OnPreBind()
        {
            if (Renderer.BoundTexture == this)
                return false;

            PreBindCallback callback = new();
            PreBind.Invoke(callback);
            return callback.ShouldBind;
        }

        protected virtual void OnPrePushData(out bool shouldPush, out bool allowPostPushCallback)
        {
            PrePushDataCallback callback = new();
            PrePushData.Invoke(callback);
            shouldPush = callback.ShouldPush;
            allowPostPushCallback = callback.AllowPostPushCallback;
        }

        protected virtual void OnPostPushData()
            => PostPushData.Invoke(this);

        public abstract ETextureTarget TextureTarget { get; }

        /// <summary>
        /// If true, this texture's data has been updated and needs to be pushed to the GPU.
        /// </summary>
        /// <returns></returns>
        public bool IsInvalidated { get; private set; } = true;
        /// <summary>
        /// Informs the renderer that this texture's data has been updated and needs to be pushed to the GPU.
        /// </summary>
        /// <returns></returns>
        public void Invalidate() => IsInvalidated = true;

        public virtual void Bind()
        {
            uint id = BindingId;
            if (id == InvalidBindingId)
                return;

            if (!OnPreBind())
                return;

            Api.BindTexture(ToGLEnum(TextureTarget), id);
            Renderer.BoundTexture = this;

            SetParameters();

            if (IsInvalidated && !IsPushing)
            {
                IsInvalidated = false;
                PushData();
            }
        }

        public void Unbind()
        {
            if (Renderer.BoundTexture != this)
                return;
            
            Renderer.BoundTexture = null;
            Api.BindTexture(ToGLEnum(TextureTarget), 0);
        }

        public void Clear(ColorF4 color, int level = 0)
        {
            var id = BindingId;
            if (id != 0)
            {
                //Api.BindTexture(ToGLEnum(TextureTarget), id);
                Renderer.ClearTexImage(id, level, color);
            }
        }

        public void GenerateMipmaps()
            => Api.GenerateTextureMipmap(BindingId);

        protected override uint CreateObject()
            => Api.GenTexture();

        protected internal override void PostGenerated()
            => Invalidate();

        public void AttachToFBO(XRFrameBuffer fbo, EFrameBufferAttachment attachment, int mipLevel = 0)
            => Api.NamedFramebufferTexture(Renderer.GenericToAPI<GLFrameBuffer>(fbo)!.BindingId, ToGLEnum(attachment), BindingId, mipLevel);
        public void DetachFromFBO(XRFrameBuffer fbo, EFrameBufferAttachment attachment, int mipLevel = 0)
            => Api.NamedFramebufferTexture(Renderer.GenericToAPI<GLFrameBuffer>(fbo)!.BindingId, ToGLEnum(attachment), 0, mipLevel);

        public abstract void PushData();
        public string ResolveSamplerName(int textureIndex, string? samplerNameOverride)
            => Data.ResolveSamplerName(textureIndex, samplerNameOverride);
    }
}