using System.Numerics;
using XREngine.Physics;
using XREngine.Physics.ContactTesting;
using XREngine.Physics.ShapeTracing;
using XREngine.Rendering;

namespace XREngine
{
    public static partial class Engine
    {
        public static class Physics
        {
            public static XRCollisionSphere NewSphere(float radius)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionBox NewBox(Vector3 halfExtents)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionCapsuleX NewCapsuleX(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionCapsuleY NewCapsuleY(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionCapsuleZ NewCapsuleZ(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionCylinderX NewCylinderX(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionCylinderY NewCylinderY(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionCylinderZ NewCylinderZ(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionConeX NewConeX(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionConeY NewConeY(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionConeZ NewConeZ(float radius, float height)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionConvexHull NewConvexHull(Vector3[] vertices)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionConvexHull NewConvexHull(IEnumerable<Vector3> vertices)
            {
                throw new NotImplementedException();
            }
            public static XRCollisionHeightField NewHeightField(
                int heightStickWidth,
                int heightStickLength,
                Stream heightfieldData,
                float heightScale,
                float minHeight,
                float maxHeight,
                int upAxis,
                XRCollisionHeightField.EHeightValueType heightDataType,
                bool flipQuadEdges)
            {
                throw new NotImplementedException();
            }
            internal static XRCollisionCompoundShape NewCompoundShape((Matrix4x4 localTransform, XRCollisionShape shape)[] shapes)
            {
                throw new NotImplementedException();
            }

            internal static XRSoftBody NewSoftBody(TSoftBodyConstructionInfo info)
            {
                throw new NotImplementedException();
            }

            internal static XRRigidBody NewRigidBody(RigidBodyConstructionInfo info)
            {
                throw new NotImplementedException();
            }

            //internal static TGhostBody NewGhostBody(TGhostBodyConstructionInfo info)
            //{
            //    throw new NotImplementedException();
            //}

            internal static bool ContactTest(ContactTest contactTest, XRWorldInstance world)
            {
                throw new NotImplementedException();
            }

            internal static bool ShapeTrace(ShapeTrace shapeTrace, XRWorldInstance world)
            {
                throw new NotImplementedException();
            }

            internal static XRPointPointConstraint NewPointPointConstraint(XRRigidBody rigidBodyA, Vector3 pivotInA)
            {
                throw new NotImplementedException();
            }

            internal static XRPointPointConstraint NewPointPointConstraint(XRRigidBody rigidBodyA, XRRigidBody rigidBodyB, Vector3 pivotInA, Vector3 pivotInB)
            {
                throw new NotImplementedException();
            }
        }
    }
}
