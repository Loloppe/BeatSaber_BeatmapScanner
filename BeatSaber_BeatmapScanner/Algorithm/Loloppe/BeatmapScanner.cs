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

        public static (double diff, double tech, double ebpm, int slider, int reset, int crouch) Analyzer(List<ColorNoteData> notes, List<BombNoteData> bombs, List<BeatmapSaveData.ObstacleData> obstacles, float bpm)
        {
            #region Prep

            var diff = 0d;
            var tech = 0d;
            var intensity = 0d;
            var ebpm = 0d;
            var reset = 0;
            var slider = 0;
            var crouch = 0;

            List<Cube> cube = new();
            List<SwingData> data = new();

            foreach(var note in notes)
            {
                cube.Add(new Cube(note));
            }

            var red = cube.Where(c => (int)c.Note.color == 0).ToList();
            var blue = cube.Where(c => (int)c.Note.color == 1).ToList();

            #endregion

            #region Algorithm

            if (red.Count() > 0)
            {
                Helper.FindNoteDirection(red, bombs, bpm);
                Helper.FixPatternHead(red);
                Helper.FindReset(red);
                (intensity, ebpm) = GetIntensity(red, bpm);
            }

            if (blue.Count() > 0)
            {
                Helper.FindNoteDirection(blue, bombs, bpm);
                Helper.FixPatternHead(blue);
                Helper.FindReset(blue);
                var temp = 0f;
                var temp2 = 0f;
                (temp, temp2) = GetIntensity(blue, bpm);
                ebpm = Math.Max(ebpm, temp2);
                intensity += temp;
            }

            (diff, tech, data) = Method.UseLackWizAlgorithm(red.Select(c => c.Note).ToList(), blue.Select(c => c.Note).ToList(), bpm);

            #endregion

            #region Calculator

            foreach(var c in cube)
            {
                if(c.Reset)
                {
                    reset++;
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

                for (int j = i + 1; j < obstacles.Count(); j++)
                {
                    if (obstacles[j].beat >= obstacles[i].beat && obstacles[j].beat <= obstacles[i].beat + obstacles[i].duration)
                    {
                        wallsGroup.Last().Add(obstacles[j]);
                    }
                    else
                    {
                        wallsGroup.Add(new List<BeatmapSaveData.ObstacleData>());
                        i = j - 1;
                        break;
                    }
                }
            }

            // Find how many time the player has to crouch
            List<int> wallsFound = new();

            foreach(var group in wallsGroup)
            {
                for(int j = 0; j < group.Count(); j++)
                {
                    bool found = false;
                    var wall = group[j];

                    // Individual
                    if(wall.layer >= 2 && wall.width >= 3)
                    {
                        crouch++;
                        break;
                    }
                    if (wall.layer >= 2 && wall.width >= 2 && wall.line == 1)
                    {
                        crouch++;
                        break;
                    }

                    // Multiple
                    if (group.Count() > 1)
                    {
                        for (int k = j + 1; k < group.Count(); k++)
                        {
                            var other = group[k];

                            if ((wall.layer >= 2 || other.layer >= 2) && wall.width >= 2 && wall.line == 0 && other.line == 2)
                            {
                                found = true;
                            }
                            else if ((wall.layer >= 2 || other.layer >= 2) && other.width >= 2 && wall.line == 2 && other.line == 0)
                            {
                                found = true;
                            }
                            else if ((wall.layer >= 2 || other.layer >= 2) && wall.line == 1 && other.line == 2)
                            {
                                found = true;
                            }
                        }
                    }

                    if(found)
                    {
                        crouch++;
                        break;
                    }
                }
            }

            if (notes.Count() < MinNote)
            {
                intensity *= notes.Count() / MinNote;
            }

            intensity /= 2;
            if (intensity < 0)
            {
                intensity = 0f;
            }

            if(data.Count() > 0)
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

            return (diff, tech, ebpm, slider, reset, crouch);
        }

        #endregion

        #region Intensity

        public static (float, float) GetIntensity(List<Cube> cubes, float bpm)
        {
            #region Prep

            var intensity = 1f;
            var speed = (Speed * bpm);
            var ebpm = 1f;
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

                if(ebpm < (500 / time))
                {
                    if (time > 0)
                    {
                        ebpm = (500 / time);
                    }
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

            ebpm *= bpm / 1000;

            return (intensity / cubes.Where(c => !c.Pattern || c.Head).Count(), ebpm);
        }

        #endregion

    }
}
