using MagicPhysX;
using XREngine.Data.Geometry;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxActor(PhysxScene scene) : PhysxBase
    {
        public abstract PxActor* ActorPtr { get; }
        public override unsafe PxBase* BasePtr => (PxBase*)ActorPtr;

        public PxActorFlags ActorFlags
        {
            get => ActorPtr->GetActorFlags();
            set => ActorPtr->SetActorFlagsMut(value);
        }

        public void SetActorFlag(PxActorFlag flag, bool value)
            => ActorPtr->SetActorFlagMut(flag, value);

        public byte DominanceGroup
        {
            get => ActorPtr->GetDominanceGroup();
            set => ActorPtr->SetDominanceGroupMut(value);
        }

        public byte OwnerClient
        {
            get => ActorPtr->GetOwnerClient();
            set => ActorPtr->SetOwnerClientMut(value);
        }

        public PxAggregate* Aggregate => ActorPtr->GetAggregate();

        public ushort CollisionGroup
        {
            get => ActorPtr->PhysPxGetGroup();
            set => ActorPtr->PhysPxSetGroup(value);
        }

        public PxGroupsMask GroupsMask
        {
            get => ActorPtr->PhysPxGetGroupsMask();
            set
            {
                PxGroupsMask mask = value;
                ActorPtr->PhysPxSetGroupsMask(&mask);
            }
        }

        public AABB GetWorldBounds(float inflation)
        {
            PxBounds3 bounds = ActorPtr->GetWorldBounds(inflation);
            return new AABB(bounds.minimum, bounds.maximum);
        }

        public string Name
        {
            get
            {
                byte* name = ActorPtr->GetName();
                return new string((sbyte*)name);
            }
            set
            {
                fixed (byte* name = System.Text.Encoding.UTF8.GetBytes(value))
                    ActorPtr->SetNameMut(name);
            }
        }

        public PhysxScene Scene { get; } = scene;
        public PxScene* ScenePtr => ActorPtr->GetScene();

        public PxActorType ActorType => NativeMethods.PxActor_getType(ActorPtr);

        public void Release()
            => ActorPtr->ReleaseMut();
    }
}