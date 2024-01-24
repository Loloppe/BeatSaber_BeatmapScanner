using static Analyzer.BeatmapScanner.Helper.Helper;
using static Analyzer.BeatmapScanner.Helper.FindAngleViaPosition;
using Analyzer.BeatmapScanner.Data;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics;
using BeatmapScanner;

namespace Analyzer.BeatmapScanner.Helper
{
    internal class HandlePatternOrdering
    {
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
                        direction = ReverseCutDirection(Mod(DirectionToDegree[arrow.Last().CutDirection] + arrow.Last().AngleOffset, 360));
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
    }
}
