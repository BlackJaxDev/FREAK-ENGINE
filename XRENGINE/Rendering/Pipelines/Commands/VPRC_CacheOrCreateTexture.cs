﻿namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_CacheOrCreateTexture(ViewportRenderCommandContainer pipeline) : ViewportRenderCommand(pipeline)
    {
        /// <summary>
        /// The name of the texture in the pipeline.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Factory method to create the texture when it is not cached.
        /// </summary>
        public required Func<XRTexture> TextureFactory { get; set; }

        /// <summary>
        /// This action is called when the texture is already cached.
        /// Cast the texture to the correct type and verify its size with this action.
        /// If true is returned, the texture will be recreated.
        /// </summary>
        public Func<XRTexture, bool>? NeedsRecreate { get; set; } = null;

        public Action<XRTexture>? Resize { get; set; } = null;

        public void SetOptions(string name, Func<XRTexture> factory, Func<XRTexture, bool>? needsRecreate, Action<XRTexture>? resize)
        {
            Name = name;
            TextureFactory = factory;
            NeedsRecreate = needsRecreate;
            Resize = resize;
        }

        protected override void Execute()
        {
            if (Pipeline.TryGetTexture(Name, out var texture) && (texture is null || !(NeedsRecreate?.Invoke(texture) ?? false)))
                return;

            if (texture is not null && texture.IsResizeable && Resize is not null)
                Resize.Invoke(texture);
            else
            {
                texture = TextureFactory();
                texture.Name = Name;
                Pipeline.SetTexture(texture);
            }
        }
    }
}
