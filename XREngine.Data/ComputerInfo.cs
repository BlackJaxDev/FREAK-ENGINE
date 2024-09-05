using XREngine.Core.Files;
using static XREngine.Data.Endian;

namespace XREngine.Core
{
    public class ComputerInfo : XRAsset
    {
        public int ProcessorCount { get; private set; }
        public bool Is64Bit { get; private set; }
        public OperatingSystem? OSVersion { get; private set; }
        public EOrder Endian { get; private set; } = EOrder.Big;
        public int MaxTextureUnits { get; internal set; }

        public static ComputerInfo Analyze() => new()
        {
            ProcessorCount = Environment.ProcessorCount,
            OSVersion = Environment.OSVersion,
            Is64Bit = Environment.Is64BitOperatingSystem
        };
    }
}
