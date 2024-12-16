namespace XREngine.Data.MMD
{
    public abstract class AnimationBase<T> : Dictionary<string, List<T>>, IBinaryDataSource where T : IBinaryDataSource, new()
    {
        public void Load(BinaryReader reader)
        {
            uint count = reader.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                string name = VMDUtils.ToShiftJisString(reader.ReadBytes(15));
                T frameKey = new();
                frameKey.Load(reader);
                if (!ContainsKey(name))
                    this[name] = [];
                this[name].Add(frameKey);
            }
        }

        public void Save(BinaryWriter writer)
        {
            uint count = (uint)this.Sum(kv => kv.Value.Count);
            writer.Write(count);
            foreach (var kv in this)
            {
                byte[] nameBytes = [.. VMDUtils.ToShiftJisBytes(kv.Key), .. new byte[15 - kv.Key.Length]];
                foreach (var frameKey in kv.Value)
                {
                    writer.Write(nameBytes);
                    frameKey.Save(writer);
                }
            }
        }
    }
}
