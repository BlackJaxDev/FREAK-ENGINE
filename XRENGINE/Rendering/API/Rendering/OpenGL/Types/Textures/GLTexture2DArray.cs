using Silk.NET.OpenGL;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.OpenGL
{
    public class GLTexture2DArray(OpenGLRenderer renderer, XRTexture2DArray data) : GLTexture<XRTexture2DArray>(renderer, data)
    {
        public override ETextureTarget TextureTarget { get; } = ETextureTarget.Texture2DArray;

        private bool _storageSet = false;

        public override void PushData()
        {
            if (IsPushing)
                return;
            try
            {
                IsPushing = true;
                //Debug.Out($"Pushing texture: {GetDescribingName()}");
                OnPrePushData(out bool shouldPush, out bool allowPostPushCallback);
                if (!shouldPush)
                {
                    if (allowPostPushCallback)
                        OnPostPushData();
                    IsPushing = false;
                    return;
                }
                Bind();
                var glTarget = ToGLEnum(TextureTarget);
                Api.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
                //EPixelInternalFormat? internalFormatForce = null;
                if (!Data.Resizable && !_storageSet)
                {
                    //Api.TextureStorage3D(BindingId, (uint)Data.SmallestMipmapLevel, ToGLEnum(Data.SizedInternalFormat), Data.Width, Data.Height, Data.Depth);
                    //internalFormatForce = ToBaseInternalFormat(Data.SizedInternalFormat);
                    _storageSet = true;
                }
                //if (Mipmaps is null || Mipmaps.Length == 0)
                //    PushMipmap(glTarget, 0, null, internalFormatForce);
                //else
                //{
                //    for (int i = 0; i < Mipmaps.Length; ++i)
                //        PushMipmap(glTarget, i, Mipmaps[i], internalFormatForce);
                //}
                int baseLevel = 0;
                int maxLevel = 1000;
                int minLOD = -1000;
                int maxLOD = 1000;
                Api.TextureParameterI(BindingId, GLEnum.TextureBaseLevel, in baseLevel);
                Api.TextureParameterI(BindingId, GLEnum.TextureMaxLevel, in maxLevel);
                Api.TextureParameterI(BindingId, GLEnum.TextureMinLod, in minLOD);
                Api.TextureParameterI(BindingId, GLEnum.TextureMaxLod, in maxLOD);
                if (Data.AutoGenerateMipmaps)
                    GenerateMipmaps();
                if (allowPostPushCallback)
                    OnPostPushData();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                IsPushing = false;
                Unbind();
            }
        }
    }
}