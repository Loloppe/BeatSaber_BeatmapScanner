#region Import

using static BeatmapSaveDataVersion3.BeatmapSaveData;
using BeatmapScanner.Algorithm.LackWiz;
using BeatmapScanner.Algorithm.Loloppe;
using System.Collections.Generic;
using BeatmapSaveDataVersion3;
using System.Linq;
using System;

#endregion

namespace BeatmapScanner.Algorithm
{
    internal class BeatmapScanner
    {
        #region Algorithm value

        #endregion

        #region Analyzer

        public static (double diff, double tech, double ebpm, double slider, double reset, double bomb, int crouch, double linear) Analyzer(List<ColorNoteData> notes, List<BombNoteData> bombs, List<BeatmapSaveData.ObstacleData> obstacles, float bpm)
        {
            #region Prep

            var pass = 0d;
            var tech = 0d;
            var ebpm = 0d;
            var reset = 0d;
            var bomb = 0d;
            var slider = 0d;
            var crouch = 0;
            var linear = 0d;

            List<Cube> cube = new();
            List<SwingData> data = new();

            foreach(var note in notes)
            {
                cube.Add(new Cube(note));
            }

            cube.OrderBy(c => c.Beat);
            var red = cube.Where(c => (int)c.Note.color == 0).ToList();
            var blue = cube.Where(c => (int)c.Note.color == 1).ToList();

            #endregion

            #region Algorithm

            if (red.Count() > 0)
            {
                Helper.FindNoteDirection(red, bombs, bpm);
                Helper.FixPatternHead(red);
                Helper.FindReset(red);
                ebpm = GetEBPM(red, bpm);
                Helper.CalculateDistance(red);
            }

            if (blue.Count() > 0)
            {
                Helper.FindNoteDirection(blue, bombs, bpm);
                Helper.FixPatternHead(blue);
                Helper.FindReset(blue);
                ebpm = Math.Max(GetEBPM(blue, bpm), ebpm);
                Helper.CalculateDistance(blue);
            }

            (pass, tech, data) = Method.UseLackWizAlgorithm(red.Select(c => c.Note).ToList(), blue.Select(c => c.Note).ToList(), bpm);

            #endregion

            #region Calculator

            if(Settings.Instance.SliderPercent)
            {
                slider = Math.Round((double)cube.Where(c => c.Slider && (c.Head || !c.Pattern)).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);
            }
            else
            {
                slider = Math.Round((double)cube.Where(c => c.Slider && (c.Head || !c.Pattern)).Count(), 0);
            }
            if (Settings.Instance.LinearPercent)
            {
                linear = Math.Round((double)cube.Where(c => c.Linear && (c.Head || !c.Pattern)).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);
            }
            else
            {
                linear = Math.Round((double)cube.Where(c => c.Linear && (c.Head || !c.Pattern)).Count(), 0);
            }
            if (Settings.Instance.ResetPercent)
            {
                reset = Math.Round((double)cube.Where(c => c.Reset && !c.Bomb && (c.Head || !c.Pattern)).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);
                bomb = Math.Round((double)cube.Where(c => c.Reset && c.Bomb && (c.Head || !c.Pattern)).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);
            }
            else
            {
                reset = Math.Round((double)cube.Where(c => c.Reset && !c.Bomb && (c.Head || !c.Pattern)).Count(), 0);
                bomb = Math.Round((double)cube.Where(c => c.Reset && c.Bomb && (c.Head || !c.Pattern)).Count(), 0);
            }

            // Find group of walls and list them together
            List<List<BeatmapSaveData.ObstacleData>> wallsGroup = new()
            {
                new List<BeatmapSaveData.ObstacleData>()
            };

            for (int i = 0; i < obstacles.Count(); i++)
            {
                wallsGroup.Last().Add(obstacles[i]);

                for (int j = i; j < obstacles.Count() - 1; j++)
                {
                    if (obstacles[j + 1].beat >= obstacles[j].beat && obstacles[j + 1].beat <= obstacles[j].beat + obstacles[j].duration)
                    {
                        wallsGroup.Last().Add(obstacles[j + 1]);
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

            foreach(var group in wallsGroup)
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
                    if(wall.layer >= 2 && wall.width >= 3)
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
                            if(k == j + 100) // So it doesn't take forever on some maps :(
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

            #endregion

            return (pass, tech, ebpm, slider, reset, bomb, crouch, linear);
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

                var duration = (cubes[i].Beat - cubes[i - 1].Beat);

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
