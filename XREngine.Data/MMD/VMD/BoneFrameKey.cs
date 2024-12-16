using Extensions;
using System.Numerics;

namespace XREngine.Data.MMD
{
    public class BoneFrameKey : IBinaryDataSource
    {
        public uint FrameNumber { get; private set; }
        public Vector3 Location { get; private set; }
        public Quaternion Rotation { get; private set; }
        public sbyte[] Interp { get; private set; } = new sbyte[64];

        public void Load(BinaryReader reader)
        {
            FrameNumber = reader.ReadUInt32();
            Location = new Vector3(reader.ReadBytes(12).SelectEvery(4, x => BitConverter.ToSingle([.. x], 0)).ToArray());
            float[] rot = reader.ReadBytes(16).SelectEvery(4, x => BitConverter.ToSingle([.. x], 0)).ToArray();
            Rotation = new Quaternion() { X = rot[0], Y = rot[1], Z = rot[2], W = rot[3] };
            if (rot.All(r => r == 0.0f))
                Rotation = Quaternion.Identity;
            Interp = reader.ReadBytes(64).Select(b => (sbyte)b).ToArray();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(FrameNumber);
            writer.Write(new float[] { Location.X, Location.Y, Location.Z }.SelectMany(BitConverter.GetBytes).ToArray());
            writer.Write(new float[] { Rotation.X, Rotation.Y, Rotation.Z, Rotation.W }.SelectMany(BitConverter.GetBytes).ToArray());
            writer.Write(Interp.Select(b => (byte)b).ToArray());
        }

        public override string ToString()
            => $"<BoneFrameKey frame {FrameNumber}, loc {string.Join(", ", Location)}, rot {string.Join(", ", Rotation)}>";
    }
}
