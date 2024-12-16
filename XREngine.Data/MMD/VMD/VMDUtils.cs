using System.Text;

namespace XREngine.Data.MMD
{
    public static class VMDUtils
    {
        public static string ToShiftJisString(byte[] byteString)
            => Encoding.GetEncoding("shift_jis").GetString(byteString).Split('\0')[0];
        public static byte[] ToShiftJisBytes(string str)
            => Encoding.GetEncoding("shift_jis").GetBytes(str);
    }
}
