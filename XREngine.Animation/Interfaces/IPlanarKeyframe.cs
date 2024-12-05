using XREngine.Data.Animation;

namespace XREngine.Animation
{
    public interface IPlanarKeyframe : IKeyframe
    {
        object InValue { get; set; }
        object OutValue { get; set; }
        object InTangent { get; set; }
        object OutTangent { get; set; }
        EVectorInterpType InterpolationTypeOut { get; set; }

        void UnifyKeyframe(EUnifyBias bias);
        void UnifyValues(EUnifyBias bias);
        void UnifyTangents(EUnifyBias bias);
        void UnifyTangentDirections(EUnifyBias bias);
        void UnifyTangentMagnitudes(EUnifyBias bias);
        void MakeOutLinear();
        void MakeInLinear();
        //void ParsePlanar(string inValue, string outValue, string inTangent, string outTangent);
        //void WritePlanar(out string inValue, out string outValue, out string inTangent, out string outTangent);
    }
}
