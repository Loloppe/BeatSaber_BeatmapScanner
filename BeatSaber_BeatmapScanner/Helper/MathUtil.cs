using System;
using System.Collections.Generic;
using UnityEngine;
using static BeatmapScanner.Algorithm.BeatmapScanner;

namespace BeatmapScanner.Algorithm
{
    internal class MathUtil
    {
        // https://github.com/shamim-akhtar/bezier-curve
        public static List<Vector2> PointList3(List<Vector2> controlPoints, float interval = 0.01f)
        {
            int N = controlPoints.Count - 1;
            if (N > 16)
            {
                Debug.Log("You have used more than 16 control points.");
                Debug.Log("The maximum control points allowed is 16.");
                controlPoints.RemoveRange(16, controlPoints.Count - 16);
            }

            List<Vector2> points = new();
            for (float t = 0.0f; t <= 1.0f + interval - 0.0001f; t += interval)
            {
                Vector2 p = new();
                for (int i = 0; i < controlPoints.Count; ++i)
                {
                    Vector2 bn = MathUtil.Bernstein(N, i, t) * controlPoints[i];
                    p += bn;
                }
                points.Add(p);
            }

            return points;
        }

        public static float ReduceWithExponentialCurve(float currentValue, float lowerBound, float upperBound, float curve)
        {
            float mappedValue = (currentValue - lowerBound) / (upperBound - lowerBound);
            return lowerBound + (upperBound - lowerBound) * (float)Math.Pow(mappedValue, curve);
        }

        public static float NormalizeVariable(float variable)
        {
            float OldMax = MaxNerfMS;
            float OldMin = MinNerfMS;
            float NewMax = -NormalizedMax;
            float NewMin = NormalizedMin;
            float OldRange = (OldMax - OldMin);
            float NewRange = (NewMax - NewMin);
            float NewValue = (((variable - OldMin) * NewRange) / OldRange) + NewMin;
            return NewValue;
        }

        public static float NormalizeVariable2(float variable)
        {
            float OldMax = MaxNote;
            float OldMin = MinNote;
            float NewMax = -NormalizedMax;
            float NewMin = NormalizedMin;
            float OldRange = (OldMax - OldMin);
            float NewRange = (NewMax - NewMin);
            float NewValue = (((variable - OldMin) * NewRange) / OldRange) + NewMin;
            return NewValue;
        }

        public static int ConvertBeatToMS(float beat, float bpm)
        {
            return (int)Math.Round(beat / bpm * 60 * 1000);
        }

        public static float ConvertDegreesToRadians(float degrees)
        {
            float radians = (float)(Math.PI / 180f) * degrees;
            return (radians);
        }

        public static float ConvertRadiansToDegrees(float radians)
        {
            float degrees = (float)(180f / Math.PI) * radians;
            return (degrees);
        }

        public static float Bernstein(int n, int i, float t)
        {
            float t_i = Mathf.Pow(t, i);
            float t_n_minus_i = Mathf.Pow((1 - t), (n - i));

            float basis = Binomial(n, i) * t_i * t_n_minus_i;
            return basis;
        }

        public static readonly float[] Factorial = new float[]
        {
                1.0f,
                1.0f,
                2.0f,
                6.0f,
                24.0f,
                120.0f,
                720.0f,
                5040.0f,
                40320.0f,
                362880.0f,
                3628800.0f,
                39916800.0f,
                479001600.0f,
                6227020800.0f,
                87178291200.0f,
                1307674368000.0f,
                20922789888000.0f,
        };

        public static float Binomial(int n, int i)
        {
            float ni;
            float a1 = Factorial[n];
            float a2 = Factorial[i];
            float a3 = Factorial[n - i];
            ni = a1 / (a2 * a3);
            return ni;
        }

        public static (Vector2, Vector2) CalculateBaseEntryExit((int line, int layer) position, float angle)
        {
            Vector2 entry = new((float)(position.line * 0.333333f - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667f + 0.166667f), (float)(position.layer * 0.333333f - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667f + 0.16667f));
            Vector2 exit = new((float)(position.line * 0.333333f + Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667f + 0.166667f), (float)(position.layer * 0.333333f + Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667f + 0.16667f));

            return (entry, exit);
        }

        public static float BerzierAngleStrainCalc(List<float> angle, bool forehand, int type)
        {
            var strainAmount = 0f;

            for (int i = 0; i < angle.Count; i++)
            {
                if (forehand)
                {
                    if (type == 1)
                    {
                        strainAmount += 2f * (float)Math.Pow((180f - Math.Abs(Math.Abs(247.5f - angle[i]) - 180f)) / 180f, 2);
                    }
                    else
                    {
                        strainAmount += 2f * (float)Math.Pow((180f - Math.Abs(Math.Abs(292.5f - angle[i]) - 180f)) / 180f, 2);
                    }
                }
                else
                {
                    if (type == 1)
                    {
                        strainAmount += 2f * (float)Math.Pow((180f - Math.Abs(Math.Abs(247.5f - 180f - angle[i]) - 180f)) / 180f, 2);
                    }
                    else
                    {
                        strainAmount += 2f * (float)Math.Pow((180f - Math.Abs(Math.Abs(292.5f - 180f - angle[i]) - 180f)) / 180f, 2);
                    }
                }
            }

            return strainAmount;
        }
    }
}
