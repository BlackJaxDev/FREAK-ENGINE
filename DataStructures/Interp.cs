using Extensions;
using System.Drawing;
using XREngine.Data.Transforms.Rotations;
using XREngine.Data.Transforms.Vectors;
using static System.Math;

namespace XREngine.Data
{
    /// <summary>
    /// Provides tools pertaining to interpolation.
    /// </summary>
    public static class Interp
    {
        #region Polynomials
        public static float EvaluatePolynomial(float third, float second, float first, float zero, float x)
        {
            float x2 = x * x;
            return third * x2 * x + second * x2 + first * x + zero;
        }
        public static Vec2 EvaluatePolynomial(Vec2 third, Vec2 second, Vec2 first, Vec2 zero, Vec2 x)
        {
            Vec2 x2 = x * x;
            return third * x2 * x + second * x2 + first * x + zero;
        }
        public static Vec3 EvaluatePolynomial(Vec3 third, Vec3 second, Vec3 first, Vec3 zero, Vec3 x)
        {
            Vec3 x2 = x * x;
            return third * x2 * x + second * x2 + first * x + zero;
        }
        public static Vec4 EvaluatePolynomial(Vec4 third, Vec4 second, Vec4 first, Vec4 zero, Vec4 x)
        {
            Vec4 x2 = x * x;
            return third * x2 * x + second * x2 + first * x + zero;
        }
        public static float EvaluatePolynomial(float second, float first, float zero, float x)
        {
            float x2 = x * x;
            return second * x2 + first * x + zero;
        }
        public static Vec2 EvaluatePolynomial(Vec2 second, Vec2 first, Vec2 zero, Vec2 x)
        {
            Vec2 x2 = x * x;
            return second * x2 + first * x + zero;
        }
        public static Vec3 EvaluatePolynomial(Vec3 second, Vec3 first, Vec3 zero, Vec3 x)
        {
            Vec3 x2 = x * x;
            return second * x2 + first * x + zero;
        }
        public static Vec4 EvaluatePolynomial(Vec4 second, Vec4 first, Vec4 zero, Vec4 x)
        {
            Vec4 x2 = x * x;
            return second * x2 + first * x + zero;
        }
        public static float EvaluatePolynomial(float first, float zero, float x)
        {
            return first * x + zero;
        }
        public static Vec2 EvaluatePolynomial(Vec2 first, Vec2 zero, Vec2 x)
        {
            return first * x + zero;
        }
        public static Vec3 EvaluatePolynomial(Vec3 first, Vec3 zero, Vec3 x)
        {
            return first * x + zero;
        }
        public static Vec4 EvaluatePolynomial(Vec4 first, Vec4 zero, Vec4 x)
        {
            return first * x + zero;
        }
        #endregion

        #region Bezier

        #region Point Approximation
        public static Vec2[] GetBezierPoints(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, int pointCount, out float length)
        {
            Vec2[] points = new Vec2[pointCount];

            // var q is the change in t between successive evaluations.
            float q = 1.0f / (pointCount - 1); // q is dependent on the number of GAPS = POINTS-1

            // coefficients of the cubic polynomial that we're FDing -
            Vec2 a = p0;
            Vec2 b = 3 * (p1 - p0);
            Vec2 c = 3 * (p2 - 2 * p1 + p0);
            Vec2 d = p3 - 3 * p2 + 3 * p1 - p0;

            // initial values of the poly and the 3 diffs -
            Vec2 s = a;                                     // the poly value
            Vec2 u = b * q + c * q * q + d * q * q * q;     // 1st order diff (quadratic)
            Vec2 v = 2 * c * q * q + 6 * d * q * q * q;     // 2nd order diff (linear)
            Vec2 w = 6 * d * q * q * q;                     // 3rd order diff (constant)

            length = 0.0f;

            Vec2 OldPos = p0;
            points[0] = p0;

            for (int i = 1; i < pointCount; ++i)
            {
                s += u;
                u += v;
                v += w;

                length += s.DistanceTo(OldPos);
                OldPos = s;

                points[i] = s;
            }
            return points;
        }
        public static Vec2[] GetBezierPoints(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, int pointCount)
        {
            if (pointCount < 2)
                throw new InvalidOperationException();

            Vec2[] points = new Vec2[pointCount];
            float timeDelta = 1.0f / (pointCount - 1);
            for (int i = 0; i < pointCount; ++i)
                points[i] = CubicBezier(p0, p1, p2, p3, timeDelta * i);

            return points;
        }
        #endregion

        #region Coefficients

        #region Position
        public static void CubicBezierCoefs(
            float p0, float t0, float t1, float p1,
            out float third, out float second, out float first, out float zero)
        {
            third = -p0 + 3.0f * t0 - 3.0f * t1 + p1;
            second = 3.0f * p0 - 6.0f * t0 + 3.0f * t1;
            first = -3.0f * p0 + 3.0f * t0;
            zero = p0;
        }
        public static void CubicBezierCoefs(
            Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1,
            out Vec2 third, out Vec2 second, out Vec2 first, out Vec2 zero)
        {
            third = -p0 + 3.0f * t0 - 3.0f * t1 + p1;
            second = 3.0f * p0 - 6.0f * t0 + 3.0f * t1;
            first = -3.0f * p0 + 3.0f * t0;
            zero = p0;
        }
        public static void CubicBezierCoefs(
            Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1,
            out Vec3 third, out Vec3 second, out Vec3 first, out Vec3 zero)
        {
            third = -p0 + 3.0f * t0 - 3.0f * t1 + p1;
            second = 3.0f * p0 - 6.0f * t0 + 3.0f * t1;
            first = -3.0f * p0 + 3.0f * t0;
            zero = p0;
        }
        public static void CubicBezierCoefs(
            Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1,
            out Vec4 third, out Vec4 second, out Vec4 first, out Vec4 zero)
        {
            third = -p0 + 3.0f * t0 - 3.0f * t1 + p1;
            second = 3.0f * p0 - 6.0f * t0 + 3.0f * t1;
            first = -3.0f * p0 + 3.0f * t0;
            zero = p0;
        }
        #endregion

        #region Velocity
        public static void CubicBezierVelocityCoefs(
            float p0, float t0, float t1, float p1,
            out float second, out float first, out float zero)
        {
            second = 3.0f * (-p0 + 3.0f * t0 - 3.0f * t1 + p1);
            first = 2.0f * (3.0f * p0 - 6.0f * t0 + 3.0f * t1);
            zero = -3.0f * p0 + 3.0f * t0;
        }
        public static void CubicBezierVelocityCoefs(
            Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1,
            out Vec2 second, out Vec2 first, out Vec2 zero)
        {
            second = 3.0f * (-p0 + 3.0f * t0 - 3.0f * t1 + p1);
            first = 2.0f * (3.0f * p0 - 6.0f * t0 + 3.0f * t1);
            zero = -3.0f * p0 + 3.0f * t0;
        }
        public static void CubicBezierVelocityCoefs(
            Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1,
            out Vec3 second, out Vec3 first, out Vec3 zero)
        {
            second = 3.0f * (-p0 + 3.0f * t0 - 3.0f * t1 + p1);
            first = 2.0f * (3.0f * p0 - 6.0f * t0 + 3.0f * t1);
            zero = -3.0f * p0 + 3.0f * t0;
        }
        public static void CubicBezierVelocityCoefs(
            Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1,
            out Vec4 second, out Vec4 first, out Vec4 zero)
        {
            second = 3.0f * (-p0 + 3.0f * t0 - 3.0f * t1 + p1);
            first = 2.0f * (3.0f * p0 - 6.0f * t0 + 3.0f * t1);
            zero = -3.0f * p0 + 3.0f * t0;
        }
        #endregion

        #region Acceleration
        public static void CubicBezierAccelerationCoefs(
           float p0, float t0, float t1, float p1,
           out float first, out float zero)
        {
            first = 6.0f * (-p0 + 3.0f * t0 - 3.0f * t1 + p1);
            zero = 2.0f * (3.0f * p0 - 6.0f * t0 + 3.0f * t1);
        }
        public static void CubicBezierAccelerationCoefs(
           Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1,
           out Vec2 first, out Vec2 zero)
        {
            first = 6.0f * (-p0 + 3.0f * t0 - 3.0f * t1 + p1);
            zero = 2.0f * (3.0f * p0 - 6.0f * t0 + 3.0f * t1);
        }
        public static void CubicBezierAccelerationCoefs(
           Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1,
           out Vec3 first, out Vec3 zero)
        {
            first = 6.0f * (-p0 + 3.0f * t0 - 3.0f * t1 + p1);
            zero = 2.0f * (3.0f * p0 - 6.0f * t0 + 3.0f * t1);
        }
        public static void CubicBezierAccelerationCoefs(
           Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1,
           out Vec4 first, out Vec4 zero)
        {
            first = 6.0f * (-p0 + 3.0f * t0 - 3.0f * t1 + p1);
            zero = 2.0f * (3.0f * p0 - 6.0f * t0 + 3.0f * t1);
        }
        #endregion

        #endregion

        #region Position
        public static float CubicBezier(float p0, float t0, float t1, float p1, float time)
        {
            CubicBezierCoefs(p0, t0, t1, p1, out float third, out float second, out float first, out float zero);
            return EvaluatePolynomial(third, second, first, zero, time);
        }
        public static Vec2 CubicBezier(Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1, float time)
        {
            CubicBezierCoefs(p0, t0, t1, p1, out Vec2 third, out Vec2 second, out Vec2 first, out Vec2 zero);
            return EvaluatePolynomial(third, second, first, zero, time);
        }
        public static Vec3 CubicBezier(Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1, float time)
        {
            CubicBezierCoefs(p0, t0, t1, p1, out Vec3 third, out Vec3 second, out Vec3 first, out Vec3 zero);
            return EvaluatePolynomial(third, second, first, zero, time);
        }
        public static Vec4 CubicBezier(Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1, float time)
        {
            CubicBezierCoefs(p0, t0, t1, p1, out Vec4 third, out Vec4 second, out Vec4 first, out Vec4 zero);
            return EvaluatePolynomial(third, second, first, zero, new Vec4(time));
        }
        #endregion

        #region Velocity
        public static float CubicBezierVelocity(float p0, float t0, float t1, float p1, float time)
        {
            CubicBezierVelocityCoefs(p0, t0, t1, p1, out float second, out float first, out float zero);
            return EvaluatePolynomial(second, first, zero, time);
        }
        public static Vec2 CubicBezierVelocity(Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1, float time)
        {
            CubicBezierVelocityCoefs(p0, t0, t1, p1, out Vec2 second, out Vec2 first, out Vec2 zero);
            return EvaluatePolynomial(second, first, zero, time);
        }
        public static Vec3 CubicBezierVelocity(Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1, float time)
        {
            CubicBezierVelocityCoefs(p0, t0, t1, p1, out Vec3 second, out Vec3 first, out Vec3 zero);
            return EvaluatePolynomial(second, first, zero, time);
        }
        public static Vec4 CubicBezierVelocity(Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1, float time)
        {
            CubicBezierVelocityCoefs(p0, t0, t1, p1, out Vec4 second, out Vec4 first, out Vec4 zero);
            return EvaluatePolynomial(second, first, zero, time);
        }
        #endregion

        #region Acceleration
        public static float CubicBezierAcceleration(float p0, float t0, float t1, float p1, float time)
        {
            CubicBezierAccelerationCoefs(p0, t0, t1, p1, out float first, out float zero);
            return EvaluatePolynomial(first, zero, time);
        }
        public static Vec2 CubicBezierAcceleration(Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1, float time)
        {
            CubicBezierAccelerationCoefs(p0, t0, t1, p1, out Vec2 first, out Vec2 zero);
            return EvaluatePolynomial(first, zero, time);
        }
        public static Vec3 CubicBezierAcceleration(Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1, float time)
        {
            CubicBezierAccelerationCoefs(p0, t0, t1, p1, out Vec3 first, out Vec3 zero);
            return EvaluatePolynomial(first, zero, time);
        }
        public static Vec4 CubicBezierAcceleration(Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1, float time)
        {
            CubicBezierAccelerationCoefs(p0, t0, t1, p1, out Vec4 first, out Vec4 zero);
            return EvaluatePolynomial(first, zero, time);
        }
        #endregion

        #endregion

        #region Hermite

        #region Coefficients

        #region Position
        public static void CubicHermiteCoefs(
            float p0, float t0, float t1, float p1,
            out float third, out float second, out float first, out float zero)
        {
            third = (2.0f * p0 + t0 - 2.0f * p1 + t1);
            second = (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
            first = t0;
            zero = p0;
        }
        public static void CubicHermiteCoefs(
            Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1,
            out Vec2 third, out Vec2 second, out Vec2 first, out Vec2 zero)
        {
            third = (2.0f * p0 + t0 - 2.0f * p1 + t1);
            second = (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
            first = t0;
            zero = p0;
        }
        public static void CubicHermiteCoefs(
            Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1,
            out Vec3 third, out Vec3 second, out Vec3 first, out Vec3 zero)
        {
            third = (2.0f * p0 + t0 - 2.0f * p1 + t1);
            second = (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
            first = t0;
            zero = p0;
        }
        public static void CubicHermiteCoefs(
            Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1,
            out Vec4 third, out Vec4 second, out Vec4 first, out Vec4 zero)
        {
            third = (2.0f * p0 + t0 - 2.0f * p1 + t1);
            second = (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
            first = t0;
            zero = p0;
        }
        #endregion

        #region Velocity
        public static void CubicHermiteVelocityCoefs(
           float p0, float t0, float t1, float p1,
           out float second, out float first, out float zero)
        {
            second = 3.0f * (2.0f * p0 + t0 - 2.0f * p1 + t1);
            first = 2.0f * (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
            zero = t0;
        }
        public static void CubicHermiteVelocityCoefs(
           Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1,
           out Vec2 second, out Vec2 first, out Vec2 zero)
        {
            second = 3.0f * (2.0f * p0 + t0 - 2.0f * p1 + t1);
            first = 2.0f * (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
            zero = t0;
        }
        public static void CubicHermiteVelocityCoefs(
           Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1,
           out Vec3 second, out Vec3 first, out Vec3 zero)
        {
            second = 3.0f * (2.0f * p0 + t0 - 2.0f * p1 + t1);
            first = 2.0f * (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
            zero = t0;
        }
        public static void CubicHermiteVelocityCoefs(
           Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1,
           out Vec4 second, out Vec4 first, out Vec4 zero)
        {
            second = 3.0f * (2.0f * p0 + t0 - 2.0f * p1 + t1);
            first = 2.0f * (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
            zero = t0;
        }
        #endregion

        #region Acceleration
        public static void CubicHermiteAccelerationCoefs(
           float p0, float t0, float t1, float p1,
           out float first, out float zero)
        {
            first = 6.0f * (2.0f * p0 + t0 - 2.0f * p1 + t1);
            zero = 2.0f * (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
        }
        public static void CubicHermiteAccelerationCoefs(
           Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1,
           out Vec2 first, out Vec2 zero)
        {
            first = 6.0f * (2.0f * p0 + t0 - 2.0f * p1 + t1);
            zero = 2.0f * (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
        }
        public static void CubicHermiteAccelerationCoefs(
           Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1,
           out Vec3 first, out Vec3 zero)
        {
            first = 6.0f * (2.0f * p0 + t0 - 2.0f * p1 + t1);
            zero = 2.0f * (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
        }
        public static void CubicHermiteAccelerationCoefs(
           Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1,
           out Vec4 first, out Vec4 zero)
        {
            first = 6.0f * (2.0f * p0 + t0 - 2.0f * p1 + t1);
            zero = 2.0f * (-3.0f * p0 - 2.0f * t0 + 3.0f * p1 - t1);
        }
        #endregion

        #endregion

        #region Position
        public static float CubicHermite(float p0, float t0, float t1, float p1, float time)
        {
            CubicHermiteCoefs(p0, t0, t1, p1, out float third, out float second, out float first, out float zero);
            return EvaluatePolynomial(third, second, first, zero, time);
        }
        public static Vec2 CubicHermite(Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1, float time)
        {
            CubicHermiteCoefs(p0, t0, t1, p1, out Vec2 third, out Vec2 second, out Vec2 first, out Vec2 zero);
            return EvaluatePolynomial(third, second, first, zero, time);
        }
        public static Vec3 CubicHermite(Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1, float time)
        {
            CubicHermiteCoefs(p0, t0, t1, p1, out Vec3 third, out Vec3 second, out Vec3 first, out Vec3 zero);
            return EvaluatePolynomial(third, second, first, zero, time);
        }
        public static Vec4 CubicHermite(Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1, float time)
        {
            CubicHermiteCoefs(p0, t0, t1, p1, out Vec4 third, out Vec4 second, out Vec4 first, out Vec4 zero);
            return EvaluatePolynomial(third, second, first, zero, time);
        }
        #endregion

        #region Velocity
        public static float CubicHermiteVelocity(float p0, float t0, float t1, float p1, float time)
        {
            CubicHermiteVelocityCoefs(p0, t0, t1, p1, out float second, out float first, out float zero);
            return EvaluatePolynomial(second, first, zero, time);
        }
        public static Vec2 CubicHermiteVelocity(Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1, float time)
        {
            CubicHermiteVelocityCoefs(p0, t0, t1, p1, out Vec2 second, out Vec2 first, out Vec2 zero);
            return EvaluatePolynomial(second, first, zero, time);
        }
        public static Vec3 CubicHermiteVelocity(Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1, float time)
        {
            CubicHermiteVelocityCoefs(p0, t0, t1, p1, out Vec3 second, out Vec3 first, out Vec3 zero);
            return EvaluatePolynomial(second, first, zero, time);
        }
        public static Vec4 CubicHermiteVelocity(Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1, float time)
        {
            CubicHermiteVelocityCoefs(p0, t0, t1, p1, out Vec4 second, out Vec4 first, out Vec4 zero);
            return EvaluatePolynomial(second, first, zero, time);
        }
        #endregion

        #region Acceleration
        public static float CubicHermiteAcceleration(float p0, float t0, float t1, float p1, float time)
        {
            CubicHermiteAccelerationCoefs(p0, t0, t1, p1, out float first, out float zero);
            return EvaluatePolynomial(first, zero, time);
        }
        public static Vec2 CubicHermiteAcceleration(Vec2 p0, Vec2 t0, Vec2 t1, Vec2 p1, float time)
        {
            CubicHermiteAccelerationCoefs(p0, t0, t1, p1, out Vec2 first, out Vec2 zero);
            return EvaluatePolynomial(first, zero, time);
        }
        public static Vec3 CubicHermiteAcceleration(Vec3 p0, Vec3 t0, Vec3 t1, Vec3 p1, float time)
        {
            CubicHermiteAccelerationCoefs(p0, t0, t1, p1, out Vec3 first, out Vec3 zero);
            return EvaluatePolynomial(first, zero, time);
        }
        public static Vec4 CubicHermiteAcceleration(Vec4 p0, Vec4 t0, Vec4 t1, Vec4 p1, float time)
        {
            CubicHermiteAccelerationCoefs(p0, t0, t1, p1, out Vec4 first, out Vec4 zero);
            return EvaluatePolynomial(first, zero, time);
        }
        #endregion

        #endregion

        #region Lerp

        public static float Lerp(float start, float end, float time)
            => start + (end - start) * time;
        public static float Lerp(float start, float end, float time, float speed)
            => Lerp(start, end, time * speed);

        public static Vec2 Lerp(Vec2 start, Vec2 end, float time)
            => start + (end - start) * time;
        public static Vec2 Lerp(Vec2 start, Vec2 end, float time, float speed)
            => Lerp(start, end, time * speed);

        public static Vec3 Lerp(Vec3 start, Vec3 end, float time)
            => start + (end - start) * time;
        public static Vec3 Lerp(Vec3 start, Vec3 end, float time, float speed)
            => Lerp(start, end, time * speed);

        public static Vec4 Lerp(Vec4 start, Vec4 end, float time)
            => start + (end - start) * time;
        public static Vec4 Lerp(Vec4 start, Vec4 end, float time, float speed)
            => Lerp(start, end, time * speed);

        public static ColorF3 Lerp(ColorF3 startValue, ColorF3 endValue, float time)
            => startValue + (endValue - startValue) * time;
        public static ColorF3 Lerp(ColorF3 startValue, ColorF3 endValue, float time, float speed)
            => startValue + (endValue - startValue) * time * speed;

        public static ColorF4 Lerp(ColorF4 startValue, ColorF4 endValue, float time)
            => startValue + (endValue - startValue) * time;
        public static ColorF4 Lerp(ColorF4 startValue, ColorF4 endValue, float time, float speed)
            => startValue + (endValue - startValue) * time * speed;

        public static Quat Slerp(Quat q1, Quat q2, float blend)
        {
            // if either input is zero, return the other.
            if (q1.LengthSquared == 0.0f)
            {
                if (q2.LengthSquared == 0.0f)
                    return Quat.Identity;

                return q2;
            }
            else if (q2.LengthSquared == 0.0f)
                return q1;

            float cosHalfAngle = q1.W * q2.W + q1.Xyz.Dot(q2.Xyz);
            if (cosHalfAngle >= 1.0f || cosHalfAngle <= -1.0f)
            {
                // angle = 0.0f, so just return one input.
                return q1;
            }
            else if (cosHalfAngle < 0.0f)
            {
                q2.Xyz = -q2.Xyz;
                q2.W = -q2.W;
                cosHalfAngle = -cosHalfAngle;
            }

            float blendA;
            float blendB;
            if (cosHalfAngle < 0.99f)
            {
                // do proper slerp for big angles
                float halfAngle = (float)Acos(cosHalfAngle);
                float sinHalfAngle = (float)Sin(halfAngle);
                float oneOverSinHalfAngle = 1.0f / sinHalfAngle;
                blendA = (float)Sin(halfAngle * (1.0f - blend)) * oneOverSinHalfAngle;
                blendB = (float)Sin(halfAngle * blend) * oneOverSinHalfAngle;
            }
            else
            {
                // do lerp if angle is really small.
                blendA = 1.0f - blend;
                blendB = blend;
            }

            Quat result = new Quat(
                blendA * q1.Xyz + blendB * q2.Xyz,
                blendA * q1.W + blendB * q2.W);

            return result.LengthSquared > 0.0f ? result.Normalized() : Quat.Identity;
        }

        #endregion

        #region Time Modifiers

        /// <summary>
        /// Maps a linear time value from 0.0f to 1.0f to a time value that bounces back a specified amount of times after hitting 1.0f.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="bounces"></param>
        /// <param name="bounceFalloff"></param>
        /// <returns></returns>
        public static float BounceTimeModifier(float time, int bounces, double bounceFalloff)
            => 1.0f - (float)(Pow(E, -bounceFalloff * time) * Abs(Cos(PI * (0.5 + bounces) * time)));
        /// <summary>
        /// Maps a linear time value from 0.0f to 1.0f to a cosine time value that eases in and out.
        /// </summary>
        public static float CosineTimeModifier(float time)
            => (1.0f - (float)Cos(time * PIf)) * 0.5f;
        public static float TimeModifier(float time, EAnimBlendType type)
        {
            //TODO
            return time;
        }

        #endregion

        #region Special Interps

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="time"></param>
        /// <param name="speed"></param>
        /// <param name="exponent"></param>
        /// <returns></returns>
        public static float QuadraticEaseEnd(float start, float end, float time, float speed = 1.0f, float exponent = 2.0f)
            => Lerp(start, end, 1.0f - (float)Pow(1.0f - (time * speed), exponent));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="time"></param>
        /// <param name="speed"></param>
        /// <param name="exponent"></param>
        /// <returns></returns>
        public static float QuadraticEaseStart(float start, float end, float time, float speed = 1.0f, float exponent = 2.0f)
            => Lerp(start, end, (float)Pow(time * speed, exponent));
        /// <summary>
        /// Smoothed interpolation between two points. Eases in and out.
        /// </summary>
        public static float Cosine(float start, float end, float time, float speed = 1.0f)
            => Lerp(start, end, CosineTimeModifier(time * speed));
        /// <summary>
        /// Smoothed interpolation between two points. Eases in and out.
        /// </summary>
        public static Vec2 Cosine(Vec2 start, Vec2 end, float time, float speed = 1.0f)
            => Vec2.Lerp(start, end, CosineTimeModifier(time * speed));
        /// <summary>
        /// Smoothed interpolation between two points. Eases in and out.
        /// </summary>
        public static Vec3 CosineTo(Vec3 start, Vec3 end, float time, float speed = 1.0f)
            => Vec3.Lerp(start, end, CosineTimeModifier(time * speed));
        /// <summary>
        /// Smoothed interpolation between two points. Eases in and out.
        /// </summary>
        public static Vec4 CosineTo(Vec4 start, Vec4 end, float time, float speed = 1.0f)
            => Vec4.Lerp(start, end, CosineTimeModifier(time * speed));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="delta"></param>
        /// <param name="degPerSec"></param>
        /// <returns></returns>
        public static Vec3 VInterpNormalRotationTo(Vec3 current, Vec3 target, float delta, float degPerSec)
        {
            Quat deltaQuat = Quat.BetweenVectors(current, target);

            deltaQuat.ToAxisAngleRad(out Vec3 deltaAxis, out float totalRads);

            float deltaRads = DegToRad(degPerSec) * delta;

            if (Abs(totalRads) > deltaRads)
            {
                //totalRads = totalRads.Clamp(-deltaRads, deltaRads);
                deltaQuat = Quat.FromAxisAngleDeg(deltaAxis, deltaRads);
                return deltaQuat * current;
            }

            return target;
        }

        #endregion

        #region Misc Lerps
        public static Color Lerp(
            Color startColor,
            Color endColor,
            float time)
        {
            time = time.Clamp(0.0f, 1.0f);
            Color color = Color.FromArgb(
                (byte)(startColor.A + ((float)endColor.A - startColor.A) * time + 0.5f),
                (byte)(startColor.R + ((float)endColor.R - startColor.R) * time + 0.5f),
                (byte)(startColor.G + ((float)endColor.G - startColor.G) * time + 0.5f),
                (byte)(startColor.B + ((float)endColor.B - startColor.B) * time + 0.5f));
            return color;
        }
        public static Point Lerp(
            Point start,
            Point end,
            float time)
        {
            time = time.Clamp(0.0f, 1.0f);
            return new Point(
                (int)(start.X + ((float)end.X - start.X) * time + 0.5f),
                (int)(start.Y + ((float)end.Y - start.Y) * time + 0.5f));
        }
        public static PointF Lerp(
            PointF start,
            PointF end,
            float time)
        {
            time = time.Clamp(0.0f, 1.0f);
            return new PointF(
                start.X + (end.X - start.X) * time,
                start.Y + (end.Y - start.Y) * time);
        }
        #endregion
    }
}
