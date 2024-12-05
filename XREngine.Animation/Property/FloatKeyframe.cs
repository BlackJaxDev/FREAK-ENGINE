using Extensions;
using XREngine.Data;
using XREngine.Data.Animation;

namespace XREngine.Animation
{
    public class FloatKeyframe : VectorKeyframe<float>
    {
        public FloatKeyframe()
            : this(0.0f, 0.0f, 0.0f, EVectorInterpType.Smooth) { }
        public FloatKeyframe(int frameIndex, float FPS, float inValue, float outValue, float inTangent, float outTangent, EVectorInterpType type)
            : this(frameIndex / FPS, inValue, outValue, inTangent, outTangent, type) { }
        public FloatKeyframe(int frameIndex, float FPS, float inoutValue, float inoutTangent, EVectorInterpType type)
            : this(frameIndex / FPS, inoutValue, inoutValue, inoutTangent, inoutTangent, type) { }
        public FloatKeyframe(float second, float inoutValue, float inoutTangent, EVectorInterpType type)
            : this(second, inoutValue, inoutValue, inoutTangent, inoutTangent, type) { }
        public FloatKeyframe(float second, float inoutValue, float inTangent, float outTangent, EVectorInterpType type)
            : this(second, inoutValue, inoutValue, inTangent, outTangent, type) { }
        public FloatKeyframe(float second, float inValue, float outValue, float inTangent, float outTangent, EVectorInterpType type)
            : base(second, inValue, outValue, inTangent, outTangent, type) { }

        public override float LerpOut(VectorKeyframe<float>? next, float diff, float span)
            => Interp.Lerp(OutValue, next?.InValue ?? 0.0f, span.IsZero() ? 0.0f : diff / span);
        public override float LerpVelocityOut(VectorKeyframe<float>? next, float diff, float span)
            => span.IsZero() ? 0.0f : ((next?.InValue ?? 0.0f) - OutValue) / (diff / span);

        public override float LerpIn(VectorKeyframe<float>? prev, float diff, float span)
            => Interp.Lerp(prev?.OutValue ?? 0.0f, InValue, span.IsZero() ? 0.0f : diff / span);
        public override float LerpVelocityIn(VectorKeyframe<float>? prev, float diff, float span)
            => span.IsZero() ? 0.0f : (InValue - (prev?.OutValue ?? 0.0f)) / (diff / span);

        public override float CubicBezierOut(VectorKeyframe<float>? next, float diff, float span)
            => Interp.CubicBezier(
                OutValue,
                OutValue + OutTangent * span,
                (next?.InValue ?? 0.0f) + (next?.InTangent ?? 0.0f) * span,
                next?.InValue ?? 0.0f,
                span.IsZero() ? 0.0f : diff / span);

        public override float CubicBezierVelocityOut(VectorKeyframe<float>? next, float diff, float span)
            => span.IsZero() 
            ? 0.0f 
            : Interp.CubicBezierVelocity(
                OutValue,
                OutValue + OutTangent * span,
                (next?.InValue ?? 0.0f) + (next?.InTangent ?? 0.0f) * span,
                next?.InValue ?? 0.0f,
                diff / span) / span;

        public override float CubicBezierAccelerationOut(VectorKeyframe<float>? next, float diff, float span)
            => span.IsZero() 
            ? 0.0f
            : Interp.CubicBezierAcceleration(
                OutValue,
                OutValue + OutTangent * span,
                (next?.InValue ?? 0.0f) + (next?.InTangent ?? 0.0f) * span,
                next?.InValue ?? 0.0f,
                diff / span) / (span * span);

        public override float CubicBezierIn(VectorKeyframe<float>? prev, float diff, float span)
            => Interp.CubicBezier(
                prev?.OutValue ?? 0.0f,
                (prev?.OutValue ?? 0.0f) + (prev?.OutTangent ?? 0.0f) * span,
                InValue + InTangent * span,
                InValue,
                span.IsZero() ? 0.0f : diff / span);

        public override float CubicBezierVelocityIn(VectorKeyframe<float>? prev, float diff, float span)
            => span.IsZero() 
            ? 0.0f 
            : Interp.CubicBezierVelocity(
                prev?.OutValue ?? 0.0f,
                (prev?.OutValue ?? 0.0f) + (prev?.OutTangent ?? 0.0f) * span,
                InValue + InTangent * span,
                InValue,
                diff / span) / span;

        public override float CubicBezierAccelerationIn(VectorKeyframe<float>? prev, float diff, float span)
            => span.IsZero()
            ? 0.0f 
            : Interp.CubicBezierAcceleration(
                prev?.OutValue ?? 0.0f,
                (prev?.OutValue ?? 0.0f) + (prev?.OutTangent ?? 0.0f) * span,
                InValue + InTangent * span,
                InValue,
                diff / span) / (span * span);

        public override string WriteToString()
            => $"{Second} {InValue} {OutValue} {InTangent} {OutTangent} {InterpolationTypeIn} {InterpolationTypeOut}";

        public override string ToString()
            => $"[S:{Second}] V:({InValue} {OutValue}) T:([{InTangent} {InterpolationTypeIn}] [{OutTangent} {InterpolationTypeOut}])";
            
        public override void ReadFromString(string str)
        {
            string[] parts = str.Split(' ');
            Second = float.Parse(parts[0]);
            InValue = float.Parse(parts[1]);
            OutValue = float.Parse(parts[2]);
            InTangent = float.Parse(parts[3]);
            OutTangent = float.Parse(parts[4]);
            InterpolationTypeIn = parts[5].AsEnum<EVectorInterpType>();
            InterpolationTypeOut = parts[6].AsEnum<EVectorInterpType>();
        }
        
        public override void MakeOutLinear()
        {
            VectorKeyframe<float>? next = Next;
            float span;
            if (next is null)
            {
                if (OwningTrack != null && OwningTrack.FirstKey != this)
                {
                    next = OwningTrack?.FirstKey as VectorKeyframe<float>;
                    span = (OwningTrack?.LengthInSeconds ?? 0.0f) - Second + (next?.Second ?? 0.0f);
                }
                else
                    return;
            }
            else
                span = next.Second - Second;

            OutTangent = ((next?.InValue ?? 0.0f) - OutValue) / span;
        }
        public override void MakeInLinear()
        {
            var prev = Prev;
            float span;
            if (prev is null)
            {
                if (OwningTrack != null && OwningTrack.LastKey != this)
                {
                    prev = OwningTrack?.LastKey as VectorKeyframe<float>;
                    span = (OwningTrack?.LengthInSeconds ?? 0.0f) - (prev?.Second ?? 0.0f) + Second;
                }
                else
                    return;
            }
            else
                span = Second - (prev?.Second ?? 0.0f);

            InTangent = -(InValue - (prev?.OutValue ?? 0.0f)) / span;
        }

        public override void UnifyTangentDirections(EUnifyBias bias) => UnifyTangents(bias);
        public override void UnifyTangentMagnitudes(EUnifyBias bias) => UnifyTangents(bias);
        
        public override void UnifyTangents(EUnifyBias bias)
        {
            switch (bias)
            {
                case EUnifyBias.Average:
                    float avg = (-InTangent + OutTangent) * 0.5f;
                    OutTangent = avg;
                    InTangent = -avg;
                    break;
                case EUnifyBias.In:
                    OutTangent = -InTangent;
                    break;
                case EUnifyBias.Out:
                    InTangent = -OutTangent;
                    break;
            }
        }
        public override void UnifyValues(EUnifyBias bias)
        {
            switch (bias)
            {
                case EUnifyBias.Average:
                    InValue = OutValue = (InValue + OutValue) / 2.0f;
                    break;
                case EUnifyBias.In:
                    OutValue = InValue;
                    break;
                case EUnifyBias.Out:
                    InValue = OutValue;
                    break;
            }
        }

        public void GenerateTangents()
        {
            var next = GetNextKeyframe(out float nextSpan);
            var prev = GetPrevKeyframe(out float prevSpan);

            if (Math.Abs(InValue - OutValue) < 0.0001f)
            {
                float tangent = 0.0f;
                float weightCount = 0;
                if (prev != null && prevSpan > 0.0f)
                {
                    tangent += (InValue - prev.OutValue) / prevSpan;
                    weightCount++;
                }
                if (next != null && nextSpan > 0.0f)
                {
                    tangent += (next.InValue - OutValue) / nextSpan;
                    weightCount++;
                }

                if (weightCount > 0)
                    tangent /= weightCount;

                OutTangent = tangent;
                InTangent = -tangent;
            }
            else
            {
                if (prev != null && prevSpan > 0.0f)
                {
                    InTangent = -(InValue - prev.OutValue) / prevSpan;
                }
                if (next != null && nextSpan > 0.0f)
                {
                    OutTangent = (next.InValue - OutValue) / nextSpan;
                }
            }

            //float valueDiff = (next?.InValue ?? InValue) - (prev?.OutValue ?? OutValue);
            //float secDiff = (next?.Second ?? Second) - (prev?.Second ?? Second);
            //if (secDiff != 0.0f)
            //    InTangent = -(OutTangent = valueDiff / secDiff);
        }
        public void GenerateOutTangent()
        {
            var next = GetNextKeyframe(out float nextSpan);
            if (next != null && nextSpan > 0.0f)
            {
                OutTangent = (next.InValue - OutValue) / nextSpan;
            }

            //var next = GetNextKeyframe(out float span1);
            //var prev = GetPrevKeyframe(out float span2);
            //float valueDiff = (next?.InValue ?? InValue) - (prev?.OutValue ?? OutValue);
            //float secDiff = (next?.Second ?? Second) - (prev?.Second ?? Second);
            //if (secDiff != 0.0f)
            //    OutTangent = valueDiff / secDiff;
        }
        public void GenerateInTangent()
        {
            var prev = GetPrevKeyframe(out float prevSpan);
            if (prev != null && prevSpan > 0.0f)
            {
                InTangent = -(InValue - prev.OutValue) / prevSpan;
            }

            //var next = GetNextKeyframe(out float span1);
            //var prev = GetPrevKeyframe(out float span2);
            //float valueDiff = (next?.InValue ?? InValue) - (prev?.OutValue ?? OutValue);
            //float secDiff = (next?.Second ?? Second) - (prev?.Second ?? Second);
            //if (secDiff != 0.0f)
            //    InTangent = -valueDiff / secDiff;
        }
        public void GenerateAdjacentTangents(bool prev, bool next)
        {
            if (prev)
            {
                var prevkf = GetPrevKeyframe(out float span2) as FloatKeyframe;
                prevkf?.GenerateTangents();
                GenerateInTangent();
            }
            if (next)
            {
                var nextKf = GetNextKeyframe(out float span1) as FloatKeyframe;
                nextKf?.GenerateTangents();
                GenerateOutTangent();
            }
        }

        public override float LerpValues(float a, float b, float t)
            => a + (b - a) * t;
    }
}
