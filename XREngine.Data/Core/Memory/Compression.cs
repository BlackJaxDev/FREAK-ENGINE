using System.Text;

namespace XREngine.Data
{
    public static class Compression
    {
        public static byte[] DecompressFromString(uint? length, string byteStr)
        {
            byte[] bytes = [];
            if (!string.IsNullOrEmpty(byteStr))
            {
                bytes = new byte[length ?? byteStr.Length / 2u];
                for (int i = 0; i < length; i++)
                    bytes[i] = byte.Parse(byteStr.Substring(i << 1, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return Decompress(bytes);
        }

        public static byte[] Decompress(byte[] bytes)
        {
            SevenZip.Compression.LZMA.Decoder decoder = new();
            MemoryStream inStream = new(bytes);
            MemoryStream outStream = new();
            inStream.Seek(0, SeekOrigin.Begin);
            decoder.SetDecoderProperties(inStream.ToArray());
            inStream.Seek(5, SeekOrigin.Begin);
            byte[] properties = new byte[5];
            inStream.Read(properties, 0, 5);
            byte[] lengthBytes = new byte[4];
            inStream.Read(lengthBytes, 0, 4);
            int len = BitConverter.ToInt32(lengthBytes, 0);
            decoder.Code(inStream, outStream, inStream.Length - 9, len, null);
            return outStream.ToArray();
        }

        public static unsafe string CompressToString(DataSource source)
        {
            byte* ptr = (byte*)source.Address;
            byte[] arr = new byte[source.Length];
            for (int i = 0; i < source.Length; i++)
                arr[i] = ptr[i];
            return CompressToString(arr);
        }

        public static unsafe string CompressToString(byte[] arr)
        {
            byte[] compressed = Compress(arr);
            StringBuilder sb = new();
            foreach (byte b in compressed)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        public static unsafe byte[] Compress(byte[] arr)
        {
            SevenZip.Compression.LZMA.Encoder encoder = new();
            MemoryStream inStream = new(arr);
            MemoryStream outStream = new();
            encoder.WriteCoderProperties(outStream);
            outStream.Write(BitConverter.GetBytes(arr.Length), 0, 4);
            encoder.Code(inStream, outStream, arr.Length, -1, null);
            return outStream.ToArray();
        }
    }
}