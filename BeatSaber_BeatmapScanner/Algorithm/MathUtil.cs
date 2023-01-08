using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BeatmapScanner.Algorithm.Data;

namespace BeatmapScanner.Algorithm
{
    internal class MathUtil
    {
        public static int ConvertBeat(double beat, float bpm)
        {
            return (int)Math.Round(beat / bpm * 60 * 1000);
        }

        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static double ConvertRadiansToDegrees(double radians)
        {
            double degrees = (180 / Math.PI) * radians;
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

        public static (Vector2, Vector2) CalculateBaseEntryExit((int line, int layer) position, int angle)
        {
            Vector2 entry = new((float)(position.line * 0.333333 - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667), (float)(position.layer * 0.333333 - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667));
            Vector2 exit = new((float)(position.line * 0.333333 + Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667), (float)(position.layer * 0.333333 + Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667));

            return (entry, exit);
        }

        public static double SwingAngleStrainCalc(List<SwingData> data, bool left)
        {
            var strainAmount = 0d;

            for (int i = 0; i < data.Count(); i++)
            {
                if (data[i].Forehand)
                {
                    if (left)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - data[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - data[i].Angle) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (left)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - data[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - data[i].Angle) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }

        public static double BerzierAngleStrainCalc(List<double> angle, bool forehand, bool left)
        {
            var strainAmount = 0d;

            for (int i = 0; i < angle.Count; i++)
            {
                if (forehand)
                {
                    if (left)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - angle[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - angle[i]) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (left)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - angle[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - angle[i]) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }
    }
}
