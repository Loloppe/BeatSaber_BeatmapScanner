using BeatmapScanner.Algorithm.Loloppe;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BeatmapSaveDataVersion3.BeatmapSaveData;
using static BeatmapScanner.Algorithm.BeatmapScanner;

namespace BeatmapScanner.Algorithm
{
    internal class Helper
    {
        public static void FindEntryExit(List<Cube> cubes)
        {
            for (int i = 0; i < cubes.Count(); i++)
            {
                var cube = cubes[i];
                var note = cube.Note;
                cube.Angle = CutDirectionIndex[(int)note.cutDirection];
                var position = (note.line, note.layer);
                (cube.EntryPosition, cube.ExitPosition) = MathUtil.CalculateBaseEntryExit(position, (float)cube.Angle);
            }
        }

        public static void FindSwingCurve(List<Cube> cubes)
        {
            cubes[0].PathAngleStrain = 0;

            for (int i = 1; i < cubes.Count(); i++)
            {
                var start = cubes[i - 1];
                var end = cubes[i];

                if (end.Pattern && !end.Head)
                {
                    continue;
                }

                var point0 = start.ExitPosition; // Start of the curve
                Vector2 point1; // Control point
                point1.x = (float)(point0.x + 0.5 * Math.Cos(MathUtil.ConvertDegreesToRadians(start.Angle)));
                point1.y = (float)(point0.y + 0.5 * Math.Sin(MathUtil.ConvertDegreesToRadians(start.Angle)));
                var point3 = end.EntryPosition; // End of the curve
                Vector2 point2; // Control point
                point2.x = (float)(point3.x - 0.5 * Math.Cos(MathUtil.ConvertDegreesToRadians(end.Angle)));
                point2.y = (float)(point3.y - 0.5 * Math.Sin(MathUtil.ConvertDegreesToRadians(end.Angle)));

                List<Vector2> points = new()
                {
                    point0,
                    point1,
                    point2,
                    point3
                };

                // Calculate a X points curve based on those initial points
                points = MathUtil.PointList3(points, 0.02f);

                points.Reverse();

                List<float> speedList = new();
                List<float> angleList = new();

                for (int j = 1; j < points.Count(); j++)
                {
                    speedList.Add((float)Math.Sqrt(Math.Pow(points[j].y - points[j - 1].y, 2) + Math.Pow((points[j].x - points[j - 1].x), 2)));
                    angleList.Add((MathUtil.ConvertRadiansToDegrees((float)Math.Atan2(points[j].y - points[j - 1].y, points[j].x - points[j - 1].x)) % 360));
                }

                float lookback;
                if (end.Reset)
                {
                    lookback = 0.8f;
                }
                else
                {
                    lookback = 0.333333f;
                }
                int count = (int)(speedList.Count() * lookback);
                end.CurveComplexity = speedList.Count() * speedList.Skip(count).Average() / 10;
                end.PathAngleStrain = MathUtil.BerzierAngleStrainCalc(angleList.Skip(count).ToList(), end.Forehand, (int)end.Note.color) / angleList.Count();
            }
        }

        public static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static void SwapValue(List<Cube> list, int indexA, int indexB)
        {
            (list[indexB].Head, list[indexA].Head) = (list[indexA].Head, list[indexB].Head);
            (list[indexB].Reset, list[indexA].Reset) = (list[indexA].Reset, list[indexB].Reset);
            (list[indexB].SoftReset, list[indexA].SoftReset) = (list[indexA].SoftReset, list[indexB].SoftReset);
        }

        public static bool FindTech(Cube before, Cube after)
        {
            switch (before.Direction) // Wristroll
            {
                case 0:
                    switch (after.Direction)
                    {
                        case 2: return true;
                        case 3: return true;
                    }
                    break;
                case 1:
                    switch (after.Direction)
                    {
                        case 2: return true;
                        case 3: return true;
                    }
                    break;
                case 2:
                    switch (after.Direction)
                    {
                        case 0: return true;
                        case 1: return true;
                    }
                    break;
                case 3:
                    switch (after.Direction)
                    {
                        case 0: return true;
                        case 1: return true;
                    }
                    break;
                case 4:
                    switch (after.Direction)
                    {
                        case 5: return true;
                        case 6: return true;
                    }
                    break;
                case 5:
                    switch (after.Direction)
                    {
                        case 4: return true;
                        case 7: return true;
                    }
                    break;
                case 6:
                    switch (after.Direction)
                    {
                        case 4: return true;
                        case 7: return true;
                    }
                    break;
                case 7:
                    switch (after.Direction)
                    {
                        case 5: return true;
                        case 6: return true;
                    }
                    break;
            }

            return false;
        }

        public static (float movement, int countInverted) FindMovement(Cube before, Cube after)
        {
            var multiplier = 0f;
            var countInverted = 0;

            if (!(before.Line == after.Line && before.Layer == after.Layer))
            {
                if (DiagonalSwing.Contains(before.Direction))
                {
                    if (before.Direction == 4 || before.Direction == 7) // Up-Left and Down-Right
                    {
                        if (before.Layer == after.Layer - 1 && before.Line == after.Line - 1) // Side multiplier
                        {
                            multiplier += Math.Abs(before.Line - after.Line) * Movement;
                        }
                        else if (before.Layer == after.Layer + 1 && before.Line == after.Line + 1) // Side multiplier
                        {
                            multiplier += Math.Abs(before.Line - after.Line) * Movement;
                        }
                        else if (before.Layer == after.Layer - 2 && before.Line == after.Line - 2) // Side multiplier
                        {
                            multiplier += Math.Abs(before.Line - after.Line) * Movement;
                        }
                        else if (before.Layer == after.Layer + 2 && before.Line == after.Line + 2) // Side multiplier
                        {
                            multiplier += Math.Abs(before.Line - after.Line) * Movement;
                        }

                        if (before.Direction == 4 && before.Layer <= after.Layer && before.Line >= after.Line) // Inverted
                        {
                            multiplier += Math.Max(Math.Abs(before.Line - after.Line), Math.Abs(before.Layer - after.Layer)) * Movement * Inverted;
                            countInverted++;
                        }
                        else if (before.Direction == 7 && before.Layer >= after.Layer && before.Line <= after.Line) // Inverted
                        {
                            multiplier += Math.Max(Math.Abs(before.Line - after.Line), Math.Abs(before.Layer - after.Layer)) * Movement * Inverted;
                            countInverted++;
                        }
                    }
                    else if (before.Direction == 5 && before.Direction == 6) // Up-Right and Down-Left
                    {
                        if (before.Layer == after.Layer + 1 && before.Line == after.Line - 1) // Side multiplier
                        {
                            multiplier += Math.Abs(before.Line - after.Line) * Movement;
                        }
                        else if (before.Layer == after.Layer - 1 && before.Line == after.Line + 1) // Side multiplier
                        {
                            multiplier += Math.Abs(before.Line - after.Line) * Movement;
                        }
                        else if (before.Layer == after.Layer + 2 && before.Line == after.Line - 2) // Side multiplier
                        {
                            multiplier += Math.Abs(before.Line - after.Line) * Movement;
                        }
                        else if (before.Layer == after.Layer - 2 && before.Line == after.Line + 2) // Side multiplier
                        {
                            multiplier += Math.Abs(before.Line - after.Line) * Movement;
                        }

                        if (before.Direction == 5 && before.Layer <= after.Layer && before.Line <= after.Line) // Inverted
                        {
                            multiplier += Math.Max(Math.Abs(before.Line - after.Line), Math.Abs(before.Layer - after.Layer)) * Movement * Inverted;
                            countInverted++;
                        }
                        else if (before.Direction == 6 && before.Layer >= after.Layer && before.Line >= after.Line) // Inverted
                        {
                            multiplier += Math.Max(Math.Abs(before.Line - after.Line), Math.Abs(before.Layer - after.Layer)) * Movement * Inverted;
                            countInverted++;
                        }
                    }
                }
                else if (VerticalSwing.Contains(before.Direction))
                {
                    if (before.Line != after.Line && before.Layer == after.Layer) // Side multiplier
                    {
                        multiplier += Math.Abs(before.Line - after.Line) * Movement;
                    }

                    if (before.Layer < after.Layer && before.Direction == 0 && after.Direction == 1) // Inverted
                    {
                        multiplier += Math.Abs(before.Layer - after.Layer) * Movement * Inverted;
                        countInverted++;
                    }
                    else if (before.Layer > after.Layer && before.Direction == 1 && after.Direction == 0) // Inverted
                    {
                        multiplier += Math.Abs(before.Layer - after.Layer) * Movement * Inverted;
                        countInverted++;
                    }
                }
                else // Horizontal
                {
                    if (before.Layer != after.Layer && before.Line == after.Line)
                    {
                        multiplier += Math.Abs(before.Layer - after.Layer) * Movement;
                    }

                    if (before.Line > after.Line && before.Direction == 2 && after.Direction == 3) // Inverted
                    {
                        multiplier += Math.Abs(before.Line - after.Line) * Movement * Inverted;
                        countInverted++;
                    }
                    else if (before.Line < after.Line && before.Direction == 3 && after.Direction == 2) // Inverted
                    {
                        multiplier += Math.Abs(before.Line - after.Line) * Movement * Inverted;
                        countInverted++;
                    }
                }
            }

            return (multiplier, countInverted);
        }

        public static void FixPatternHead(List<Cube> cubes)
        {
            for (int j = 1; j < cubes.Count(); j++)
            {
                for (int i = 1; i < cubes.Count(); i++)
                {
                    if (cubes[i].Note.beat == cubes[i - 1].Note.beat)
                    {
                        switch (cubes[i - 1].Direction)
                        {
                            case 0: // Either this is wrong or both note are on same layer (loloppe notes), swapping should be fine right...
                                if (cubes[i - 1].Layer > cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 1:
                                if (cubes[i - 1].Layer < cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 2:
                                if (cubes[i - 1].Line < cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 3:
                                if (cubes[i - 1].Line > cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 4: // I know it's not great, but good enough for now
                                if (cubes[i - 1].Line < cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                else if (cubes[i - 1].Layer > cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 5:
                                if (cubes[i - 1].Line > cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                else if (cubes[i - 1].Layer > cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 6:
                                if (cubes[i - 1].Line < cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                else if (cubes[i - 1].Layer < cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 7:
                                if (cubes[i - 1].Line > cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                else if (cubes[i - 1].Layer < cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                        }
                    }
                }
            }
        }

        public static void FindReset(List<Cube> cubes, List<Cube> compare)
        {
            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head) // Not a reset, since it's the same swing
                {
                    continue; // Reset is false by default
                }

                if (SameDirection(cubes[i - 1].Direction, cubes[i].Direction)) // Reset
                {
                    cubes[i].Reset = true;
                    continue;
                }

                if (cubes[i].Beat - cubes[i - 1].Beat >= 0.9f) // Assume that everything that's 1 beat or higher are soft reset.
                {
                    cubes[i].SoftReset = true;
                }
            }
        }

        public static bool SameDirection(int before, int after)
        {
            switch(before)
            {
                case 0: 
                    if (UpSwing.Contains(after)) return true;
                    break;
                case 1:
                    if (DownSwing.Contains(after)) return true;
                    break;
                case 2:
                    if (LeftSwing.Contains(after)) return true;
                    break;
                case 3:
                    if (RightSwing.Contains(after)) return true;
                    break;
                case 4:
                    if (UpLeftSwing.Contains(after)) return true;
                    break;
                case 5:
                    if (UpRightSwing.Contains(after)) return true;
                    break;
                case 6:
                    if (DownLeftSwing.Contains(after)) return true;
                    break;
                case 7:
                    if (DownRightSwing.Contains(after)) return true;
                    break;
            }

            return false;
        }

        public static void FindNoteDirection(List<Cube> cubes, List<BombNoteData> bombs)
        {
            if (((int)cubes[0].Note.cutDirection) == 8) // We find the first arrow note and then go backward
            {
                var c = cubes.Where(c => !c.Assumed).FirstOrDefault();
                if (c != null)
                {
                    int temp = 1;
                    for (int i = 0; i < cubes.IndexOf(c); i++)
                    {
                        temp = ReverseCutDirection((int)c.Note.cutDirection);
                    }
                    cubes[0].Direction = temp;
                }
                else // No choice but to assume, there's no arrow
                {
                    if (cubes[0].Note.layer == 2)
                    {
                        cubes[0].Direction = 0;
                    }
                    else
                    {
                        cubes[0].Direction = 1;
                    }
                }
            }
            else
            {
                cubes[0].Direction = (int)cubes[0].Note.cutDirection;
            }

            bool pattern = false;

            // This won't fix dot note properly, we will have to call it again after this method run
            FixPatternHead(cubes);

            // So now the note should technically be in proper order.. (or at least it shouldn't matter much)

            for (int i = 1; i < cubes.Count(); i++)
            {
                // Here we try to find if notes are part of the same swing
                if (cubes[i].Beat - cubes[i - 1].Beat <= 0.15 && (cubes[i].Note.cutDirection == cubes[i - 1].Note.cutDirection || // A bit faster than 1/8 and same direction
                    cubes[i].Assumed || SameDirection(cubes[i - 1].Direction, (int)cubes[i].Note.cutDirection))) // Or if the next note is a dot, or if parity break
                {
                    if (!pattern)
                    {
                        cubes[i - 1].Head = true;
                    }

                    cubes[i - 1].Pattern = true;
                    cubes[i].Pattern = true;
                    pattern = true;
                }
                else
                {
                    pattern = false;
                }

                BombNoteData bo = null;
                if (i != cubes.Count() - 1)
                {
                    bo = bombs.FirstOrDefault(b => cubes[i - 1].Beat < b.beat && cubes[i].Beat >= b.beat && cubes[i].Line == b.line);
                }

                if (bo != null)
                {
                    cubes[i].Bomb = true; // Bomb between, could be a reset
                }

                if(cubes[i].Pattern && !cubes[i].Head && cubes[i - 1].Bomb)
                {
                    cubes[i].Bomb = cubes[i - 1].Bomb;
                }

                if (cubes[i].Assumed && !cubes[i].Pattern && !cubes[i].Bomb) // Reverse the direction if there's no bomb reset and it's a dot
                {
                    cubes[i].Direction = ReverseCutDirection(cubes[i - 1].Direction);
                }
                else if (cubes[i].Assumed && cubes[i].Pattern) // Part of a pattern, the direction is the same as the last probably
                {
                    cubes[i].Direction = cubes[i - 1].Direction;
                }
                else if (cubes[i].Assumed && cubes[i].Bomb) // Is a dot and there's a bomb near in the same lane, probably a reset
                {
                    // For simplicity purpose
                    if (bo.layer == 0)
                    {
                        cubes[i].Direction = 1;
                    }
                    else if (bo.layer == 1)
                    {
                        if (cubes[i].Layer == 0)
                        {
                            cubes[i].Direction = 0;
                        }
                        else
                        {
                            cubes[i].Direction = 1;
                        }
                    }
                    else if (bo.layer == 2)
                    {
                        cubes[i].Direction = 0;
                    }
                }
                else // Current direction is fine
                {
                    cubes[i].Direction = (int)cubes[i].Note.cutDirection;
                }
            }      
        }

        public static int ReverseCutDirection(int direction)
        {
            return direction switch
            {
                0 => 1,
                1 => 0,
                2 => 3,
                3 => 2,
                4 => 7,
                5 => 6,
                6 => 5,
                7 => 4,
                _ => 8,
            };
        }

        public static void FindPalmUp(List<Cube> cubes)
        {
            foreach(var cube in cubes)
            {
                switch (cube.Forehand)
                {
                    case true:
                        if (UpSwing.Contains((int)cube.Note.cutDirection))
                        {
                            cube.PalmUp = true;
                        }
                        else
                        {
                            cube.PalmUp = false;
                        }
                        break;
                    case false:
                        if (DownSwing.Contains((int)cube.Note.cutDirection))
                        {
                            cube.PalmUp = true;
                        }
                        else
                        {
                            cube.PalmUp = false;
                        }
                        break;
                }
            }
        }

        public static void FindForeHand(List<Cube> cubes)
        {
            if (DownSwing.Contains(cubes[0].Direction))
            {
                cubes[0].Forehand = true;
            }
            else
            {
                cubes[0].Forehand = false;
            }

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head) // Same swing
                {
                    cubes[i].Forehand = cubes[i - 1].Forehand;
                }
                else if (cubes[i].Reset || cubes[i].SoftReset) // Assume the most comfortable position
                {
                    if (DownSwing.Contains(cubes[i].Direction))
                    {
                        cubes[i].Forehand = true;
                    }
                    else if (UpSwing.Contains(cubes[i].Direction))
                    {
                        cubes[i].Forehand = false;
                    }
                    else if (LeftSwing.Contains(cubes[i].Direction) && (int)cubes[i].Note.color == 0)
                    {
                        cubes[i].Forehand = false;
                    }
                    else if(RightSwing.Contains(cubes[i].Direction) && (int)cubes[i].Note.color == 0)
                    {
                        cubes[i].Forehand = true;
                    }
                    else if (LeftSwing.Contains(cubes[i].Direction) && (int)cubes[i].Note.color == 1)
                    {
                        cubes[i].Forehand = true;
                    }
                    else if (RightSwing.Contains(cubes[i].Direction) && (int)cubes[i].Note.color == 1)
                    {
                        cubes[i].Forehand = false;
                    }
                }
                else // Other direction
                {
                    cubes[i].Forehand = !cubes[i - 1].Forehand;
                }
            }
        }
    }
}
