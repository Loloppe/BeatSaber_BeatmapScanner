#region Import

using static BeatmapSaveDataVersion3.BeatmapSaveData;
using BeatmapScanner.Algorithm.LackWiz;
using BeatmapScanner.Algorithm.Loloppe;
using System.Collections.Generic;
using System.Linq;
using System;

#endregion

namespace BeatmapScanner.Algorithm
{
    internal class BeatmapScanner
    {
        #region Algorithm value

        // Nerf T and M algorithm using an exponential curve
        public static float MaxNerfMS = 500f;
        public static float MinNerfMS = 250f;
        public static float NormalizedMax = 5f;   
        public static float NormalizedMin = 0f;

        // Nerf map based on notes count
        public static float MinNote = 80f;

        // I multipler
        public static float Speed = 0.00125f;
        public static float Reset = 1.1f;

        #endregion

        #region Analyzer

        public static (float star, float tech, float intensity, float ebpm) Analyzer(List<ColorNoteData> notes, List<BombNoteData> bombs, float bpm)
        {
            #region Prep

            var diff = 0d;
            var tech = 0d;
            var intensity = 0d;
            var ebpm = 0d;

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
                Helper.FindNoteDirection(red, bombs);
                Helper.FixPatternHead(red);
                Helper.FindReset(red);
                (intensity, ebpm) = GetIntensity(red, bpm);
            }

            if (blue.Count() > 0)
            {
                Helper.FindNoteDirection(blue, bombs);
                Helper.FixPatternHead(blue);
                Helper.FindReset(blue);
                var temp = 0f;
                var temp2 = 0f;
                (temp, temp2) = GetIntensity(blue, bpm);
                ebpm = Math.Max(ebpm, temp2);
                intensity += temp;
            }

            // LackWiz algorithm
            (tech, diff, data) = Method.UseLackWizAlgorithm(red.Select(c => c.Note).ToList(), blue.Select(c => c.Note).ToList(), bpm);

            #endregion

            #region Calculator

            // Nerf if the amount of notes is too low
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
                // Remove tech from DD map
                var temp = 1 - cube.Where(c => c.Reset).Count() * 1.25 / cube.Count();
                if(temp < 0)
                {
                    temp = 0;
                }
                tech *= temp;
                // Nerf tech based on speed (mostly to remove slow DD)
                if (cube.Count() > 0)
                {
                    var nerf = 1d; // The higher number, the less it will be nerfed.

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

            return ((float)Math.Round(diff, 2), (float)Math.Round(tech, 2), (float)Math.Round(intensity, 2), (float)Math.Round(ebpm, 0));
        }

        #endregion

        #region Intensity

        public static (float, float) GetIntensity(List<Cube> cubes, float bpm)
        {
            #region Prep

            var intensity = 1f;
            var speed = (Speed * bpm);
            var ebpm = 1f;

            #endregion

            #region Algorithm

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head) // Skip rest of pattern
                {
                    continue;
                }

                var time = (cubes[i].Beat - cubes[i - 1].Beat);

                if(ebpm < (500 / time)) // Calc EBPM
                {
                    if (time > 0)
                    {
                        ebpm = (500 / time);
                    }
                }

                if (cubes[i].Reset || cubes[i].Head) // Reset and head of pattern
                {
                    if(time != 0f)
                    {
                        intensity += (speed / time) * Reset;
                    }
                }
                else // Other note
                {
                    if (time != 0f)
                    {
                        intensity += speed / time;
                    }
                }

            }

            #endregion

            ebpm *= bpm / 1000;

            return (intensity / cubes.Where(c => !c.Pattern || c.Head).Count(), ebpm);
        }

        #endregion

    }
}
