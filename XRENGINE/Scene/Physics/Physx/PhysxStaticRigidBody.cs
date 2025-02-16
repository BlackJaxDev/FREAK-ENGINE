﻿using MagicPhysX;
using System.Numerics;
using XREngine.Scene;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxStaticRigidBody : PhysxRigidActor, IAbstractStaticRigidBody
    {
        private readonly unsafe PxRigidStatic* _obj;

        public static Dictionary<nint, PhysxStaticRigidBody> AllStaticRigidBodies { get; } = [];
        public static PhysxStaticRigidBody? GetStaticBody(PxRigidStatic* ptr)
            => AllStaticRigidBodies.TryGetValue((nint)ptr, out var body) ? body : null;

        public PhysxStaticRigidBody()
            : this(null, null) { }

        internal PhysxStaticRigidBody(PxRigidStatic* obj)
        {
            _obj = obj;
            AllActors.Add((nint)_obj, this);
            AllRigidActors.Add((nint)_obj, this);
            AllStaticRigidBodies.Add((nint)_obj, this);
        }

        public PhysxStaticRigidBody(
            Vector3? position,
            Quaternion? rotation)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            _obj = PhysxScene.PhysicsPtr->CreateRigidStaticMut(&tfm);
            AllActors.Add((nint)_obj, this);
            AllRigidActors.Add((nint)_obj, this);
            AllStaticRigidBodies.Add((nint)_obj, this);
        }
        public PhysxStaticRigidBody(
            PhysxShape shape,
            Vector3? position = null,
            Quaternion? rotation = null)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            _obj = PhysxScene.PhysicsPtr->PhysPxCreateStatic1(&tfm, shape.ShapePtr);
            AllActors.Add((nint)_obj, this);
            AllRigidActors.Add((nint)_obj, this);
            AllStaticRigidBodies.Add((nint)_obj, this);
        }
        public PhysxStaticRigidBody(
            PhysxMaterial material,
            IAbstractPhysicsGeometry geometry,
            Vector3? position = null,
            Quaternion? rotation = null,
            Vector3? shapeOffsetTranslation = null,
            Quaternion? shapeOffsetRotation = null)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            var shapeTfm = PhysxScene.MakeTransform(shapeOffsetTranslation, shapeOffsetRotation);
            using var structObj = geometry.GetPhysxStruct();
            _obj = PhysxScene.PhysicsPtr->PhysPxCreateStatic(&tfm, structObj.ToStructPtr<PxGeometry>(), material.MaterialPtr, &shapeTfm);
            AllActors.Add((nint)_obj, this);
            AllRigidActors.Add((nint)_obj, this);
            AllStaticRigidBodies.Add((nint)_obj, this);
        }

        public static PhysxStaticRigidBody CreatePlane(PxPlane plane, PhysxMaterial material)
        {
            var stat = PhysxScene.PhysicsPtr->PhysPxCreatePlane(&plane, material.MaterialPtr);
            return new PhysxStaticRigidBody(stat);
        }
        public static PhysxStaticRigidBody CreatePlane(Vector3 normal, float distance, PhysxMaterial material)
            => CreatePlane(PxPlane_new_1(normal.X, normal.Y, normal.Z, distance), material);
        public static PhysxStaticRigidBody CreatePlane(PhysxPlane plane, PhysxMaterial material)
            => CreatePlane(plane.InternalPlane, material);

        public override unsafe PxRigidActor* RigidActorPtr => (PxRigidActor*)_obj;
        public override unsafe PxActor* ActorPtr => (PxActor*)_obj;
        public override unsafe PxBase* BasePtr => (PxBase*)_obj;

        public override Vector3 LinearVelocity { get; } = Vector3.Zero;
        public override Vector3 AngularVelocity { get; } = Vector3.Zero;
    }
}