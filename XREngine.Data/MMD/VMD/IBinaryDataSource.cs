namespace XREngine.Data.MMD
{
    public interface IBinaryDataSource
    {
        void Load(BinaryReader reader);
        void Save(BinaryWriter writer);
    }
}
