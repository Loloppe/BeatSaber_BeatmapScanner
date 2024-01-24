using static Analyzer.BeatmapScanner.Helper.HandlePatternOrdering;
using static Analyzer.BeatmapScanner.Helper.Helper;
using static Analyzer.BeatmapScanner.Helper.FindAngleViaPosition;
using static Analyzer.BeatmapScanner.Helper.IsSlider;
using static Analyzer.BeatmapScanner.Helper.IsSameDirection;
using Analyzer.BeatmapScanner.Data;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.BeatmapScanner.Algorithm
{
    internal class FlowDetector
    {
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
                    if ((cubes[i].Time - cubes[i - 1].Time <= 0.125 && SliderCond(cubes[i - 1], cubes[i], lastSimPos, bpm, njs) && IsSameDir(cubes[i - 1].Direction, cubes[i].Direction))
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
            if (cubes.Last().CutDirection == 8)
            {
                if ((cubes.Last().Time - cubes[cubes.Count - 2].Time <= 0.125 && SliderCond(cubes[cubes.Count - 2], cubes.Last(), lastSimPos, bpm, njs))
                    || cubes.Last().Time - cubes[cubes.Count - 2].Time <= 0.0625)
                {
                    (cubes.Last().Direction, lastSimPos) = FindAngleViaPos(cubes, cubes.Count - 1, cubes.Count - 2, cubes[cubes.Count - 2].Direction, true);
                    if (cubes[cubes.Count - 2].CutDirection == 8)
                    {
                        cubes[cubes.Count - 2].Direction = cubes.Last().Direction;
                    }
                    cubes.Last().Pattern = true;
                    if (!cubes[cubes.Count - 2].Pattern)
                    {
                        cubes[cubes.Count - 2].Pattern = true;
                        cubes[cubes.Count - 2].Head = true;
                    }
                }
                else
                {
                    (cubes.Last().Direction, lastSimPos) = FindAngleViaPos(cubes, cubes.Count - 1, cubes.Count - 2, cubes[cubes.Count - 2].Direction, false);
                }
            }
            else
            {
                cubes.Last().Direction = Mod(DirectionToDegree[cubes.Last().CutDirection] + cubes.Last().AngleOffset, 360);
                if (((cubes.Last().Time - cubes[cubes.Count - 2].Time <= 0.125 && SliderCond(cubes[cubes.Count - 2], cubes.Last(), lastSimPos, bpm, njs))
                    || cubes.Last().Time - cubes[cubes.Count - 2].Time <= 0.0625) && IsSameDir(cubes[cubes.Count - 2].Direction, cubes.Last().Direction))
                {
                    cubes.Last().Pattern = true;
                    if (!cubes[cubes.Count - 2].Pattern)
                    {
                        cubes[cubes.Count - 2].Pattern = true;
                        cubes[cubes.Count - 2].Head = true;
                    }
                }
            }
        }
    }
}
