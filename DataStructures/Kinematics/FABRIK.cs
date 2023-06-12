using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Kinematics
{
    public class FABRIK
    {
        public class Bone
        {
            public Vec3 Position;
            public float Length;

            public Bone(Vec3 position, float length)
            {
                Position = position;
                Length = length;
            }
        }

        public List<Bone> Bones { get; private set; }
        public float Tolerance { get; set; }

        public FABRIK()
        {
            Bones = new List<Bone>();
            Tolerance = 0.01f;
        }

        public void AddBone(Vec3 position, float length)
        {
            Bones.Add(new Bone(position, length));
        }

        public bool Solve(Vec3 target, int maxIterations = 10)
        {
            if (Bones.Count == 0)
            {
                throw new InvalidOperationException("No bones added to the FABRIK solver.");
            }

            float totalLength = 0;
            for (int i = 0; i < Bones.Count; i++)
            {
                totalLength += Bones[i].Length;
            }

            if ((target - Bones[0].Position).Length > totalLength)
            {
                // The target is unreachable, move bones towards the target.
                for (int i = 0; i < Bones.Count - 1; i++)
                {
                    Vec3 dir = (target - Bones[i].Position).Normalized();
                    Bones[i + 1].Position = Bones[i].Position + dir * Bones[i].Length;
                }
                return false;
            }

            int iteration = 0;
            float distanceToTarget = (Bones[^1].Position - target).Length;

            while (distanceToTarget > Tolerance && iteration < maxIterations)
            {
                // Stage 1: Forward reaching
                Bones[^1].Position = target;

                for (int i = Bones.Count - 2; i >= 0; i--)
                {
                    Vec3 dir = (Bones[i].Position - Bones[i + 1].Position).Normalized();
                    Bones[i].Position = Bones[i + 1].Position + dir * Bones[i].Length;
                }

                // Stage 2: Backward reaching
                for (int i = 0; i < Bones.Count - 1; i++)
                {
                    Vec3 dir = (Bones[i + 1].Position - Bones[i].Position).Normalized();
                    Bones[i + 1].Position = Bones[i].Position + dir * Bones[i].Length;
                }

                distanceToTarget = (Bones[^1].Position - target).Length;
                iteration++;
            }

            return iteration < maxIterations;
        }
    }
}
