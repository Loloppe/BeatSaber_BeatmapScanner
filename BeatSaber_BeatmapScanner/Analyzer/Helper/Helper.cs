using Analyzer.BeatmapScanner.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.BeatmapScanner.Helper
{
    internal class Helper
    {
        public static int[] DirectionToDegree = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };

        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = degrees * (Math.PI / 180f);
            return radians;
        }

        public static double ConvertRadiansToDegrees(double radians)
        {
            double degrees = radians * (180f / Math.PI);
            return degrees;
        }

        public static double Mod(double x, double m)
        {
            return (x % m + m) % m;
        }

        public static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static double ReverseCutDirection(double direction)
        {
            if (direction >= 180)
            {
                return direction - 180;
            }
            else
            {
                return direction + 180;
            }
        }

        public static string DegreeToName(double direction)
        {
            return direction switch
            {
                double d when d > 67.5 && d <= 112.5 => "UP",
                double d when d > 247.5 && d <= 292.5 => "DOWN",
                double d when d > 157.5 && d <= 202.5 => "LEFT",
                double d when d <= 22.5 && d >= 0 || d > 337.5 && d < 360 => "RIGHT",
                double d when d > 112.5 && d <= 157.5 => "UP-LEFT",
                double d when d > 22.5 && d <= 67.5 => "UP-RIGHT",
                double d when d > 202.5 && d <= 247.5 => "DOWN-LEFT",
                double d when d > 292.5 && d <= 337.5 => "DOWN-RIGHT",
                _ => "ERROR",
            };
        }

        public static SwingData Closest(List<SwingData> lst, double K)
        {
            return lst.OrderBy(item => Math.Abs(item.Time - K)).First();
        }
    }
}
