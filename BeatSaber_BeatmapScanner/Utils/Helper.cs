using Analyzer.BeatmapScanner.Data;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatmapScanner.Utils
{
    internal class Helper
    {
        public static float GetEBPM(List<NoteData> notes, float bpm, float njs, bool leftOrRight)
        {
            var ordered = notes.OrderBy(x => x.time).ToList();
            List<Cube> cubes = [];
            var previous = 0f;
            var effectiveBPM = 10f;
            var peakBPM = 10f;
            var count = 0;
            List<float> timestamps = [];
            var bps = bpm / 60;

            foreach (var note in ordered)
            {
                Cube cube = new()
                {
                    AngleOffset = note.cutDirectionAngleOffset,
                    CutDirection = (int)note.cutDirection,
                    Type = (int)note.colorType,
                    Time = bps * note.time,
                    Line = note.lineIndex,
                    Layer = (int)note.noteLineLayer,
                    Direction = DirectionToDegree[(int)note.cutDirection] + note.cutDirectionAngleOffset
                };
                cubes.Add(cube);
            }

            // Need to find multinotes stuff
            Detect(cubes, bpm, njs, leftOrRight);

            for (int i = 1; i < cubes.Count; i++)
            {
                if (!cubes[i].Head && cubes[i].Pattern)
                {
                    continue;
                }

                var duration = cubes[i].Time - cubes[i - 1].Time;

                if (duration > 0)
                {
                    if (previous >= duration - 0.01 && previous <= duration + 0.01 && duration < effectiveBPM)
                    {
                        count++;
                        if (count >= Settings.Instance.EBPM)
                        {
                            effectiveBPM = duration;
                        }
                    }
                    else
                    {
                        count = 0;
                    }

                    if (duration < peakBPM)
                    {
                        peakBPM = duration;
                    }

                    previous = duration;
                }
            }

            if (effectiveBPM == 10)
            {
                return (0.5f / peakBPM * bpm);
            }

            return (0.5f / effectiveBPM * bpm);
        }

        #region Helper method

        public static double[] DirectionToDegree = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };

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

        public static (double x, double y) SimSwingPos(double x, double y, double direction, double dis = 5)
        {
            return (x + dis * Math.Cos(ConvertDegreesToRadians(direction)), y + dis * Math.Sin(ConvertDegreesToRadians(direction)));
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

        public static double Mod(double x, double m)
        {
            return (x % m + m) % m;
        }

        public static void Detect(List<Cube> cubes, float bpm, float njs, bool leftOrRight)
        {
            if (cubes.Count < 2)
            {
                return;
            }

            double testValue = 45;

            if (leftOrRight)
            {
                testValue = -45;
            }

            // Pre-order the stack/window/tower here if possible
            HandlePattern(cubes);
            (double x, double y) lastSimPos = (0, 0);
            // First note
            if (cubes[0].CutDirection == 8)
            {
                if (cubes[1].CutDirection != 8 && cubes[1].Time - cubes[0].Time <= 0.125)
                {
                    // Second note is an arrow and it's a pattern, so we can determine the first note
                    cubes[0].Direction = Mod(DirectionToDegree[cubes[1].CutDirection] + cubes[1].AngleOffset, 360);
                }
                else
                {
                    // Find the first arrow and reverse direction for each dot note in between
                    var c = cubes.Where(ca => ca.CutDirection != 8).FirstOrDefault();
                    if (c != null)
                    {
                        cubes[0].Direction = DirectionToDegree[c.CutDirection] + c.AngleOffset;
                        for (int i = cubes.IndexOf(c); i > 0; i--)
                        {
                            if (cubes[i].Time - cubes[i - 1].Time > 0.125)
                            {
                                cubes[0].Direction = ReverseCutDirection(cubes[0].Direction);
                            }
                        }
                    }
                    else
                    {
                        // Arrow doesn't exist, we use position instead
                        if (cubes[0].Layer >= 2)
                        {
                            cubes[0].Direction = 90;
                        }
                        else
                        {
                            cubes[0].Direction = 270;
                        }
                    }
                }
            }
            else
            {
                cubes[0].Direction = Mod(DirectionToDegree[cubes[0].CutDirection] + cubes[0].AngleOffset, 360);
            }
            // Second note
            if (cubes[1].CutDirection == 8)
            {
                if ((cubes[1].Time - cubes[0].Time <= 0.125 && IsSlid(cubes[0].Line, cubes[0].Layer, cubes[1].Line, cubes[1].Layer, cubes[0].Direction)) || cubes[1].Time - cubes[0].Time <= 0.0625)
                {
                    // Is a pattern with first note
                    (cubes[1].Direction, lastSimPos) = FindAngleViaPos(cubes, 1, 0, cubes[0].Direction, true);
                    if (cubes[0].CutDirection == 8)
                    {
                        cubes[0].Direction = cubes[1].Direction;
                    }
                    cubes[1].Pattern = true;
                    cubes[0].Pattern = true;
                    cubes[0].Head = true;
                }
                else
                {
                    (cubes[1].Direction, lastSimPos) = FindAngleViaPos(cubes, 1, 0, cubes[0].Direction, false);
                }
            }
            else
            {
                cubes[1].Direction = Mod(DirectionToDegree[cubes[1].CutDirection] + cubes[1].AngleOffset, 360);
                if ((cubes[1].Time - cubes[0].Time <= 0.125 && IsSlid(cubes[0].Line, cubes[0].Layer, cubes[1].Line, cubes[1].Layer, cubes[0].Direction)) || cubes[1].Time - cubes[0].Time <= 0.1429)
                {
                    cubes[0].Head = true;
                    cubes[0].Pattern = true;
                    cubes[1].Pattern = true;
                }
            }
            // Rest of the notes
            for (int i = 2; i < cubes.Count - 1; i++)
            {
                if (cubes[i].CutDirection == 8)
                {
                    if ((SliderCond(cubes[i - 1], cubes[i], lastSimPos, bpm, njs))
                        || cubes[i].Time - cubes[i - 1].Time <= 0.0625)
                    {
                        // Pattern
                        (cubes[i].Direction, lastSimPos) = FindAngleViaPos(cubes, i, i - 1, cubes[i - 1].Direction, true);
                        if (cubes[i - 1].CutDirection == 8)
                        {
                            cubes[i - 1].Direction = cubes[i].Direction;
                        }
                        cubes[i].Pattern = true;
                        if (!cubes[i - 1].Pattern)
                        {
                            cubes[i - 1].Pattern = true;
                            cubes[i - 1].Head = true;
                        }
                        continue;
                    }
                    else // Probably not a pattern
                    {
                        (cubes[i].Direction, lastSimPos) = FindAngleViaPos(cubes, i, i - 1, cubes[i - 1].Direction, false);

                        // Verify if the flow work
                        if (!IsSameDir(cubes[i - 1].Direction, cubes[i].Direction))
                        {
                            if (cubes[i + 1].CutDirection != 8)
                            {
                                // If the next note is an arrow, we want to check that too
                                var nextDir = Mod(DirectionToDegree[cubes[i + 1].CutDirection] + cubes[i + 1].AngleOffset, 360);
                                if (IsSameDir(cubes[i].Direction, nextDir))
                                {
                                    // Attempt a different angle
                                    if (!IsSameDir(cubes[i].Direction + testValue, nextDir))
                                    {
                                        cubes[i].Direction = Mod(cubes[i].Direction + testValue, 360);
                                        continue; // Work
                                    }
                                    else if (!IsSameDir(cubes[i].Direction - testValue, nextDir))
                                    {
                                        cubes[i].Direction = Mod(cubes[i].Direction - testValue, 360);
                                        continue; // Work
                                    }
                                }
                            }
                            continue;
                        }

                        if (!IsSameDir(cubes[i - 1].Direction, cubes[i].Direction + testValue))
                        {
                            cubes[i].Direction = Mod(cubes[i].Direction + testValue, 360);
                            continue; // Work
                        }
                        else if (!IsSameDir(cubes[i - 1].Direction, cubes[i].Direction - testValue))
                        {
                            cubes[i].Direction = Mod(cubes[i].Direction - testValue, 360);
                            continue; // Work
                        }

                        // Maybe the note before is wrong?
                        if (cubes[i - 1].CutDirection == 8 && !IsSameDir(cubes[i - 2].Direction, cubes[i - 1].Direction + testValue))
                        {
                            var lastDir = Mod(cubes[i - 1].Direction + testValue, 360);
                            if (!IsSameDir(lastDir, cubes[i].Direction + testValue * 2))
                            {
                                cubes[i - 1].Direction = Mod(cubes[i - 1].Direction + testValue, 360);
                                cubes[i].Direction = Mod(cubes[i].Direction + testValue * 2, 360);
                                continue; // Work
                            }
                        }
                        if (cubes[i - 1].CutDirection == 8 && !IsSameDir(cubes[i - 2].Direction, cubes[i - 1].Direction - testValue))
                        {
                            var lastDir = Mod(cubes[i - 1].Direction - testValue, 360);
                            if (!IsSameDir(lastDir, cubes[i].Direction - testValue * 2))
                            {
                                cubes[i - 1].Direction = Mod(cubes[i - 1].Direction - testValue, 360);
                                cubes[i].Direction = Mod(cubes[i].Direction - testValue * 2, 360);
                                continue; // Work
                            }
                        }
                    }
                }
                else // Is an arrow
                {
                    cubes[i].Direction = Mod(DirectionToDegree[cubes[i].CutDirection] + cubes[i].AngleOffset, 360);
                    if ((cubes[i].Time - cubes[i - 1].Time <= 0.125 && IsSameDir(cubes[i - 1].Direction, cubes[i].Direction) && SliderCond(cubes[i - 1], cubes[i], lastSimPos, bpm, njs))
                        || cubes[i].Time - cubes[i - 1].Time <= 0.0625)
                    {
                        cubes[i].Pattern = true;
                        if (!cubes[i - 1].Pattern)
                        {
                            cubes[i - 1].Pattern = true;
                            cubes[i - 1].Head = true;
                        }
                    }
                    continue;
                }
            }
            // Fix dot flow that only work from one way
            for (int i = 2; i < cubes.Count - 2; i++)
            {
                if (cubes[i].CutDirection == 8 && !cubes[i].Pattern)
                {
                    if ((IsSameDir(cubes[i].Direction, cubes[i - 1].Direction) && !IsSameDir(cubes[i].Direction, cubes[i + 1].Direction))
                        || (!IsSameDir(cubes[i].Direction, cubes[i - 1].Direction) && IsSameDir(cubes[i].Direction, cubes[i + 1].Direction)))
                    {
                        if (!IsSameDir(cubes[i].Direction + testValue, cubes[i - 1].Direction) && !IsSameDir(cubes[i].Direction + testValue, cubes[i + 1].Direction))
                        {
                            cubes[i].Direction = Mod(cubes[i].Direction + testValue, 360);
                        }
                        else if (!IsSameDir(cubes[i].Direction - testValue, cubes[i - 1].Direction) && !IsSameDir(cubes[i].Direction - testValue, cubes[i + 1].Direction))
                        {
                            cubes[i].Direction = Mod(cubes[i].Direction - testValue, 360);
                        }
                    }
                }
            }
            // Handle the last notes
            if (cubes[cubes.Count - 1].CutDirection == 8)
            {
                if ((cubes[cubes.Count - 1].Time - cubes[cubes.Count - 2].Time <= 0.125 && SliderCond(cubes[cubes.Count - 2], cubes[cubes.Count - 1], lastSimPos, bpm, njs))
                    || cubes[cubes.Count - 1].Time - cubes[cubes.Count - 2].Time <= 0.0625)
                {
                    (cubes[cubes.Count - 1].Direction, lastSimPos) = FindAngleViaPos(cubes, cubes.Count - 1, cubes.Count - 2, cubes[cubes.Count - 2].Direction, true);
                    if (cubes[cubes.Count - 2].CutDirection == 8)
                    {
                        cubes[cubes.Count - 2].Direction = cubes[cubes.Count - 1].Direction;
                    }
                    cubes[cubes.Count - 1].Pattern = true;
                    if (!cubes[cubes.Count - 2].Pattern)
                    {
                        cubes[cubes.Count - 2].Pattern = true;
                        cubes[cubes.Count - 2].Head = true;
                    }
                }
                else
                {
                    (cubes[cubes.Count - 1].Direction, lastSimPos) = FindAngleViaPos(cubes, cubes.Count - 1, cubes.Count - 2, cubes[cubes.Count - 2].Direction, false);
                }
            }
            else
            {
                cubes[cubes.Count - 1].Direction = Mod(DirectionToDegree[cubes[cubes.Count - 1].CutDirection] + cubes[cubes.Count - 1].AngleOffset, 360);
                if (((cubes[cubes.Count - 1].Time - cubes[cubes.Count - 2].Time <= 0.125 && SliderCond(cubes[cubes.Count - 2], cubes[cubes.Count - 1], lastSimPos, bpm, njs))
                    || cubes[cubes.Count - 1].Time - cubes[cubes.Count - 2].Time <= 0.0625) && IsSameDir(cubes[cubes.Count - 2].Direction, cubes[cubes.Count - 1].Direction))
                {
                    cubes[cubes.Count - 1].Pattern = true;
                    if (!cubes[cubes.Count - 2].Pattern)
                    {
                        cubes[cubes.Count - 2].Pattern = true;
                        cubes[cubes.Count - 2].Head = true;
                    }
                }
            }
        }

        public static void HandlePattern(List<Cube> cubes)
        {
            var length = 0;
            for (int n = 0; n < cubes.Count - 2; n++)
            {
                if (length > 0)
                {
                    length--;
                    continue;
                }
                if (cubes[n].Time == cubes[n + 1].Time)
                {
                    // Pattern found
                    length = cubes.Where(c => c.Time == cubes[n].Time).Count() - 1;
                    Cube[] arrow = cubes.Where(c => c.CutDirection != 8 && c.Time == cubes[n].Time).ToArray();
                    double direction = 0;
                    if (arrow.Length == 0)
                    {
                        // Pattern got no arrow
                        var foundArrow = cubes.Where(c => c.CutDirection != 8 && c.Time > cubes[n].Time).ToList();
                        if (foundArrow.Count > 0)
                        {
                            // An arrow note is found after the note
                            direction = ReverseCutDirection(Mod(DirectionToDegree[foundArrow[0].CutDirection] + foundArrow[0].AngleOffset, 360));
                            for (int i = cubes.IndexOf(foundArrow[0]) - 1; i > n; i--)
                            {
                                // Reverse for every dot note in between
                                if (cubes[i + 1].Time - cubes[i].Time >= 0.25)
                                {
                                    direction = ReverseCutDirection(direction);
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // Use the arrow to determine the direction
                        direction = ReverseCutDirection(Mod(DirectionToDegree[arrow[arrow.Length - 1].CutDirection] + arrow[arrow.Length - 1].AngleOffset, 360));
                    }
                    // Simulate a swing to determine the entry point of the pattern
                    (double x, double y) pos;
                    if (n > 0)
                    {
                        pos = SimSwingPos(cubes[n - 1].Line, cubes[n - 1].Layer, direction);
                    }
                    else
                    {
                        pos = SimSwingPos(cubes[0].Line, cubes[0].Layer, direction);
                    }
                    // Calculate the distance of each note based on the new position
                    List<double> distance = new();
                    for (int i = n; i < n + length + 1; i++)
                    {
                        distance.Add(Math.Sqrt(Math.Pow(pos.y - cubes[i].Layer, 2) + Math.Pow(pos.x - cubes[i].Line, 2)));
                    }
                    // Re-order the notes in the proper order
                    for (int i = 0; i < distance.Count; i++)
                    {
                        for (int j = n; j < n + length; j++)
                        {
                            if (distance[j - n + 1] < distance[j - n])
                            {
                                Swap(cubes, j, j + 1);
                                Swap(distance, j - n + 1, j - n);
                            }
                        }
                    }
                }
            }
        }
        public static bool SliderCond(Cube prev, Cube next, (double x, double y) sim, float bpm, float njs)
        {
            if (prev.CutDirection == 8)
            {
                if (next.Time - prev.Time <= 0.125)
                {
                    if (prev.Line == next.Line && prev.Layer == next.Layer && next.CutDirection == 8) return true;
                    if (IsSlid(sim.x, sim.y, next.Line, next.Layer, prev.Direction)) return true;
                }
                if ((next.Time - prev.Time) / (bpm / 60) * njs <= 1 && next.CutDirection == 8) return true;
                return false;
            }

            if (IsSlid(prev.Line, prev.Layer, next.Line, next.Layer, prev.Direction)) return true;
            return false;
        }

        public static bool IsSlid(double x1, double y1, double x2, double y2, double direction)
        {
            switch (direction)
            {
                case double d when d > 67.5 && d <= 112.5:
                    if (y1 < y2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 247.5 && d <= 292.5:
                    if (y1 > y2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 157.5 && d <= 202.5:
                    if (x1 > x2)
                    {
                        return true;
                    }
                    break;
                case double d when d <= 22.5 && d >= 0 || d > 337.5 && d < 360:
                    if (x1 < x2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 112.5 && d <= 157.5:
                    if (y1 < y2 && x1 >= x2)
                    {
                        return true;
                    }
                    if (x1 > x2 && y1 <= y2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 22.5 && d <= 67.5:
                    if (y1 < y2 && x1 <= x2)
                    {
                        return true;
                    }
                    if (x1 < x2 && y1 <= y2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 202.5 && d <= 247.5:
                    if (y1 > y2 && x1 >= x2)
                    {
                        return true;
                    }
                    if (x1 > x2 && y1 >= y2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 292.5 && d <= 337.5:
                    if (y1 > y2 && x1 <= x2)
                    {
                        return true;
                    }
                    if (x1 < x2 && y1 >= y2)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        public static bool IsSameDir(double before, double after, double degree = 67.5)
        {
            before = Mod(before, 360);
            after = Mod(after, 360);

            if (Math.Abs(before - after) <= 180)
            {
                if (Math.Abs(before - after) < degree)
                {
                    return true;
                }
            }
            else
            {
                if (360 - Math.Abs(before - after) < degree)
                {
                    return true;
                }
            }

            return false;
        }

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

        public static int DetectCrouchWalls(List<Wall> walls)
        {
            var crouch = 0;
            bool x1 = false;
            float x1end = 0f;
            bool x2 = false;
            float x2end = 0f;
            bool active = false;

            // Need to determine if there's a crouch wall in the middle 2 row at the same time
            // x <= 1 and x + w - 1 >= 2 | h >= 1 while y >= 2
            // y above 2 is the same as 2
            foreach (var wall in walls)
            {
                if (wall.Beats > x1end) x1 = false;
                if (wall.Beats > x2end) x2 = false;

                if (!x1 && !x2) active = false;

                if (wall.y >= 2 && wall.Height >= 1)
                {
                    if (wall.x <= 1 && wall.x + wall.Width - 1 >= 1)
                    {
                        x1 = true;
                        x1end = wall.Beats + wall.DurationInBeats;
                    }
                    if (wall.x <= 2 && wall.x + wall.Width - 1 >= 2)
                    {
                        x2 = true;
                        x2end = wall.Beats + wall.DurationInBeats;
                    }
                }

                if (x1 && x2 && !active)
                {
                    crouch++;
                    active = true;
                }
            }

            return crouch;
        }

        #endregion
    }
}
