namespace XREngine
{
    public static partial class Engine
    {
        public static partial class Rendering
        {
            public static class Constants
            {
                public const string ShadowExponentBaseUniform = "ShadowBase";
                public const string ShadowExponentUniform = "ShadowMult";
                public const string ShadowBiasMinUniform = "ShadowBiasMin";
                public const string ShadowBiasMaxUniform = "ShadowBiasMax";

                public const string BoneTransformsName = "Transforms";
                //public const string BoneNrmMtxName = "BoneNrmMtx";
                public const string MorphWeightsName = "MorphWeights";
                public const string LightsStructName = "LightData";
            }
        }
    }
}