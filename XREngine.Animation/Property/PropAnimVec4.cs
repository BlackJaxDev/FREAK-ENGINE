using Extensions;
using System.ComponentModel;
using System.Numerics;
using XREngine.Data;
using XREngine.Data.Animation;
using XREngine.Data.Core;

namespace XREngine.Animation
{
    public class PropAnimVector4 : PropAnimVector<Vector4, Vector4Keyframe>
    {
        public PropAnimVector4() : base() { }
        public PropAnimVector4(float lengthInSeconds, bool looped, bool useKeyframes)
            : base(lengthInSeconds, looped, useKeyframes) { }
        public PropAnimVector4(int frameCount, float FPS, bool looped, bool useKeyframes)
            : base(frameCount, FPS, looped, useKeyframes) { }

        protected override Vector4 LerpValues(Vector4 t1, Vector4 t2, float time) => Vector4.Lerp(t1, t2, time);
        protected override float[] GetComponents(Vector4 value) => new float[] { value.X, value.Y, value.Z, value.W };
        protected override Vector4 GetMaxValue() => new Vector4(float.MaxValue);
        protected override Vector4 GetMinValue() => new Vector4(float.MinValue);
        protected override float GetVelocityMagnitude()
        {
            Vector4 b = CurrentVelocity;
            float a = 1.0f;
            Vec5 start = Vec5.Zero;
            Vec5 end = new Vec5(a, b.X, b.Y, b.Z, b.W);
            return start.DistanceTo(end);
        }
        private struct Vec5
        {
            public float X, Y, Z, U, V;

            public static readonly Vec5 Zero = new Vec5();

            public Vec5(float x, float y, float z, float u, float v)
            {
                X = x;
                Y = y;
                Z = z;
                U = u;
                V = v;
            }

            [Browsable(false)]
            public float LengthSquared => X * X + Y * Y + Z * Z + U * U + V * V;
            [Browsable(false)]
            public float Length => (float)Math.Sqrt(LengthSquared);
            [Browsable(false)]
            public float LengthFast => 1.0f / XRMath.InverseSqrtFast(LengthSquared);

            public float DistanceTo(Vec5 point) => (point - this).Length;
            public float DistanceToFast(Vec5 point) => (point - this).LengthFast;
            public float DistanceToSquared(Vec5 point) => (point - this).LengthSquared;

            public static Vec5 operator -(Vec5 left, Vec5 right)
                => new Vec5(
                    left.X - right.X,
                    left.Y - right.Y,
                    left.Z - right.Z,
                    left.U - right.U,
                    left.V - right.V);
        }
    }
    public class Vector4Keyframe : VectorKeyframe<Vector4>
    {
        public Vector4Keyframe()
          : this(0.0f, Vector4.Zero, Vector4.Zero, EVectorInterpType.Smooth) { }
        public Vector4Keyframe(int frameIndex, float FPS, Vector4 inValue, Vector4 outValue, Vector4 inTangent, Vector4 outTangent, EVectorInterpType type)
            : this(frameIndex / FPS, inValue, outValue, inTangent, outTangent, type) { }
        public Vector4Keyframe(int frameIndex, float FPS, Vector4 inoutValue, Vector4 inoutTangent, EVectorInterpType type)
            : this(frameIndex / FPS, inoutValue, inoutValue, inoutTangent, inoutTangent, type) { }
        public Vector4Keyframe(float second, Vector4 inoutValue, Vector4 inoutTangent, EVectorInterpType type)
            : this(second, inoutValue, inoutValue, inoutTangent, inoutTangent, type) { }
        public Vector4Keyframe(float second, Vector4 inoutValue, Vector4 inTangent, Vector4 outTangent, EVectorInterpType type)
            : this(second, inoutValue, inoutValue, inTangent, outTangent, type) { }
        public Vector4Keyframe(float second, Vector4 inValue, Vector4 outValue, Vector4 inTangent, Vector4 outTangent, EVectorInterpType type)
            : base(second, inValue, outValue, inTangent, outTangent, type) { }

        public override Vector4 Lerp(VectorKeyframe<Vector4> next, float diff, float span)
            => Interp.Lerp(OutValue, next.InValue, span.IsZero() ? 0.0f : diff / span);
        public override Vector4 LerpVelocity(VectorKeyframe<Vector4> next, float diff, float span)
            => span.IsZero() ? Vector4.Zero : (next.InValue - OutValue) / (diff / span);

        public override Vector4 CubicBezier(VectorKeyframe<Vector4> next, float diff, float span)
            => Interp.CubicBezier(OutValue, OutValue + OutTangent * span, next.InValue + next.InTangent * span, next.InValue, span.IsZero() ? 0.0f : diff / span);
        public override Vector4 CubicBezierVelocity(VectorKeyframe<Vector4> next, float diff, float span)
            => Interp.CubicBezierVelocity(OutValue, OutValue + OutTangent * span, next.InValue + next.InTangent * span, next.InValue, span.IsZero() ? 0.0f : diff / span);
        public override Vector4 CubicBezierAcceleration(VectorKeyframe<Vector4> next, float diff, float span)
            => Interp.CubicBezierAcceleration(OutValue, OutValue + OutTangent * span, next.InValue + next.InTangent * span, next.InValue, span.IsZero() ? 0.0f : diff / span);

        public override string WriteToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17}", Second, InValue.X, InValue.Y, InValue.Z, InValue.W, OutValue.X, OutValue.Y, OutValue.Z, OutValue.W, InTangent.X, InTangent.Y, InTangent.Z, InTangent.W, OutTangent.X, OutTangent.Y, OutTangent.Z, OutTangent.W, InterpolationType);
            //return string.Format("{0} {1} {2} {3} {4} {5}", Second, InValue.WriteToString(), OutValue.WriteToString(), InTangent.WriteToString(), OutTangent.WriteToString(), InterpolationType);
        }
        public override void ReadFromString(string str)
        {
            string[] parts = str.Split(' ');
            Second = float.Parse(parts[0]);
            InValue = new Vector4(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]));
            OutValue = new Vector4(float.Parse(parts[5]), float.Parse(parts[6]), float.Parse(parts[7]), float.Parse(parts[8]));
            InTangent = new Vector4(float.Parse(parts[9]), float.Parse(parts[10]), float.Parse(parts[11]), float.Parse(parts[12]));
            OutTangent = new Vector4(float.Parse(parts[13]), float.Parse(parts[14]), float.Parse(parts[15]), float.Parse(parts[16]));
            InterpolationType = parts[17].AsEnum<EVectorInterpType>();
        }

        public override void MakeOutLinear()
        {
            var next = Next;
            float span;
            if (next is null)
            {
                if (OwningTrack != null && OwningTrack.FirstKey != this)
                {
                    next = (VectorKeyframe<Vector4>)OwningTrack.FirstKey;
                    span = OwningTrack.LengthInSeconds - Second + next.Second;
                }
                else
                    return;
            }
            else
                span = next.Second - Second;
            OutTangent = (next.InValue - OutValue) / span;
        }

        public override void MakeInLinear()
        {
            var prev = Prev;
            float span;
            if (prev is null)
            {
                if (OwningTrack != null && OwningTrack.LastKey != this)
                {
                    prev = (VectorKeyframe<Vector4>)OwningTrack.LastKey;
                    span = OwningTrack.LengthInSeconds - prev.Second + Second;
                }
                else
                    return;
            }
            else
                span = Second - prev.Second;
            InTangent = (InValue - prev.OutValue) / span;
        }

        public override void UnifyTangents(EUnifyBias bias)
        {
            switch (bias)
            {
                case EUnifyBias.Average:
                    InTangent = -(OutTangent = (-InTangent + OutTangent) * 0.5f);
                    break;
                case EUnifyBias.In:
                    OutTangent = -InTangent;
                    break;
                case EUnifyBias.Out:
                    InTangent = -OutTangent;
                    break;
            }
        }

        public override void UnifyTangentDirections(EUnifyBias bias)
        {
            switch (bias)
            {
                case EUnifyBias.Average:
                    {
                        float inLength = InTangent.Length();
                        float outLength = OutTangent.Length();
                        Vector4 inTan = InTangent.Normalized();
                        Vector4 outTan = OutTangent.Normalized();
                        Vector4 avg = (-inTan + outTan) * 0.5f;
                        avg.Normalized();
                        InTangent = -avg * inLength;
                        OutTangent = avg * outLength;
                    }
                    break;
                case EUnifyBias.In:
                    {
                        float outLength = OutTangent.Length();
                        Vector4 inTan = InTangent.Normalized();
                        OutTangent = -inTan * outLength;
                    }
                    break;
                case EUnifyBias.Out:
                    {
                        float inLength = InTangent.Length();
                        Vector4 outTan = OutTangent.Normalized();
                        InTangent = -outTan * inLength;
                    }
                    break;
            }
        }

        public override void UnifyTangentMagnitudes(EUnifyBias bias)
        {
            switch (bias)
            {
                case EUnifyBias.Average:
                    {
                        float inLength = InTangent.Length();
                        float outLength = OutTangent.Length();
                        float avgLength = (inLength + outLength) * 0.5f;
                        Vector4 inTan = InTangent.Normalized();
                        Vector4 outTan = OutTangent.Normalized();
                        InTangent = inTan * avgLength;
                        OutTangent = outTan * avgLength;
                    }
                    break;
                case EUnifyBias.In:
                    {
                        float inLength = InTangent.Length();
                        Vector4 outTan = OutTangent.Normalized();
                        OutTangent = -outTan * inLength;
                        break;
                    }
                case EUnifyBias.Out:
                    {
                        float outLength = OutTangent.Length();
                        Vector4 inTan = InTangent.Normalized();
                        InTangent = -inTan * outLength;
                        break;
                    }
            }
        }

        public override void UnifyValues(EUnifyBias bias)
        {
            switch (bias)
            {
                case EUnifyBias.Average:
                    InValue = OutValue = (InValue + OutValue) * 0.5f;
                    break;
                case EUnifyBias.In:
                    OutValue = InValue;
                    break;
                case EUnifyBias.Out:
                    InValue = OutValue;
                    break;
            }
        }
    }
}
