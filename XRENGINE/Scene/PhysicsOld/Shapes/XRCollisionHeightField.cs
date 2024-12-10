using System.Numerics;
using XREngine.Data.Colors;

namespace XREngine.Physics
{
    public abstract class XRCollisionHeightField : XRCollisionShape
    {
        public enum EHeightValueType
        {
            Single = 0,
            Double = 1,
            Int32 = 2,
            Int16 = 3,
            FixedPoint88 = 4,
            Byte = 5
        }
        
        public static XRCollisionHeightField New(
            int heightStickWidth,
            int heightStickLength,
            Stream heightfieldData,
            float heightScale,
            float minHeight,
            float maxHeight,
            int upAxis,
            EHeightValueType heightDataType,
            bool flipQuadEdges)
            => Engine.Physics.NewHeightField(
                heightStickWidth,
                heightStickLength,
                heightfieldData,
                heightScale,
                minHeight,
                maxHeight,
                upAxis,
                heightDataType,
                flipQuadEdges);

        public override void Render(Matrix4x4 worldTransform, ColorF4 color, bool solid)
        {

        }
    }
}
