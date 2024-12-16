namespace XREngine.Data.MMD
{
    public class SelfShadowFrameKey : IBinaryDataSource
    {
        public uint FrameNumber { get; private set; }
        public sbyte Mode { get; private set; }
        public float Distance { get; private set; }

        public void Load(BinaryReader reader)
        {
            FrameNumber = reader.ReadUInt32();
            Mode = (sbyte)reader.ReadByte();

            if (Mode < 0 || Mode > 2)
                throw new InvalidFileError($"Invalid self shadow mode {Mode} at frame {FrameNumber}");
            
            Distance = 10000 - reader.ReadSingle() * 100000;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(FrameNumber);
            writer.Write(Mode);
            writer.Write((10000 - Distance) / 100000);
        }

        public override string ToString()
            => $"<SelfShadowFrameKey frame {FrameNumber}, mode {Mode}, distance {Distance}>";
    }
}
