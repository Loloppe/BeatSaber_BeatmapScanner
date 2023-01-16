using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatmapScanner.Algorithm.LackWiz
{
    internal class MathWiz
    {
        public static List<(double x, double y)> PointList2(List<(double x, double y)> controlPoints, double interval = 0.01)
        {
            int N = controlPoints.Count() - 1;
            if (N > 16)
            {
                controlPoints.RemoveRange(16, controlPoints.Count - 16);
            }

            List<(double x, double y)> p = new();

            for (double t = 0.0; t <= 1.0 + interval - 0.0001; t += interval)
            {
                (double x, double y) point = new();
                for (int i = 0; i < controlPoints.Count; ++i)
                {
                    double bn = Bernstein(N, i, t);
                    point.x += (bn * controlPoints[i].x);
                    point.y += (bn * controlPoints[i].y);
                }
                p.Add(point);
            }

            return p;
        }

        public static double Bernstein(int n, int i, double t)
        {
            double t_i = Math.Pow(t, i);
            double t_n_minus_i = Math.Pow((1 - t), (n - i));

            double basis = Binomial(n, i) * t_i * t_n_minus_i;
            return basis;
        }

        public static double Binomial(int n, int i)
        {
            double ni;
            double a1 = Factorial[n];
            double a2 = Factorial[i];
            double a3 = Factorial[n - i];
            ni = a1 / (a2 * a3);
            return ni;
        }

        public static readonly double[] Factorial = new double[]
        {
                1.0d,
                1.0d,
                2.0d,
                6.0d,
                24.0d,
                120.0d,
                720.0d,
                5040.0d,
                40320.0d,
                362880.0d,
                3628800.0d,
                39916800.0d,
                479001600.0d,
                6227020800.0d,
                87178291200.0d,
                1307674368000.0d,
                20922789888000.0d,
        };

        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = Math.PI / 180f * degrees;
            return (radians);
        }

        public static double ConvertRadiansToDegrees(double radians)
        {
            double degrees = 180f / Math.PI * radians;
            return (degrees);
        }

        public static ((double, double), (double, double)) CalculateBaseEntryExit((double x, double y) position, double angle)
        {
            (double, double) entry = (position.x * 0.333333 - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667,
                position.y * 0.333333 - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667);

            (double, double) exit = (position.x * 0.333333 + Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667f + 0.166667,
                position.y * 0.333333 + Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667);

            return (entry, exit);
        }

        public static double SwingAngleStrainCalc(List<SwingData> swingData, bool leftOrRight)
        {
            // False or 0 = Left, True or 1 = Right
            var strainAmount = 0d;
            // TODO calculate strain from angle based on left or right hand
            for (int i = 0; i < swingData.Count(); i++)
            {
                if (swingData[i].Forehand)
                {
                    // The Formula firse calculates by first normalizing the angle difference (/180) then using
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                }
                else if (leftOrRight)
                {
                    strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - swingData[i].Angle) - 180)) / 180, 2);
                }
                else
                {
                    strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - swingData[i].Angle) - 180)) / 180, 2);
                }
            }

            return strainAmount * 2;
        }

        public static double BezierAngleStrainCalc(List<double> angleData, bool forehand, bool leftOrRight)
        {
            var strainAmount = 0d;

            for (int i = 0; i < angleData.Count(); i++)
            {
                if (forehand)
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - angleData[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - angleData[i]) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - angleData[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - angleData[i]) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }
    }
}
