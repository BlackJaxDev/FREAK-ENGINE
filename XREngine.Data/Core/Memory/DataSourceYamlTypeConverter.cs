using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace XREngine.Data
{
    public sealed class DataSourceYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
            => type == typeof(DataSource);

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            uint? length = null;
            string byteStr = string.Empty;

            parser.Consume<MappingStart>();
            {
                while (parser.TryConsume<Scalar>(out var scalar) && scalar != null)
                {
                    switch (scalar.Value)
                    {
                        case "Length":
                            length = uint.Parse(parser.Consume<Scalar>().Value);
                            break;
                        case "Bytes":
                            byteStr = parser.Consume<Scalar>().Value;
                            break;
                    }
                }
            }
            parser.Consume<MappingEnd>();

            return new DataSource(DecompressFromString(length, byteStr));
        }

        public unsafe void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            if (value is not DataSource source)
                return;

            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));
            {
                emitter.Emit(new Scalar("Length"));
                emitter.Emit(new Scalar(source.Length.ToString()));

                emitter.Emit(new Scalar("Bytes"));
                emitter.Emit(new Scalar(CompressToString(source)));
            }
            emitter.Emit(new MappingEnd());
        }

        private static byte[] DecompressFromString(uint? length, string byteStr)
        {
            byte[] bytes = [];
            if (!string.IsNullOrEmpty(byteStr))
            {
                bytes = new byte[length ?? byteStr.Length / 2u];
                for (int i = 0; i < length; i++)
                    bytes[i] = byte.Parse(byteStr.Substring(i << 1, 2), System.Globalization.NumberStyles.HexNumber);
            }
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

        private static unsafe string CompressToString(DataSource source)
        {
            byte* ptr = (byte*)source.Address;
            byte[] arr = new byte[source.Length];
            for (int i = 0; i < source.Length; i++)
                arr[i] = ptr[i];
            SevenZip.Compression.LZMA.Encoder encoder = new();
            MemoryStream inStream = new(arr);
            MemoryStream outStream = new();
            encoder.WriteCoderProperties(outStream);
            outStream.Write(BitConverter.GetBytes(arr.Length), 0, 4);
            encoder.Code(inStream, outStream, arr.Length, -1, null);
            byte[] compressed = outStream.ToArray();
            StringBuilder sb = new();
            foreach (byte b in compressed)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
