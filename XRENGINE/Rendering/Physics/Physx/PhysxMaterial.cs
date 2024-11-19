using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxMaterial : AbstractPhysicsMaterial
    {
        private readonly unsafe PxMaterial* _obj;
        private readonly PhysxScene _scene;

        public PhysxMaterial(PhysxScene scene)
        {
            _scene = scene;
            _obj = _scene.PhysicsPtr->CreateMaterialMut(0.0f, 0.0f, 0.0f);
        }

        public PxMaterial* Material => _obj;

        public override float StaticFriction
        {
            get => _obj->GetStaticFriction();
            set => _obj->SetStaticFrictionMut(value);
        }
        public override float DynamicFriction
        {
            get => _obj->GetDynamicFriction();
            set => _obj->SetDynamicFrictionMut(value);
        }
        public override float Restitution
        {
            get => _obj->GetRestitution();
            set => _obj->SetRestitutionMut(value);
        }
        public override float Damping
        {
            get => _obj->GetDamping();
            set => _obj->SetDampingMut(value);
        }
        public override ECombineMode FrictionCombineMode
        {
            get => Conv(_obj->GetFrictionCombineMode());
            set => _obj->SetFrictionCombineModeMut(Conv(value));
        }
        public override ECombineMode RestitutionCombineMode
        {
            get => Conv(_obj->GetRestitutionCombineMode());
            set => _obj->SetRestitutionCombineModeMut(Conv(value));
        }
        public PxMaterialFlags Flags
        {
            get => _obj->GetFlags();
            set => _obj->SetFlagsMut(value);
        }
        public override bool DisableFriction
        {
            get => (Flags & PxMaterialFlags.DisableFriction) != 0;
            set
            {
                if (value)
                    Flags |= PxMaterialFlags.DisableFriction;
                else
                    Flags &= ~PxMaterialFlags.DisableFriction;
            }
        }
        public override bool DisableStrongFriction
        {
            get => (Flags & PxMaterialFlags.DisableStrongFriction) != 0;
            set
            {
                if (value)
                    Flags |= PxMaterialFlags.DisableStrongFriction;
                else
                    Flags &= ~PxMaterialFlags.DisableStrongFriction;
            }
        }
        public override bool ImprovedPatchFriction
        {
            get => (Flags & PxMaterialFlags.ImprovedPatchFriction) != 0;
            set
            {
                if (value)
                    Flags |= PxMaterialFlags.ImprovedPatchFriction;
                else
                    Flags &= ~PxMaterialFlags.ImprovedPatchFriction;
            }
        }
        public override bool CompliantContact
        {
            get => (Flags & PxMaterialFlags.CompliantContact) != 0;
            set
            {
                if (value)
                    Flags |= PxMaterialFlags.CompliantContact;
                else
                    Flags &= ~PxMaterialFlags.CompliantContact;
            }
        }

        private static PxCombineMode Conv(ECombineMode mode)
            => mode switch
            {
                ECombineMode.Average => PxCombineMode.Average,
                ECombineMode.Min => PxCombineMode.Min,
                ECombineMode.Multiply => PxCombineMode.Multiply,
                ECombineMode.Max => PxCombineMode.Max,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
            };

        private static ECombineMode Conv(PxCombineMode mode)
            => mode switch
            {
                PxCombineMode.Average => ECombineMode.Average,
                PxCombineMode.Min => ECombineMode.Min,
                PxCombineMode.Multiply => ECombineMode.Multiply,
                PxCombineMode.Max => ECombineMode.Max,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
            };
    }
}