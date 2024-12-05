using YamlDotNet.Serialization;

namespace Unity
{
    public class UnityStaticContext : StaticContext
    {
        /// <summary>
        /// Gets whether the type is known to the context
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns></returns>
        public override bool IsKnownType(Type type)
        {
            return type switch
            {
                Type t when t == typeof(UnityAnimationClip) => true,
                Type t when t == typeof(UnityAnimationClip.Wrapper) => true,
                Type t when t == typeof(Curve) => true,
                Type t when t == typeof(FloatCurve) => true,
                Type t when t == typeof(CurveData) => true,
                Type t when t == typeof(CurveKey) => true,
                Type t when t == typeof(UnityBounds) => true,
                Type t when t == typeof(UnityVector3) => true,
                Type t when t == typeof(ClipBindingConstant) => true,
                Type t when t == typeof(GenericBinding) => true,
                Type t when t == typeof(AnimationClipSettings) => true,
                Type t when t == typeof(AdditiveReferencePoseClip) => true,
                Type t when t == typeof(Event) => true,
                _ => false,
            };
        }

        ///// <summary>
        ///// Gets the <see cref="ITypeResolver"/> to use for serialization
        ///// </summary>
        ///// <returns></returns>
        //public override ITypeResolver GetTypeResolver()
        //{

        //}

        ///// <summary>
        ///// Gets the factory to use for serialization and deserialization
        ///// </summary>
        ///// <returns></returns>
        //public override StaticObjectFactory GetFactory()
        //{

        //}

        ///// <summary>
        ///// Gets the type inspector to use when statically serializing/deserializing YAML.
        ///// </summary>
        ///// <returns></returns>
        //public override ITypeInspector GetTypeInspector()
        //{

        //}
    }
}
