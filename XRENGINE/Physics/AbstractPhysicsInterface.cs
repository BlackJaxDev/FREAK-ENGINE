using System.Numerics;
using XREngine.Data.Core;

namespace XREngine.Physics
{
    public abstract class AbstractPhysicsInterface : XRBase
    {
        /// <summary>
        /// Creates a new physics scene using the specified physics library.
        /// </summary>
        /// <returns>A new scene to populate with physics bodies and constraints.</returns>
        public abstract AbstractPhysicsWorld NewScene();

        /// <summary>
        /// Creates a new rigid body using the specified physics library.
        /// </summary>
        /// <param name="info">Construction information.</param>
        /// <returns>A new rigid body.</returns>
        public abstract XRRigidBody NewRigidBody(RigidBodyConstructionInfo info);

        //public abstract TGhostBody NewGhostBody(TGhostBodyConstructionInfo info);

        /// <summary>
        /// Creates a new soft body using the specified physics library.
        /// </summary>
        /// <param name="info">Construction information.</param>
        /// <returns>A new soft body.</returns>
        public abstract XRSoftBody NewSoftBody(TSoftBodyConstructionInfo info);

        #region Shapes
        public abstract XRCollisionBox NewBox(Vector3 halfExtents);
        public abstract XRCollisionSphere NewSphere(float radius);

        public abstract XRCollisionConeX NewConeX(float radius, float height);
        public abstract XRCollisionConeY NewConeY(float radius, float height);
        public abstract XRCollisionConeZ NewConeZ(float radius, float height);

        public abstract XRCollisionCylinderX NewCylinderX(float radius, float height);
        public abstract XRCollisionCylinderY NewCylinderY(float radius, float height);
        public abstract XRCollisionCylinderZ NewCylinderZ(float radius, float height);

        public abstract XRCollisionCapsuleX NewCapsuleX(float radius, float height);
        public abstract XRCollisionCapsuleY NewCapsuleY(float radius, float height);
        public abstract XRCollisionCapsuleZ NewCapsuleZ(float radius, float height);

        public abstract XRCollisionCompoundShape NewCompoundShape((Matrix4x4 localTransform, XRCollisionShape shape)[] shapes);
        public abstract XRCollisionConvexHull NewConvexHull(IEnumerable<Vector3> points);
        public abstract XRCollisionHeightField NewHeightField(
            int heightStickWidth, int heightStickLength, nint heightfieldData,
            float heightScale, float minHeight, float maxHeight,
            int upAxis, XRCollisionHeightField.EHeightValueType heightDataType, bool flipQuadEdges);

        #endregion

        #region Constraints
        public abstract XRPointPointConstraint NewPointPointConstraint(XRRigidBody rigidBodyA, XRRigidBody rigidBodyB, Vector3 pivotInA, Vector3 pivotInB);
        public abstract XRPointPointConstraint NewPointPointConstraint(XRRigidBody rigidBodyA, Vector3 pivotInA);
        #endregion
    }
}