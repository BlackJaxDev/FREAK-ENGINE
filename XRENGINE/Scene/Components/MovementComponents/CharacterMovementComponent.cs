﻿using Extensions;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Components;
using XREngine.Data.Core;
using XREngine.Physics;
using XREngine.Physics.ShapeTracing;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Components
{
    [RequireComponents(typeof(CapsuleYComponent))]
    public class CharacterMovement3DComponent : MovementComponent
    {
        public CapsuleYComponent? RootCapsule => GetSiblingComponent<CapsuleYComponent>(true);

        private EMovementMode _currentMovementMode = EMovementMode.Falling;
        private Vector3 _worldGroundContactPoint;
        private XRCollisionObject? _currentWalkingSurface;
        private Vector3 _groundNormal;
        private Action<Vector3>? _subUpdateTick;
        private Vector3 
            _position, _prevPosition,
            _velocity, _prevVelocity,
            _acceleration;

        public Vector3 CurrentPosition => _position;
        public Vector3 CurrentVelocity => _velocity;
        public Vector3 CurrentAcceleration => _acceleration;

        private bool _postWalkAllowJump = false, _justJumped = false;
        private ShapeTraceClosest _closestTrace = new(
            null, Matrix4x4.Identity, Matrix4x4.Identity,
            (ushort)ECollisionGroup.Characters,
            (ushort)(ECollisionGroup.StaticWorld | ECollisionGroup.DynamicWorld));

        public float VerticalStepUpHeight { get; set; } = 1.0f;
        public float MaxWalkAngle { get; set; } = 50.0f;
        public float WalkingMovementSpeed { get; set; } = 0.17f;
        public float JumpSpeed { get; set; } = 8.0f;
        public float FallingMovementSpeed { get; set; } = 10.0f;
        public bool AlignInputToGround { get; set; } = true;
        public bool IsCrouched { get; set; } = false;
        public Quaternion UpToGroundNormalRotation { get; set; } = Quaternion.Identity;
        public float AllowJumpTimeDelta { get; set; }
        public float AllowJumpExtraTime { get; set; } = 1.0f;
        public Vector3 GroundNormal
        {
            get => _groundNormal;
            private set
            {
                _groundNormal = value;
                UpToGroundNormalRotation = XRMath.RotationBetweenVectors(Globals.Up, GroundNormal);
            }
        }
        public EMovementMode CurrentMovementMode
        {
            get => _currentMovementMode;
            protected set
            {
                if (_currentMovementMode == value)
                    return;

                if (SceneNode.TryGetComponent(out CapsuleYComponent? root) && root?.CollisionObject is XRRigidBody body)
                {
                    body.SimulatingPhysics = true;
                    switch (value)
                    {
                        case EMovementMode.Walking:

                            _justJumped = false;
                            //_velocity = root.PhysicsDriver.CollisionObject.LinearVelocity;
                            //body.SimulatingPhysics = false;
                            body.IsKinematic = true;
                            //Physics simulation updates the world matrix, but not its components (translation, for example)
                            //Do that now
                            root.TransformAs<Transform>().Translation = root.Transform.WorldTranslation;
                            
                            _subUpdateTick = TickWalking;
                            break;
                        case EMovementMode.Falling:

                            if (_postWalkAllowJump = _currentMovementMode == EMovementMode.Walking && !_justJumped)
                            {
                                AllowJumpTimeDelta = 0.0f;
                                _velocity.Y = 0.0f;
                            }

                            body.IsKinematic = false;
                            body.LinearVelocity = _velocity;

                            CurrentWalkingSurface = null;

                            _subUpdateTick = TickFalling;
                            break;
                    }
                }
                _currentMovementMode = value;
            }
        }
        public XRCollisionObject? CurrentWalkingSurface
        {
            get => _currentWalkingSurface;
            set
            {
                if (_currentWalkingSurface?.Owner is XRComponent comp1)
                    comp1.Transform.WorldMatrixChanged.RemoveListener(FloorTransformChanged);

                SetField(ref _currentWalkingSurface, value);

                if (_currentWalkingSurface?.Owner is XRComponent comp2)
                    comp2.Transform.WorldMatrixChanged.AddListener(FloorTransformChanged);
            }
        }

        protected internal override void Start()
        {
            CurrentWalkingSurface = null;
            _subUpdateTick = TickFalling;
            RegisterTick(ETickGroup.PrePhysics, (int)ETickOrder.Input, MainUpdateTick);
        }

        protected internal override void Stop()
        {
            _subUpdateTick = null;
            UnregisterTick(ETickGroup.PrePhysics, (int)ETickOrder.Input, MainUpdateTick);
        }

        private void FloorTransformChanged(TransformBase floor)
        {
            //TODO: calculate relative transform from floor world transform to character world transform.
            //Use that to move the character with the floor.

            //TODO: change to falling if ground accelerates down with gravity faster than the character
            //CapsuleYComponent root = OwningActor.RootComponent as CapsuleYComponent;
            //ISceneComponent comp = (ISceneComponent)_currentWalkingSurface.Owner;
            ////Matrix4x4 transformDelta = comp.PreviousInverseWorldMatrix * comp.WorldMatrix;
            //root.WorldMatrix.Value = root.WorldMatrix * comp.PreviousInverseWorldMatrix * comp.WorldMatrix;
            //Vector3 point = newWorldMatrix.Translation;

            //root.Translation = point;
            //root.Rotation.Yaw += transformDelta.ExtractRotation(true).ToYawPitchRoll().Yaw;
        }
        private void MainUpdateTick()
        {
            if (_postWalkAllowJump)
            {
                AllowJumpTimeDelta += Engine.Delta;
                if (AllowJumpTimeDelta > AllowJumpExtraTime)
                    _postWalkAllowJump = false;
            }
            _subUpdateTick?.Invoke(ConsumeInput());
        }
        protected virtual void TickWalking(Vector3 movementInput)
        {
            Matrix4x4 inputTransform;
            CapsuleYComponent root = RootCapsule!;
            Transform rootTfm = root.TransformAs<Transform>();

            XRCollisionShape? shape = root.CollisionObject?.CollisionShape;
            XRRigidBody? body = root.CollisionObject as XRRigidBody;
            
            _prevPosition = rootTfm.Translation;

            //Use gravity currently affecting this body
            Vector3 gravity = body?.Gravity ?? Vector3.Zero;

            Vector3 down = gravity;
            down = Vector3.Normalize(down);
            Vector3 stepUpVector = -VerticalStepUpHeight * down;
            Matrix4x4 stepUpMatrix = Matrix4x4.CreateTranslation(stepUpVector);

            //Add input

            Quaternion groundRot = AlignInputToGround ? UpToGroundNormalRotation : Quaternion.Identity;

            _closestTrace.Shape = shape;

            #region Movement input
            while (true)
            {
                if (movementInput != Vector3.Zero)
                {
                    Vector3 finalInput = Vector3.Transform(movementInput * WalkingMovementSpeed, groundRot);
                    groundRot = Quaternion.Identity;
                    inputTransform = Matrix4x4.CreateTranslation(finalInput);

                    Matrix4x4 wm = root.Transform.WorldMatrix;
                    _closestTrace.Start = stepUpMatrix * wm;
                    _closestTrace.End = stepUpMatrix * inputTransform * wm;
                    
                    if (SceneNode.World?.PhysicsScene?.Trace(_closestTrace) ?? false)
                    {
                        if (_closestTrace.HitFraction.IsZero())
                        {
                            Vector3 hitNormal = _closestTrace.HitNormalWorld;
                            finalInput.Normalize();
                            float dot = Vector3.Dot(hitNormal, finalInput);
                            if (dot < 0.0f)
                            {
                                //running left is up, right is down
                                Vector3 up = Vector3.Cross(finalInput, hitNormal);
                                Vector3 newMovement = Vector3.Cross(hitNormal, up);
                                if (!newMovement.Equals(movementInput))
                                {
                                    movementInput = newMovement;
                                    continue;
                                }
                            }
                            break;
                        }

                        float hitF = _closestTrace.HitFraction;

                        //Something is in the way
                        rootTfm.Translation += finalInput * hitF;
                        
                        Vector3 normal = _closestTrace.HitNormalWorld;
                        if (IsSurfaceNormalWalkable(normal))
                        {
                            GroundNormal = normal;
                            groundRot = UpToGroundNormalRotation;

                            XRRigidBody? rigidBody = _closestTrace.CollisionObject as XRRigidBody;
                            //if (CurrentWalkingSurface == d)
                            //    break;

                            CurrentWalkingSurface = rigidBody;
                            if (!(hitF - 1.0f).IsZero())
                            {
                                float invHitF = 1.0f - hitF;
                                movementInput = movementInput * invHitF;
                                continue;
                            }
                        }
                        else
                        {
                            finalInput = finalInput.Normalize();
                            float dot = Vector3.Dot(normal, finalInput);
                            if (dot < 0.0f)
                            {
                                //running left is up, right is down
                                Vector3 up = finalInput.Cross(normal);
                                movementInput = normal.Cross(up);
                                continue;
                            }
                        }
                    }
                    else
                        rootTfm.Translation += finalInput;
                }
                break;
            }
            #endregion

            //Test for walkable ground

            #region Ground Test

            float centerToGroundDist = root.Shape.HalfHeight + root.Shape.Radius;
            float marginDist = (CurrentWalkingSurface?.CollisionShape?.Margin ?? 0.0f) + (body?.CollisionShape?.Margin ?? 0.0f);
            float groundTestDist = 2.0f;// centerToGroundDist + marginDist;
            down *= groundTestDist;
            inputTransform = Matrix4x4.CreateTranslation(down);

            _closestTrace.Start = stepUpMatrix * root.Transform.WorldMatrix;
            _closestTrace.End = stepUpMatrix * inputTransform * root.Transform.WorldMatrix;

            bool traceSuccess = World?.PhysicsScene?.Trace(_closestTrace) ?? false;
            bool walkSuccess = traceSuccess && IsSurfaceNormalWalkable(_closestTrace.HitNormalWorld);
            if (!walkSuccess)
            {
                //Engine.Out(traceSuccess ? "walk surface failed" : "walk trace failed");

                CurrentMovementMode = EMovementMode.Falling;
                return;
            }

            _worldGroundContactPoint = _closestTrace.HitPointWorld;
            //Vector3 diff = Vector3.Lerp(stepUpVector, down, _closestTrace.HitFraction);
            rootTfm.Translation = new Vector3(rootTfm.Translation.X, _worldGroundContactPoint.Y + centerToGroundDist, rootTfm.Translation.Z);

            GroundNormal = _closestTrace.HitNormalWorld;
            CurrentWalkingSurface = _closestTrace.CollisionObject as XRRigidBody;

            #endregion

            if (root.CollisionObject is not null)
                root.CollisionObject.WorldTransform = root.Transform.WorldMatrix;

            _prevVelocity = _velocity;
            _position = root.Transform.WorldTranslation;
            _velocity = (_position - _prevPosition) / Engine.Delta;
            _acceleration = (_velocity - _prevVelocity) / Engine.Delta;
        }
        protected virtual void TickFalling(Vector3 movementInput)
        {
            if (RootCapsule!.CollisionObject is not XRRigidBody body)
                return;
            
            Vector3 v = body.LinearVelocity;
            if (v.XZ().Length() < 8.667842f)
                body.ApplyCentralForce((body.Mass * FallingMovementSpeed) * movementInput);
        }
        public void Jump()
        {
            //Nothing to jump OFF of?
            if (_currentMovementMode != EMovementMode.Walking && !_postWalkAllowJump)
                return;

            //Get root component of the character
            IGenericCollidable root = RootCapsule as IGenericCollidable;

            //If the root has no rigid body, the player can't jump
            if (root?.CollisionObject is not XRRigidBody chara)
                return;

            _postWalkAllowJump = false;
            _justJumped = true;
            
            Vector3 up = chara.Gravity;
            up = Vector3.Normalize(up);
            up = -up;

            if (_postWalkAllowJump = _currentMovementMode == EMovementMode.Walking && !_justJumped)
            {
                AllowJumpTimeDelta = 0.0f;
                _velocity.Y = 0.0f;
            }

            chara.SimulatingPhysics = true;
            _subUpdateTick = TickFalling;

            if (_currentWalkingSurface != null)
                chara.Translate(up * 2.0f);

            chara.LinearVelocity = _velocity;

            if (_currentWalkingSurface != null && 
                _currentWalkingSurface is XRRigidBody rigid &&
                rigid.SimulatingPhysics &&
                rigid.LinearFactor != Vector3.Zero)
            {
                //TODO: calculate push off force using ground's mass.
                //For example, you can't jump off a piece of debris.
                float surfaceMass = rigid.Mass;
                float charaMass = chara.Mass;
                Vector3 surfaceVelInitial = rigid.LinearVelocity;
                Vector3 charaVelInitial = chara.LinearVelocity;

                Vector3 charaVelFinal = up * JumpSpeed;
                Vector3 surfaceVelFinal = (surfaceMass * surfaceVelInitial + charaMass * charaVelInitial - charaMass * charaVelFinal) / surfaceMass;

                Vector3 surfaceImpulse = (surfaceVelFinal - surfaceVelInitial) * surfaceMass;
                rigid.ApplyImpulse(surfaceImpulse, Vector3.Transform(_worldGroundContactPoint, rigid.WorldTransform.Inverted()));
                chara.ApplyCentralImpulse(charaVelFinal * (1.0f / chara.Mass));
            }
            else
            {
                //The ground isn't movable, so just apply the jump force directly.
                //impulse = mass * velocity change
                chara.ApplyCentralImpulse(up * (JumpSpeed * chara.Mass));
            }

            CurrentMovementMode = EMovementMode.Falling;
            CurrentWalkingSurface = null;
        }
        public void EndJump()
        {

        }
        public void ToggleCrouch()
        {

        }
        public void SetCrouched()
        {

        }
        public bool IsSurfaceNormalWalkable(Vector3 normal)
        {
            //TODO: use friction between surfaces, not just a constant angle
            return XRMath.AngleBetween(Globals.Up, normal) <= MaxWalkAngle;
        }
        public void OnHit(XRCollisionObject other, XRContactInfo point, bool thisIsA)
        {
            Vector3 normal;
            if (thisIsA)
            {
                _worldGroundContactPoint = point.PositionWorldOnB;
                normal = point.NormalWorldOnB;
            }
            else
            {
                _worldGroundContactPoint = point.PositionWorldOnA;
                normal = -point.NormalWorldOnB;
            }
            normal = Vector3.Normalize(normal);
            if (CurrentMovementMode == EMovementMode.Falling)
            {
                if (IsSurfaceNormalWalkable(normal))
                {
                    CurrentWalkingSurface = other;
                    CurrentMovementMode = EMovementMode.Walking;
                    RootCapsule!.TransformAs<Transform>().Translation += normal * -point.Distance;
                }
            }
            else if (CurrentMovementMode == EMovementMode.Walking)
            {
                //other.Activate();
            }
        }
        public void OnContactEnded(XRRigidBody other)
        {

        }
    }
}