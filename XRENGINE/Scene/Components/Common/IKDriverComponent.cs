using System.Numerics;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Components
{
    public class IKDriverComponent : XRComponent
    {
        public TransformBase? GoalSocket { get; set; }
        public Transform? EndEffectorSocket { get; set; }
        public Transform? BaseSocket { get; set; }

        public float SqrDistError { get; set; } = 0.01f;
        public float Weight { get; set; } = 1.0f;
        public int MaxIterations { get; set; } = 10;
        
        private List<Transform> SocketChain { get; } = [];

        protected internal override void OnComponentActivated()
        {
            SocketChain.Clear();

            Transform? current = EndEffectorSocket;
            while (current != null && current != BaseSocket?.Parent)
            {
                SocketChain.Add(current);
                current = current.Parent as Transform;
            }

            RegisterTick(ETickGroup.PrePhysics, ETickOrder.Logic, Update);
        }
        protected internal override void OnComponentDeactivated()
        {
            SocketChain.Clear();

            UnregisterTick(ETickGroup.PrePhysics, ETickOrder.Logic, Update);
        }

        private void Update(float delta)
        {
            if (GoalSocket is null || EndEffectorSocket is null || BaseSocket is null)
                return;

            Vector3 goalPos = GoalSocket.WorldTranslation;
            Vector3 effectorPos = EndEffectorSocket.WorldTranslation;
            Vector3 targetPos = Vector3.Lerp(effectorPos, goalPos, Weight);

            int iters = 0;
            do
            {
                for (int i = 0; i < SocketChain.Count - 2; ++i)
                {
                    for (int j = 1; j < i + 3 && j < SocketChain.Count; ++j)
                    {
                        RotateSocket(EndEffectorSocket, SocketChain[j], targetPos);

                        if ((EndEffectorSocket.WorldTranslation - targetPos).LengthSquared() <= SqrDistError)
                            return;
                    }
                }
            }
            while (((EndEffectorSocket.WorldTranslation - targetPos).LengthSquared()) > SqrDistError && ++iters <= MaxIterations);
        }

        private void RotateSocket(Transform effector, Transform bone, Vector3 goalPos)
        {
            Vector3 effectorPos = effector.WorldTranslation;
            Vector3 bonePos = bone.WorldTranslation;
            Quaternion boneRot = bone.Rotation;
            Vector3 boneToEffector = effectorPos - bonePos;
            Vector3 boneToGoal = goalPos - bonePos;
            Quaternion fromToRot = XRMath.RotationBetweenVectors(boneToEffector, boneToGoal);
            Quaternion newRot = fromToRot * boneRot;
            bone.Rotation = newRot;
        }
    }
}
