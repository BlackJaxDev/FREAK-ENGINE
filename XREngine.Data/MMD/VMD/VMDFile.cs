namespace XREngine.Data.MMD
{
    public class VMDFile
    {
        public string? FilePath { get; private set; }
        public VMDHeader? Header { get; private set; }
        public BoneAnimation? BoneAnimation { get; private set; }
        public ShapeKeyAnimation? ShapeKeyAnimation { get; private set; }
        public CameraAnimation? CameraAnimation { get; private set; }
        public LampAnimation? LampAnimation { get; private set; }
        public SelfShadowAnimation? SelfShadowAnimation { get; private set; }
        public PropertyAnimation? PropertyAnimation { get; private set; }

        public void Load(string path)
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            FilePath = path;
            Header = new VMDHeader();
            BoneAnimation = [];
            ShapeKeyAnimation = [];
            CameraAnimation = [];
            LampAnimation = [];
            SelfShadowAnimation = [];
            PropertyAnimation = [];

            Header.Load(reader);
            try
            {
                BoneAnimation.Load(reader);
                ShapeKeyAnimation.Load(reader);
                CameraAnimation.Load(reader);
                LampAnimation.Load(reader);
                SelfShadowAnimation.Load(reader);
                PropertyAnimation.Load(reader);
            }
            catch (EndOfStreamException) { }
        }

        public void Save(string? path = null)
        {
            if (Header is null || BoneAnimation is null || ShapeKeyAnimation is null || CameraAnimation is null || LampAnimation is null || SelfShadowAnimation is null || PropertyAnimation is null)
                throw new InvalidOperationException("Cannot save VMD file without loading it first.");
            
            path ??= FilePath ?? string.Empty;
            using var writer = new BinaryWriter(File.OpenWrite(path));
            Header.Save(writer);
            BoneAnimation.Save(writer);
            ShapeKeyAnimation.Save(writer);
            CameraAnimation.Save(writer);
            LampAnimation.Save(writer);
            SelfShadowAnimation.Save(writer);
            PropertyAnimation.Save(writer);
        }
    }
}
