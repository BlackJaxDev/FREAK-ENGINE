
using XREngine.Data.Core;

namespace XREngine.Rendering
{
    public class ColorGradingSettings : XRBase
    {
        public ColorGradingSettings()
        {

        }

        public bool AutoExposure { get; set; }
        public float Exposure { get; set; }
    }
}