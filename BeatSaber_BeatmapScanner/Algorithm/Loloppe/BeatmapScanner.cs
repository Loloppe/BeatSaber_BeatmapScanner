#region Import

using BeatmapScanner.Algorithm.Loloppe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        // T multipler
        public static float TechCap = 3f;
        public static float Diagonal = 0f;
        public static float Horizontal = 1f;
        public static float WristRoll = 1f;
        public static float PalmUp = 1f;

        // I multipler
        public static float IntensityCap = 11f; 
        public static float Speed = 0.00125f;
        public static float Reset = 1.1f;

        // M multipler
        public static float MovementCap = 3f;
        public static float Movement = 1f;
        public static float Inverted = 1f;
        public static float Pattern = 0.5f;

        // Duration
        public static float Duration = 300f;
        public static float DurationMultiplier = 0.5f;

        // NJS
        public static float NJS = 10f;
        public static float NJSMultiplier = 0.02f;

        // Global multipler
        public static float Base = 1f;
        public static float T = 15f;
        public static float I = 15f;
        public static float M = 15f;
        public static float Global = 1f;

        #endregion

        #region Analyzer

        public static (float star, float tech, float intensity, float movement) Analyzer(List<ColorNoteData> notes, List<BombNoteData> bombs, float bpm, float njs, float duration)
        {
            #region Prep

            // Multiplier that will be fetched by the algorithm
            var tech = 0f;
            var intensity = 0f;
            var movement = 0f;


            // Separate the note per color, it's easier that way.
            List<Cube> red = new();
            List<Cube> blue = new();

            foreach (var note in notes)
            {
                if (note.color == NoteColorType.ColorA && (int)note.cutDirection != 9)
                {
                    red.Add(new Cube(note));
                }
                else if (note.color == NoteColorType.ColorB && (int)note.cutDirection != 9)
                {
                    blue.Add(new Cube(note));
                }
            }

            #endregion

            #region Algorithm

            if (red.Count() > 0)
            {
                Helper.FindNoteDirection(red, bombs);
                Helper.FixPatternHead(red);
                Helper.FindReset(red, blue);
                Helper.FindForeHand(red);
                Helper.FindPalmUp(red);
                Helper.FindEntryExit(red);
                Helper.FindSwingCurve(red);
                tech += GetTech(red, bpm);
                intensity += GetIntensity(red, bpm);
                movement += GetMovement(red, bpm);
            }

            if (blue.Count() > 0)
            {
                Helper.FindNoteDirection(blue, bombs);
                Helper.FixPatternHead(blue);
                Helper.FindReset(blue, red);
                Helper.FindForeHand(blue);
                Helper.FindPalmUp(blue);
                Helper.FindEntryExit(blue);
                Helper.FindSwingCurve(blue);
                tech += GetTech(blue, bpm);
                intensity += GetIntensity(blue, bpm);
                movement += GetMovement(blue, bpm);
            }

            #endregion

            #region Calculator

            // Nerf if the amount of notes is too low
            if (notes.Count() < MinNote)
            {
                tech *= notes.Count() / MinNote;
                intensity *= notes.Count() / MinNote;
                movement *= notes.Count() / MinNote;
            }
            else
            {
                var normalized = MathUtil.NormalizeVariable2(MaxNote / Math.Max(notes.Count(), MaxNote));
                var buff = MathUtil.ReduceWithExponentialCurve(2, 0, 1, normalized);

                tech *= buff;
                intensity *= buff;
                movement *= buff;
            }

            // Tech
            if (tech < 0)
            {
                tech = 0f;
            }
            var t = tech;
            t *= (float)Math.Pow(0.9, tech);

            // Intensity
            intensity /= 2;
            if (intensity < 0)
            {
                intensity = 0f;
            }
            var i = intensity;
            

            // Movement
            movement /= 2;
            if (movement < 0)
            {
                movement = 0f;
            }
            var m = movement;
            m *= (float)Math.Pow(0.9, movement);

            // NJS
            var js = (njs / NJS) * NJSMultiplier;

            // Duration
            var d = (duration / Duration) * DurationMultiplier;

            // Multiplier
            t *= T;
            i *= I;
            m *= M;

            // Cap
            if(Config.Instance.StarLimiter)
            {
                if (t > TechCap)
                {
                    t = TechCap;
                }
                if (m > MovementCap)
                {
                    m = MovementCap;
                }
                if (i > IntensityCap)
                {
                    i = IntensityCap;
                }
            }
            

            // Final calculation
            float point;
            if (!Config.Instance.StarLimiter)
            {
                point = (Base + d + js + t + i + m);
            }
            else if (tech > movement)
            {
                point = (Base + d + js + t + i + m * 0.2f);
            }
            else
            {
                point = (Base + d + js + t * 0.2f + i + m);
            }

            point--;
            point *= Global;

            if (point < 0) // Minimum value
            {
                point = 0.01f;
            }

            #endregion

            Plugin.Log.Info("T:" + (float)Math.Round(t, 2) + " I:" + (float)Math.Round(i, 2) + " M:" + (float)Math.Round(m, 2));
            Plugin.Log.Info("Star:" + (float)Math.Round(point, 2) + " Tech:" + (float)Math.Round(tech, 2) + " Intensity:" + (float)Math.Round(intensity, 2) + " Movement:" + (float)Math.Round(movement, 2));

            return ((float)Math.Round(point, 2), (float)Math.Round(tech, 2), (float)Math.Round(intensity, 2), (float)Math.Round(movement, 2));
        }

        #endregion

        #region Tech

        public static float GetTech(List<Cube> cubes, float bpm)
        {
            #region Prep

            float tech;
            float horizontal;
            float diagonal;
            float palmUp;
            var skipped = 0f;
            var nerfed = 0;
            var countNo = 0;
            var averageNerfTime = 0f;
            var total = 0f;
            float nerf;
            float timeInMS;
            float normalized;

            #endregion

            #region Algorithm

            for (int i = 1; i < cubes.Count(); i++)
            {
                horizontal = 0f;
                diagonal = 0f;
                palmUp = 0f;
                tech = 0f;
                nerf = 1f;
                timeInMS = MathUtil.ConvertBeatToMS(cubes[i].Beat - cubes[i - 1].Beat, bpm);

                if (cubes[i].Pattern && !cubes[i].Head) // Skip rest of pattern
                {
                    skipped++;
                    continue;
                }

                if (timeInMS > MinNerfMS && timeInMS <= MaxNerfMS) // Nerf tech
                {
                    normalized = MathUtil.NormalizeVariable(timeInMS);
                    nerf = MathUtil.ReduceWithExponentialCurve(2, 0, 1, normalized);
                    averageNerfTime += timeInMS;
                    nerfed++;
                }
                else if (timeInMS > MaxNerfMS) // No tech
                {
                    nerf = 0f;
                    countNo++;
                }

                if (cubes[i].PalmUp) // Palm Up
                {
                    palmUp = PalmUp;
                }

                if (Helper.FindTech(cubes[i - 1], cubes[i])) // Wristroll
                {
                    tech = WristRoll;
                }
                else if (PureHorizontalSwing.Contains(cubes[i].Direction)) // Horizontal
                {
                    horizontal = Horizontal;
                }
                else if (DiagonalSwing.Contains(cubes[i].Direction)) // Diagonal
                {
                    diagonal = Diagonal;
                }

                total += (horizontal + diagonal + palmUp + tech) * nerf;
            }

            total /= (cubes.Count() - skipped);

            #endregion

            return total;
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

        #region Movement

        public static float GetMovement(List<Cube> cubes, float bpm)
        {
            #region Prep

            var movement = 0f;
            var inverted = 0;
            var pattern = 0f;
            var nerfed = 0;
            var removed = 0;
            float multiplier;
            float nerf;
            float timeInMS;
            float normalized;
            int temp;

            #endregion

            #region Algorithm

            for (int i = 1; i < cubes.Count(); i++)
            {
                multiplier = 0f;
                nerf = 1f;
                timeInMS = MathUtil.ConvertBeatToMS(cubes[i].Beat - cubes[i - 1].Beat, bpm);

                if (timeInMS > MinNerfMS && timeInMS <= MaxNerfMS) // Nerf movement
                {
                    nerfed++;
                    normalized = MathUtil.NormalizeVariable(timeInMS);
                    nerf = MathUtil.ReduceWithExponentialCurve(2, 0, 1, normalized);
                }
                else if (timeInMS > MaxNerfMS) // No movement
                {
                    removed++;
                    nerf = 0f;
                }

                if (cubes[i].Pattern && !cubes[i].Head) // Skip pattern that aren't head
                {
                    pattern += Pattern * nerf;
                    continue;
                }

                if (!(cubes[i - 1].Line == cubes[i].Line && cubes[i - 1].Layer == cubes[i].Layer)) // Don't check if there's no movement
                {
                    (multiplier, temp) = Helper.FindMovement(cubes[i - 1], cubes[i]);
                    inverted += temp;
                    multiplier += (temp * Inverted);
                }
                    
                multiplier *= nerf; // Nerf if necessary
                movement += multiplier; // Store value found
            }

            movement += pattern; // Add pattern notes as extra movement

            #endregion

            return movement / cubes.Count();
        }

        #endregion

    }
}
