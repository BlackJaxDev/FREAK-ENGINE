using MagicPhysX;
using XREngine.Data.Geometry;
using XREngine.Scene;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxActor : PhysxBase, IAbstractPhysicsActor
    {
        public abstract PxActor* ActorPtr { get; }
        public override unsafe PxBase* BasePtr => (PxBase*)ActorPtr;

        ~PhysxActor() => Release();

        public bool DebugVisualize
        {
            get => ActorFlags.HasFlag(PxActorFlag.Visualization);
            set => SetActorFlag(PxActorFlag.Visualization, value);
        }
        public bool GravityEnabled
        {
            get => !ActorFlags.HasFlag(PxActorFlag.DisableGravity);
            set => SetActorFlag(PxActorFlag.DisableGravity, !value);
        }
        public bool SimulationEnabled
        {
            get => !ActorFlags.HasFlag(PxActorFlag.DisableSimulation);
            set => SetActorFlag(PxActorFlag.DisableSimulation, !value);
        }
        public bool SendSleepNotifies
        {
            get => ActorFlags.HasFlag(PxActorFlag.SendSleepNotifies);
            set => SetActorFlag(PxActorFlag.SendSleepNotifies, value);
        }

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

        public PxScene* ScenePtr => ActorPtr->GetScene();
        public PhysxScene? Scene => PhysxScene.Scenes.TryGetValue((nint)ScenePtr, out PhysxScene? scene) ? scene : null;

        public PxActorType ActorType => NativeMethods.PxActor_getType(ActorPtr);

        public virtual void Release()
            => ActorPtr->ReleaseMut();

        public void Destroy(bool wakeOnLostTouch = false)
        {
            Scene?.RemoveActor(this, wakeOnLostTouch);
            Release();
        }
    }
}