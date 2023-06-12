using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XREngine.Rendering.Graphics.Renderers
{
    public abstract class AbstractRenderer
    {
        protected bool frameBufferResized = false;

        private Dictionary<string, bool> _verifiedExtensions = new Dictionary<string, bool>();

        private void LogExtension(string name, bool exists)
        {
            _verifiedExtensions.Add(name, exists);
        }
        private bool ExtensionChecked(string name)
        {
            _verifiedExtensions.TryGetValue(name, out bool exists);
            return exists;
        }
        protected void VerifyExt<T>(string name, ref T output) where T : NativeExtension<Vk>
        {
            if (output is null && !ExtensionChecked(name))
                LogExtension(name, LoadExt(out output));
        }

    }
}
