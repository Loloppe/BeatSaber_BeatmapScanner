using Analyzer.BeatmapScanner.Data;
using System;
using System.Collections.Generic;
using static Analyzer.BeatmapScanner.Helper.Helper;
using static Analyzer.BeatmapScanner.Helper.IsSameDirection;

namespace Analyzer.BeatmapScanner.Helper
{
    internal class FindAngleViaPosition
    {
        public static (double, (double x, double y)) FindAngleViaPos(List<Cube> cubes, int index, int h, double guideAngle, bool pattern)
        {
            (double x, double y) previousPosition;
            (double x, double y) currentPosition = (cubes[index].Line, cubes[index].Layer);

            if (pattern)
            {
                previousPosition = (cubes[h].Line, cubes[h].Layer);
            }
            else
            {
                previousPosition = SimSwingPos(cubes[h].Line, cubes[h].Layer, guideAngle);
            }

            var currentAngle = ReverseCutDirection(Mod(ConvertRadiansToDegrees(Math.Atan2(previousPosition.y - currentPosition.y, previousPosition.x - currentPosition.x)), 360));

            if (pattern && !IsSameDir(currentAngle, guideAngle))
            {
                currentAngle = ReverseCutDirection(currentAngle);
            }
            else if (!pattern && IsSameDir(currentAngle, guideAngle))
            {
                currentAngle = ReverseCutDirection(currentAngle);
            }

            var simPos = SimSwingPos(cubes[index].Line, cubes[index].Layer, currentAngle);

            return (currentAngle, simPos);
        }

        public static (double x, double y) SimSwingPos(double x, double y, double direction, double dis = 5)
        {
            return (x + dis * Math.Cos(ConvertDegreesToRadians(direction)), y + dis * Math.Sin(ConvertDegreesToRadians(direction)));
        }
    }
}
