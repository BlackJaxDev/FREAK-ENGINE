namespace XREngine.Data.MMD
{
    public class ShapeKeyFrameKey : IBinaryDataSource
    {
        public uint FrameNumber { get; private set; }
        public float Weight { get; private set; }

        public void Load(BinaryReader reader)
        {
            FrameNumber = reader.ReadUInt32();
            Weight = reader.ReadSingle();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(FrameNumber);
            writer.Write(Weight);
        }

        public override string ToString()
            => $"<ShapeKeyFrameKey frame {FrameNumber}, weight {Weight}>";
    }
}
