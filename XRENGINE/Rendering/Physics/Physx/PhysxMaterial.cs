using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxMaterial : AbstractPhysicsMaterial
    {
        private readonly unsafe PxMaterial* _materialPtr;

        public PhysxMaterial()
            => _materialPtr = PhysxScene.PhysicsPtr->CreateMaterialMut(0.0f, 0.0f, 0.0f);
        public PhysxMaterial(
            float staticFriction,
            float dynamicFriction,
            float restitution)
            => _materialPtr = PhysxScene.PhysicsPtr->CreateMaterialMut(staticFriction, dynamicFriction, restitution);
        public PhysxMaterial(PxMaterial* materialPtr)
            => _materialPtr = materialPtr;
        public PhysxMaterial(
            float staticFriction,
            float dynamicFriction,
            float restitution,
            float damping,
            ECombineMode frictionCombineMode,
            ECombineMode restitutionCombineMode,
            bool disableFriction,
            bool disableStrongFriction,
            bool improvedPatchFriction,
            bool compliantContact)
        {
            _materialPtr = PhysxScene.PhysicsPtr->CreateMaterialMut(staticFriction, dynamicFriction, restitution);
            Damping = damping;
            FrictionCombineMode = frictionCombineMode;
            RestitutionCombineMode = restitutionCombineMode;
            DisableFriction = disableFriction;
            DisableStrongFriction = disableStrongFriction;
            ImprovedPatchFriction = improvedPatchFriction;
            CompliantContact = compliantContact;
        }

        public PxMaterial* MaterialPtr => _materialPtr;

        public override float StaticFriction
        {
            get => _materialPtr->GetStaticFriction();
            set => _materialPtr->SetStaticFrictionMut(value);
        }
        public override float DynamicFriction
        {
            get => _materialPtr->GetDynamicFriction();
            set => _materialPtr->SetDynamicFrictionMut(value);
        }
        public override float Restitution
        {
            get => _materialPtr->GetRestitution();
            set => _materialPtr->SetRestitutionMut(value);
        }
        public override float Damping
        {
            get => _materialPtr->GetDamping();
            set => _materialPtr->SetDampingMut(value);
        }
        public override ECombineMode FrictionCombineMode
        {
            get => Conv(_materialPtr->GetFrictionCombineMode());
            set => _materialPtr->SetFrictionCombineModeMut(Conv(value));
        }
        public override ECombineMode RestitutionCombineMode
        {
            get => Conv(_materialPtr->GetRestitutionCombineMode());
            set => _materialPtr->SetRestitutionCombineModeMut(Conv(value));
        }
        public PxMaterialFlags MaterialFlags
        {
            get => _materialPtr->GetFlags();
            set => _materialPtr->SetFlagsMut(value);
        }
        public override bool DisableFriction
        {
            get => (MaterialFlags & PxMaterialFlags.DisableFriction) != 0;
            set
            {
                if (value)
                    MaterialFlags |= PxMaterialFlags.DisableFriction;
                else
                    MaterialFlags &= ~PxMaterialFlags.DisableFriction;
            }
        }
        public override bool DisableStrongFriction
        {
            get => (MaterialFlags & PxMaterialFlags.DisableStrongFriction) != 0;
            set
            {
                if (value)
                    MaterialFlags |= PxMaterialFlags.DisableStrongFriction;
                else
                    MaterialFlags &= ~PxMaterialFlags.DisableStrongFriction;
            }
        }
        public override bool ImprovedPatchFriction
        {
            get => (MaterialFlags & PxMaterialFlags.ImprovedPatchFriction) != 0;
            set
            {
                if (value)
                    MaterialFlags |= PxMaterialFlags.ImprovedPatchFriction;
                else
                    MaterialFlags &= ~PxMaterialFlags.ImprovedPatchFriction;
            }
        }
        public override bool CompliantContact
        {
            get => (MaterialFlags & PxMaterialFlags.CompliantContact) != 0;
            set
            {
                if (value)
                    MaterialFlags |= PxMaterialFlags.CompliantContact;
                else
                    MaterialFlags &= ~PxMaterialFlags.CompliantContact;
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