#region Import

using BeatmapScanner.Algorithm.LackWiz;
using BeatmapScanner.Algorithm.Loloppe;
using System;
using System.Collections.Generic;
using System.Linq;
using static BeatmapSaveDataVersion3.BeatmapSaveData;

#endregion

namespace BeatmapScanner.Algorithm
{
    internal class BeatmapScanner
    {
        #region Array
        public static int[] VerticalSwing = { 0, 1, 4, 5, 6, 7 };
        public static int[] HorizontalSwing = { 2, 3, 4, 5, 6, 7 };
        public static int[] DiagonalSwing = { 4, 5, 6, 7 };
        public static int[] PureVerticalSwing = { 0, 1 };
        public static int[] PureHorizontalSwing = { 2, 3 };

        public static int[] UpSwing = { 0, 4, 5 };
        public static int[] DownSwing = { 1, 6, 7 };
        public static int[] LeftSwing = { 2, 4, 6 };
        public static int[] RightSwing = { 3, 5, 7 };
        public static int[] UpLeftSwing = { 0, 2, 4 };
        public static int[] DownLeftSwing = { 1, 2, 6 };
        public static int[] UpRightSwing = { 0, 3, 5 };
        public static int[] DownRightSwing = { 1, 3, 7 };

        public static int[] CutDirectionIndex = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };
        #endregion

        #region Algorithm value

        // Nerf T and M algorithm using an exponential curve
        public static float MaxNerfMS = 500f;
        public static float MinNerfMS = 250f;
        public static float NormalizedMax = 5f;   
        public static float NormalizedMin = 0f;

        // Nerf/buff map based on notes count
        public static float MinNote = 80f;
        public static float MaxNote = 10000f;

        // I multipler
        public static float Speed = 0.00125f;
        public static float Reset = 1.1f;

        #endregion

        #region Analyzer

        public static (float star, float tech, float intensity) Analyzer(List<ColorNoteData> notes, List<BombNoteData> bombs, float bpm)
        {
            #region Prep

            var diff = 0d;
            var tech = 0d;
            var intensity = 0d;

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
                intensity += GetIntensity(red, bpm);
            }

            if (blue.Count() > 0)
            {
                Helper.FindNoteDirection(blue, bombs);
                Helper.FixPatternHead(blue);
                Helper.FindReset(blue);
                intensity += GetIntensity(blue, bpm);
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
            else
            {
                var normalized = MathUtil.NormalizeVariable2(MaxNote / Math.Max(notes.Count(), MaxNote));
                var buff = MathUtil.ReduceWithExponentialCurve(2, 0, 1, normalized);

                intensity *= buff;
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

            return ((float)Math.Round(diff, 2), (float)Math.Round(tech, 2), (float)Math.Round(intensity, 2));
        }

        #endregion

        #region Intensity

        public static float GetIntensity(List<Cube> cubes, float bpm)
        {
            #region Prep

            var intensity = 1f;
            var speed = (Speed * bpm);

            #endregion

            #region Algorithm

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head) // Skip rest of pattern
                {
                    continue;
                }

                var time = (cubes[i].Beat - cubes[i - 1].Beat);

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

            return intensity / cubes.Where(c => !c.Pattern || c.Head).Count();
        }

        #endregion

    }
}
