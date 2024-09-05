using System.Numerics;

namespace XREngine.Data.Kinematics
{
    public class FABRIK
    {
        public class Bone(Vector3 position, float length)
        {
            public Vector3 Position = position;
            public float Length = length;
        }

        public List<Bone> Bones { get; private set; }
        public float Tolerance { get; set; }

        public FABRIK()
        {
            Bones = [];
            Tolerance = 0.01f;
        }

        public void AddBone(Vector3 position, float length)
        {
            Bones.Add(new Bone(position, length));
        }

        public bool Solve(Vector3 target, int maxIterations = 10)
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

            if ((target - Bones[0].Position).Length() > totalLength)
            {
                // The target is unreachable, move bones towards the target.
                for (int i = 0; i < Bones.Count - 1; i++)
                {
                    Vector3 dir = Vector3.Normalize(target - Bones[i].Position);
                    Bones[i + 1].Position = Bones[i].Position + dir * Bones[i].Length;
                }
                return false;
            }

            int iteration = 0;
            float distanceToTarget = (Bones[^1].Position - target).Length();

            while (distanceToTarget > Tolerance && iteration < maxIterations)
            {
                // Stage 1: Forward reaching
                Bones[^1].Position = target;

                for (int i = Bones.Count - 2; i >= 0; i--)
                {
                    Vector3 dir = Vector3.Normalize(Bones[i].Position - Bones[i + 1].Position);
                    Bones[i].Position = Bones[i + 1].Position + dir * Bones[i].Length;
                }

                // Stage 2: Backward reaching
                for (int i = 0; i < Bones.Count - 1; i++)
                {
                    Vector3 dir = Vector3.Normalize(Bones[i + 1].Position - Bones[i].Position);
                    Bones[i + 1].Position = Bones[i].Position + dir * Bones[i].Length;
                }

                distanceToTarget = (Bones[^1].Position - target).Length();
                iteration++;
            }

            return iteration < maxIterations;
        }
    }
}
