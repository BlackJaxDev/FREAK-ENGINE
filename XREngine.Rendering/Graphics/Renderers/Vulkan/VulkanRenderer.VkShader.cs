using File = System.IO.File;

namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{
    public unsafe partial class VulkanRenderer
    {
        public class VkShader : GraphicShader<VulkanRenderer>
        {
            public byte[]? spv = null;
            public string? path;

            public VkShader()
            {

            }

            public VkShader(byte[] spv, string path)
            {
                this.spv = spv;
                this.path = path;
            }

            public void LoadSpv()
            {
                spv = File.ReadAllBytes(path);
                var shader = Renderer.CreateShaderModule(spv);
            }
        }
    }
}
