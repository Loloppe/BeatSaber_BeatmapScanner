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

        public static float MaxNerfMS = 500f;
        public static float MinNerfMS = 250f;
        public static float NormalizedMax = 5f;   
        public static float NormalizedMin = 0f;

        public static float MinNote = 80f;

        public static float Speed = 0.00125f;
        public static float Reset = 1.1f;

        #endregion

        #region Analyzer

        public static (double diff, double tech, double ebpm, int slider, int reset, int bomb, int crouch) Analyzer(List<ColorNoteData> notes, List<BombNoteData> bombs, List<BeatmapSaveData.ObstacleData> obstacles, float bpm)
        {
            #region Prep

            var pass = 0d;
            var tech = 0d;
            var ebpm = 0d;
            var reset = 0;
            var bomb = 0;
            var slider = 0;
            var crouch = 0;

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
                ebpm = GetIntensity(red, bpm);
            }

            if (blue.Count() > 0)
            {
                Helper.FindNoteDirection(blue, bombs, bpm);
                Helper.FixPatternHead(blue);
                Helper.FindReset(blue);
                ebpm = Math.Max(GetIntensity(blue, bpm), ebpm);
            }

            (pass, tech, data) = Method.UseLackWizAlgorithm(red.Select(c => c.Note).ToList(), blue.Select(c => c.Note).ToList(), bpm);

            #endregion

            #region Calculator

            foreach(var c in cube)
            {
                if((c.Pattern && c.Head) || !c.Pattern)
                {
                    if (c.Reset && !c.Bomb)
                    {
                        Plugin.Log.Error("Reset:" + c.Beat + " " + c.Note.color);
                        reset++;
                    }
                    else if (c.Reset)
                    {
                        Plugin.Log.Error("Bomb:" + c.Beat + " " + c.Note.color);
                        bomb++;
                    }
                }
                
                if(c.Head && c.Slider)
                {
                    slider++;
                }
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

            if (data.Count() > 0)
            {
                var temp = 1 - cube.Where(c => c.Reset).Count() * 1.25 / cube.Count();
                if(temp < 0)
                {
                    temp = 0;
                }
                tech *= temp;
                if (cube.Count() > 0)
                {
                    var nerf = 1d; 

                    for (int i = 1; i < cube.Count(); i++) 
                    {
                        if (!cube[i].Pattern || cube[i].Head)
                        {
                            var timeInMS = MathUtil.ConvertBeatToMS(cube[i].Beat - cube[i - 1].Beat, bpm);

                            if (timeInMS > MinNerfMS)
                            {
                                var normalized = MathUtil.NormalizeVariable(timeInMS);
                                nerf += MathUtil.ReduceWithExponentialCurve(2, 0, 1, normalized);
                                continue;
                            }
                        }

                        nerf++;
                    }
                    tech *= nerf / cube.Where(c => !c.Pattern || c.Head).Count();
                }
                tech -= 0.5;
                if (tech < 0)
                {
                    tech = 0;
                }
            }

            #endregion

            return (pass, tech, ebpm, slider, reset, bomb, crouch);
        }

        #endregion

        #region Intensity

        public static float GetIntensity(List<Cube> cubes, float bpm)
        {
            #region Prep

            var intensity = 1f;
            var speed = (Speed * bpm);
            var previous = 0f;
            var ebpm = 0f;
            var pbpm = 0f;
            var count = 0;
            var prev = cubes[0].Beat;

            #endregion

            #region Algorithm

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head)
                {
                    continue;
                }

                var time = (cubes[i].Beat - prev);

                if(time > 0)
                {
                    if(Settings.Instance.EBPM > 1)
                    {
                        if (previous == (500 / time) && (500 / time) > ebpm)
                        {
                            count++;
                            if (count == Settings.Instance.EBPM - 1)
                            {
                                ebpm = previous;
                            }
                        }
                        else
                        {
                            count = 0;
                        }
                    }    

                    if ((500 / time) > pbpm)
                    {
                        pbpm = previous;
                    }

                    previous = (500 / time);
                }

                if (cubes[i].Reset || cubes[i].Head)
                {
                    if(time != 0f)
                    {
                        intensity += (speed / time) * Reset;
                    }
                }
                else
                {
                    if (time != 0f)
                    {
                        intensity += speed / time;
                    }
                }

                prev = cubes[i].Beat;
            }

            #endregion

            if(ebpm == 0)
            {
                ebpm = pbpm;
            }
            ebpm *= bpm / 1000;
            intensity /= cubes.Where(c => !c.Pattern || c.Head).Count();

            return ebpm;
        }

        #endregion

    }
}
