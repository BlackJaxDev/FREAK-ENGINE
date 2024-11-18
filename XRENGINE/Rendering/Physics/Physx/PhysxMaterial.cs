using MagicPhysX;
using XREngine.Data.Core;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxMaterial : XRBase
    {
        private readonly unsafe PxMaterial* _obj;
        private readonly PhysxScene _scene;

        public PxMaterial* Material => _obj;

        public float StaticFriction
        {
            get => PxMaterial_getStaticFriction(_obj);
            set => PxMaterial_setStaticFriction_mut(_obj, value);
        }
        public float DynamicFriction
        {
            get => PxMaterial_getDynamicFriction(_obj);
            set => PxMaterial_setDynamicFriction_mut(_obj, value);
        }
        public float Restitution
        {
            get => PxMaterial_getRestitution(_obj);
            set => PxMaterial_setRestitution_mut(_obj, value);
        }
        public float Damping
        {
            get => PxMaterial_getDamping(_obj);
            set => PxMaterial_setDamping_mut(_obj, value);
        }
        public PxCombineMode FrictionCombineMode
        {
            get => PxMaterial_getFrictionCombineMode(_obj);
            set => PxMaterial_setFrictionCombineMode_mut(_obj, value);
        }
        public PxCombineMode RestitutionCombineMode
        {
            get => PxMaterial_getRestitutionCombineMode(_obj);
            set => PxMaterial_setRestitutionCombineMode_mut(_obj, value);
        }
        public PxMaterialFlags Flags
        {
            get => PxMaterial_getFlags(_obj);
            set => PxMaterial_setFlags_mut(_obj, value);
        }

        public PhysxMaterial(PhysxScene scene)
        {
            _scene = scene;
            _obj = _scene.Physics->CreateMaterialMut(0.0f, 0.0f, 0.0f);
        }
    }
}