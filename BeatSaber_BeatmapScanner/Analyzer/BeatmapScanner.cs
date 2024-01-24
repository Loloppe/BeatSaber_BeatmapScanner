using Analyzer.BeatmapScanner.Algorithm;
using Analyzer.BeatmapScanner.Data;
using BeatmapSaveDataVersion3;
using System;
using System.Collections.Generic;
using System.Linq;
using static BeatmapSaveDataVersion3.BeatmapSaveData;

namespace BeatmapScanner.Algorithm
{
    internal class BeatmapScanner
    {
        #region Algorithm value

        #endregion

        #region Analyzer

        public static List<double> Analyzer(List<ColorNoteData> notes, List<BurstSliderData> chains, List<BombNoteData> bombs, List<BeatmapSaveData.ObstacleData> walls, float bpm, float njs)
        {
            List<double> value = new();
            List<Cube> cubes = new();

            foreach (var note in notes)
            {
                cubes.Add(new Cube(note));
            }

            cubes = cubes.OrderBy(c => c.Time).ToList();

            foreach (var chain in chains)
            {
                var found = cubes.FirstOrDefault(x => x.Time == chain.beat && x.Type == (int)chain.colorType && x.Line == chain.headLine && x.Layer == chain.headLayer && x.CutDirection == (int)chain.headCutDirection);
                if (found != null)
                {
                    found.Chain = true;
                    found.TailLine = chain.tailLine;
                    found.TailLayer = chain.tailLayer;
                    found.Squish = chain.squishAmount;
                }
            }

            var red = cubes.Where(c => c.Type == 0).ToList();
            var blue = cubes.Where(c => c.Type == 1).ToList();

            value = Analyze.UseLackWizAlgorithm(red, blue, bpm, njs);

            #region Crouch walls count
            value.Add(0);

            // Find group of walls and list them together
            var crouch = 0;
            List<List<BeatmapSaveData.ObstacleData>> wallsGroup = new()
            {
                new List<BeatmapSaveData.ObstacleData>()
            };

            for (int i = 0; i < walls.Count(); i++)
            {
                wallsGroup.Last().Add(walls[i]);

                for (int j = i; j < walls.Count() - 1; j++)
                {
                    if (walls[j + 1].beat >= walls[j].beat && walls[j + 1].beat <= walls[j].beat + walls[j].duration)
                    {
                        wallsGroup.Last().Add(walls[j + 1]);
                    }
                    else
                    {
                        i = j;
                        wallsGroup.Add(new List<BeatmapSaveData.ObstacleData>());
                        break;
                    }
                }
            }

            // Find how many time the player has to crouch
            List<int> wallsFound = new();
            int count;

            foreach (var group in wallsGroup)
            {
                float found = 0f;
                count = 0;

                for (int j = 0; j < group.Count(); j++)
                {
                    var wall = group[j];

                    if (found != 0f && wall.beat - found < 1.5) // Skip too close
                    {
                        continue;
                    }
                    else
                    {
                        found = 0f;
                    }

                    // Individual
                    if (wall.layer >= 2 && wall.width >= 3)
                    {
                        count++;
                        found = wall.beat + wall.duration;
                    }
                    else if (wall.layer >= 2 && wall.width >= 2 && wall.line == 1)
                    {
                        count++;
                        found = wall.beat + wall.duration;
                    }
                    else if (group.Count() > 1) // Multiple
                    {
                        for (int k = j + 1; k < group.Count(); k++)
                        {
                            if (k == j + 100) // So it doesn't take forever on some maps :(
                            {
                                break;
                            }

                            var other = group[k];

                            if ((wall.layer >= 2 || other.layer >= 2) && wall.width >= 2 && wall.line == 0 && other.line == 2)
                            {
                                count++;
                                found = wall.beat + wall.duration;
                                break;
                            }
                            else if ((wall.layer >= 2 || other.layer >= 2) && other.width >= 2 && wall.line == 2 && other.line == 0)
                            {
                                count++;
                                found = wall.beat + wall.duration;
                                break;
                            }
                            else if ((wall.layer >= 2 || other.layer >= 2) && wall.line == 1 && other.line == 2)
                            {
                                count++;
                                found = wall.beat + wall.duration;
                                break;
                            }
                        }
                    }
                }

                crouch += count;
            }
            value[value.Count - 1] = crouch;
            #endregion

            #region EBPM
            value.Add(0);

            if (red.Count() > 0)
            {
                value[value.Count - 1] = GetEBPM(red, bpm); 
            }

            if (blue.Count() > 0)
            {
                value[value.Count - 1] = Math.Max(GetEBPM(blue, bpm), value.Last());
            }
            #endregion

            return value;
        }

        #endregion

        #region EBPM

        public static float GetEBPM(List<Cube> cubes, float bpm)
        {
            #region Prep

            var previous = 0f;
            var effectiveBPM = 10f;
            var peakBPM = 10f;
            var count = 0;

            #endregion

            #region Algorithm

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head)
                {
                    continue;
                }

                var duration = (cubes[i].Time - cubes[i - 1].Time);

                if(duration > 0)
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

            #endregion

            if (effectiveBPM == 10)
            {
                return bpm;
            }

            effectiveBPM = 0.5f / effectiveBPM * bpm;

            return effectiveBPM;
        }

        #endregion

    }
}
