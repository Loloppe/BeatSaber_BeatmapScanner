using System;
using static Analyzer.BeatmapScanner.Helper.Helper;

namespace Analyzer.BeatmapScanner.Helper
{
    internal class CalculateBaseEntryExit
    {
        public static ((double x, double y) entry, (double x, double y) exit) CalcBaseEntryExit((double x, double y) position, double angle)
        {
            (double, double) entry = (position.x * 0.333333 - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667,
                position.y * 0.333333 - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667);

            (double, double) exit = (position.x * 0.333333 + Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667f + 0.166667,
                position.y * 0.333333 + Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667);

            return (entry, exit);
        }
    }
}
