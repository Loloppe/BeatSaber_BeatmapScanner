using Analyzer.BeatmapScanner.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.BeatmapScanner.Helper
{
    internal class Curve
    {
        public static double BernsteinPoly(int i, int n, double t)
        {
            return BinomialCoefficient(n, i) * Math.Pow(t, n - i) * Math.Pow(1 - t, i);
        }
        const int nTimes = 25;
        private static readonly double[] tCached = Enumerable.Range(0, nTimes).Select(i => i / (double)(nTimes - 1)).ToArray();
        public static List<Point> BezierCurve(List<Point> points)
        {
            int nPoints = points.Count;

            List<Point> result = new(points.Count);

            for (int i = 0; i < nTimes; i++)
            {
                double currentT = tCached[i];
                double x = 0;
                double y = 0;
                for (int j = 0; j < nPoints; j++)
                {
                    double poly = BernsteinPoly(j, nPoints - 1, currentT);
                    x += points[j].X * poly;
                    y += points[j].Y * poly;
                }
                result.Add(new(x, y));
            }

            return result;
        }

        private static long BinomialCoefficient(int n, int k)
        {
            if (k < 0 || k > n)
            {
                return 0;
            }

            if (k == 0 || k == n)
            {
                return 1;
            }

            long result = 1;
            for (int i = 1; i <= k; i++)
            {
                result = result * (n - i + 1) / i;
            }

            return result;
        }
    }
}
