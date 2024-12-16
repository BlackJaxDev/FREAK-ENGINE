using System.Text;

namespace XREngine.Data.MMD
{
    public class VMDHeader : IBinaryDataSource
    {
        private static readonly byte[] VMD_SIGN = Encoding.ASCII.GetBytes("Vocaloid Motion Data 0002");

        public string ModelName { get; private set; } = string.Empty;

        public void Load(BinaryReader reader)
        {
            byte[] sig = reader.ReadBytes(30);

            if (!sig.Take(VMD_SIGN.Length).SequenceEqual(VMD_SIGN))
                throw new InvalidFileError($"File signature \"{Encoding.ASCII.GetString(sig)}\" is invalid.");
            
            ModelName = VMDUtils.ToShiftJisString(reader.ReadBytes(20)) ?? string.Empty;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(VMD_SIGN);
            writer.Write(VMDUtils.ToShiftJisBytes(ModelName).Concat(new byte[20 - ModelName.Length]).ToArray());
        }

        public override string ToString()
            => $"<Header model_name {ModelName}>";
    }
}
