using static BeatmapSaveDataVersion3.BeatmapSaveData;
using BeatmapScanner.Algorithm.Loloppe;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BeatmapScanner.Algorithm
{
    internal class Helper
    {
        #region Array

        public static double[] DirectionToDegree = { 90, 270, 180, 0, 135, 45, 225, 315, 8 };

        #endregion

        public static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static void SwapValue(List<Cube> list, int indexA, int indexB)
        {
            (list[indexB].Head, list[indexA].Head) = (list[indexA].Head, list[indexB].Head);
            (list[indexB].Reset, list[indexA].Reset) = (list[indexA].Reset, list[indexB].Reset);
        }

        public static void FixPatternHead(List<Cube> cubes)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int i = 1; i < cubes.Count(); i++)
                {
                    if (cubes[i].Note.beat == cubes[i - 1].Note.beat)
                    {
                        switch (cubes[i - 1].Direction)
                        {
                            case 0:
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
                            case 4: 
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

        public static void FindReset(List<Cube> cubes)
        {
            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head)
                {
                    continue; 
                }

                if (IsSameDirection(cubes[i - 1].Direction, cubes[i].Direction))
                {
                    cubes[i].Reset = true;
                    continue;
                }
            }
        }

        public static bool IsSameDirection(double before, double after)
        {
            if (before - after <= 180)
            {
                var diff = Math.Abs(before - after);
                if (diff <= 67.5)
                {
                    return true;
                }
            }
            else if (before - after > 180)
            {
                var diff = 360 - Math.Abs(before - after);
                if (diff <= 67.5)
                {
                    return true;
                }
            }

            return false;
        }

        public static void FindNoteDirection(List<Cube> cubes, List<BombNoteData> bombs, float bpm)
        {
            if (cubes[0].Assumed)
            {
                var c = cubes.Where(ca => !ca.Assumed).FirstOrDefault();
                if (c != null)
                {
                    double temp = 270;
                    for (int i = 0; i < cubes.IndexOf(c); i++)
                    {
                        var a = DirectionToDegree[(int)c.Note.cutDirection] + c.Note.angleOffset;
                        if (a >= 360)
                        {
                            a -= 180;
                        }
                        temp = ReverseCutDirection(a);
                    }
                    cubes[0].Direction = temp;
                }
                else
                {
                    if (cubes[0].Note.layer == 2)
                    {
                        cubes[0].Direction = 90;
                    }
                    else
                    {
                        cubes[0].Direction = 270;
                    }
                }
            }
            else
            {
                var a = DirectionToDegree[(int)cubes[0].Note.cutDirection] + cubes[0].Note.angleOffset;
                if (a >= 360)
                {
                    a -= 180;
                }
                cubes[0].Direction = a;
            }

            bool pattern = false;

            FixPatternHead(cubes);

            BombNoteData bo = null;

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Beat - cubes[i - 1].Beat < 0.25 && (cubes[i].Note.cutDirection == cubes[i - 1].Note.cutDirection ||
                    cubes[i].Assumed || cubes[i - 1].Assumed || IsSameDirection(cubes[i - 1].Direction, DirectionToDegree[(int)cubes[i].Note.cutDirection] + cubes[i].Note.angleOffset)))
                {
                    if (!pattern)
                    {
                        cubes[i - 1].Head = true;
                        if (cubes[i].Beat - cubes[i - 1].Beat < 0.26 && cubes[i].Beat - cubes[i - 1].Beat >= 0.01)
                        {
                            cubes[i - 1].Slider = true;
                        }
                    }

                    cubes[i - 1].Pattern = true;
                    cubes[i].Pattern = true;
                    pattern = true;
                }
                else
                {
                    pattern = false;
                }

                bo = bombs.LastOrDefault(b => cubes[i - 1].Beat < b.beat && cubes[i].Beat >= b.beat && cubes[i].Line == b.line);

                if (bo != null)
                {
                    cubes[i].Bomb = true;
                }

                if (cubes[i].Pattern && cubes[i - 1].Bomb)
                {
                    cubes[i].Bomb = cubes[i - 1].Bomb;
                }

                if (cubes[i].Assumed && !cubes[i].Pattern && !cubes[i].Bomb)
                {
                    cubes[i].Direction = ReverseCutDirection((int)cubes[i - 1].Direction);
                }
                else if (cubes[i].Assumed && cubes[i].Pattern)
                {
                    cubes[i].Direction = cubes[i - 1].Direction;
                }
                else if (cubes[i].Assumed && cubes[i].Bomb)
                {
                    if (bo.layer == 0)
                    {
                        cubes[i].Direction = 270;
                    }
                    else if (bo.layer == 1)
                    {
                        if (cubes[i].Layer == 0)
                        {
                            cubes[i].Direction = 90;
                        }
                        else
                        {
                            cubes[i].Direction = 270;
                        }
                    }
                    else if (bo.layer == 2)
                    {
                        cubes[i].Direction = 90;
                    }
                }
                else
                {
                    var a = DirectionToDegree[(int)cubes[i].Note.cutDirection] + cubes[i].Note.angleOffset;
                    if (a >= 360)
                    {
                        a -= 180;
                    }
                    cubes[i].Direction = a;
                }
            }
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

        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = degrees * (Math.PI / 180f);
            return (radians);
        }

        public static ((double x, double y) entry, (double x, double y) exit) CalculateBaseEntryExit((double x, double y) position, double angle)
        {
            (double, double) entry = (position.x * 0.333333 - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667,
                position.y * 0.333333 - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667);

            (double, double) exit = (position.x * 0.333333 + Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667f + 0.166667,
                position.y * 0.333333 + Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667);

            return (entry, exit);
        }

        public static bool IsInLinearPath(Cube previous, Cube current, Cube next)
        {
            var prev = CalculateBaseEntryExit((previous.Line, previous.Layer), previous.Direction);
            var curr = CalculateBaseEntryExit((current.Line, current.Layer), current.Direction);
            ((double x, double y) entry, (double x, double y) exit) nxt;
            if (IsSameDirection(previous.Direction, next.Direction))
            {
                nxt = CalculateBaseEntryExit((next.Line, next.Layer), previous.Direction);
            }
            else
            {
                nxt = CalculateBaseEntryExit((next.Line, next.Layer), next.Direction);
            }

            var dxc = nxt.entry.x - prev.entry.x;
            var dyc = nxt.entry.y - prev.entry.y;

            var dxl = curr.exit.x - prev.entry.x;
            var dyl = curr.exit.y - prev.entry.y;

            var cross = dxc * dyl - dyc * dxl;
            if (cross != 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void CalculateDistance(List<Cube> cubes)
        {
            Cube pre = cubes[1];
            Cube pre2 = cubes[0];

            cubes[0].Linear = true;
            cubes[1].Linear = true;

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (!cubes[i].Pattern || cubes[i].Head)
                {
                    // var prev = CalculateBaseEntryExit((pre.Line, pre.Layer), pre.Direction);
                    // var now = CalculateBaseEntryExit((cubes[i].Line, cubes[i].Layer), cubes[i].Direction);
                    // var distance = Math.Sqrt(Math.Pow(now.exit.x - prev.entry.x, 2) + Math.Pow(now.exit.y - prev.entry.y, 2));

                    if (IsInLinearPath(pre2, pre, cubes[i]))
                    {
                        cubes[i].Linear = true;
                    }

                    pre2 = pre;
                    pre = cubes[i];
                }
            }
        }
    }
}
