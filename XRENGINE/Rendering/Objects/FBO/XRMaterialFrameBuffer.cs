namespace XREngine.Rendering
{
    public delegate void DelSetUniforms(XRRenderProgram program);
    /// <summary>
    /// Sets this FBO's render targets to the textures in the provided material using their FrameBufferAttachment properties.
    /// </summary>
    public class XRMaterialFrameBuffer(XRMaterial material) : XRFrameBuffer
    {
        public XRMaterial Material
        {
            get => material;
            set
            {
                if (material == value)
                    return;

                SetRenderTargets(material = value);
                VerifyTextures();
            }
        }

        private void VerifyTextures()
        {
            uint w = 0;
            uint h = 0;
            uint tw = 0;
            uint th = 0;

            foreach (var tex in Material.Textures)
            {
                if (tex?.FrameBufferAttachment is null)
                    continue;

                if (tex is XRTexture2D tref)
                {
                    tw = tref.Width;
                    th = tref.Height;
                }
                else if (tex is XRTextureView2D vref)
                {
                    tw = vref.Width;
                    th = vref.Height;
                }

                if (w < 0)
                    w = tw;
                else if (w != tw)
                    Debug.LogWarning($"FBO texture widths are not all the same.");

                if (h < 0)
                    h = th;
                else if (h != th)
                    Debug.LogWarning($"FBO texture heights are not all the same.");
            }
        }
    }
}
