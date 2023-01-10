using System;
using System.Collections.Generic;
using System.Linq;
using static BeatmapSaveDataVersion3.BeatmapSaveData;
using static BeatmapScanner.Algorithm.Data;

// Source of code taken for this project:
// Tech https://github.com/LackWiz/ppCurve/
// Bezier https://github.com/shamim-akhtar/bezier-curve

namespace BeatmapScanner.Algorithm
{
    internal class BeatmapScanner
    {
        public static (float star, float tech, float intensity, float movement) Analyzer(List<ColorNoteData> notes, float bpm)
        {
            if (notes.Count > 2 && bpm > 0)
            {
                float point;
                var temp = 1f;
                var tech = 1f;
                var intensity = 1f;
                var movement = 1f;

                List<ColorNoteData> red = new();
                List<ColorNoteData> blue = new();

                foreach (var note in notes)
                {
                    if (note.color == NoteColorType.ColorA && (int)note.cutDirection != 9)
                    {
                        red.Add(note);
                    }
                    else if (note.color == NoteColorType.ColorB && (int)note.cutDirection != 9)
                    {
                        blue.Add(note);
                    }
                }

                var FullData = new List<SwingData>();
                if (red.Count() > 0)
                {
                    if (Config.Instance.Log)
                    {
                        Plugin.Log.Info("Left Data");
                    }
                    var LeftSwingData = Tech.ProcessSwing(red);
                    var LeftPatternData = Tech.SplitPattern(LeftSwingData);
                    LeftSwingData = Tech.PredictParity(LeftPatternData, true);
                    LeftSwingData = Tech.CalcSwingCurve(LeftSwingData, true);
                    FullData.AddRange(LeftSwingData);
                    tech = GetTech(red, bpm);
                    intensity = GetIntensity(red, bpm);
                    movement = GetMovement(red, bpm);
                }

                if (blue.Count() > 0)
                {
                    if (Config.Instance.Log)
                    {
                        Plugin.Log.Info("Right Data");
                    }
                    var RightSwingData = Tech.ProcessSwing(blue);
                    var RightPatternData = Tech.SplitPattern(RightSwingData);
                    RightSwingData = Tech.PredictParity(RightPatternData, false);
                    RightSwingData = Tech.CalcSwingCurve(RightSwingData, false);
                    FullData.AddRange(RightSwingData);
                    tech += GetTech(blue, bpm);
                    intensity += GetIntensity(notes, bpm);
                    movement += GetMovement(blue, bpm);
                }

                // Tech stuff
                var angleStain = 0f;

                for (int i = 0; i < FullData.Count(); i++)
                {
                    angleStain += (float)FullData[i].AngleStrain;
                }

                angleStain /= FullData.Count();

                if(Config.Instance.Log)
                {
                    Plugin.Log.Info("Angle Strain: " + angleStain);
                }

                tech /= 2; // Average of left and right hand
                tech--; // 0 should be the default, not 1.
                tech *= (1 + angleStain);
                if(tech < 0)
                {
                    tech = 0;
                }
                tech *= 2;
                Math.Round(tech, 2);

                // Average of swing length as base with the Bezier curve.
                for (int i = 0; i < FullData.Count(); i++)
                {
                    temp += (float)FullData[i].Length;
                }
                temp /= FullData.Count();
                Math.Round(temp, 2);

                // Intensity
                intensity /= (2 * 10);
                Math.Round(intensity, 2);

                // Movement
                movement /= 2;
                Math.Round(movement, 2);

                // Final calculation
                point = (temp * ((1 + tech) * Config.Instance.Tech) * ((1 + movement) * Config.Instance.Movement) * ((1 + intensity) * Config.Instance.Intensity));

                // Debug
                if(Config.Instance.Log)
                {
                    Plugin.Log.Info("Overall");
                    Plugin.Log.Info("Base: " + temp + " Tech: " + tech + " Intensity: " + intensity + " Movement: " + movement + " Final: " + point);
                }

                return ((float)Math.Round(point, 2), (float)Math.Round(tech, 2), (float)Math.Round(intensity, 2), (float)Math.Round(movement, 2));
            }
            else
            {
                return (-1f, -1f, -1f, -1f);
            }
        }

        public static int[] VerticalSwing = { 0, 1, 4, 5, 6, 7};
        public static int[] DiagonalSwing = { 4, 5, 6, 7 };

        public static float GetMovement(List<ColorNoteData> notes, float bpm)
        {
            var movement = 1f;
            var inverted = 5f;
            var previous = 1;
            int current;
            float temp;
            var count = 0;
            var count2 = 0;

            ColorNoteData lastNote = notes[0];

            foreach (var note in notes)
            {
                current = (int)note.cutDirection;
                temp = 0f;

                if (note.beat - lastNote.beat <= 0.15) // Skip pattern or stuff that are just too fast.
                {
                    count2++;
                    if(current != 8)
                    {
                        previous = current;
                    }
                    lastNote = note;
                    continue;
                }

                if ((int)note.cutDirection == 8)
                {
                    // Gotta simulate next cut if it's a dot
                    current = Helper.ReverseCutDirection(previous);
                }

                if (!(lastNote.line == note.line && lastNote.layer == note.layer)) // We don't want to check fused notes
                {
                    if (DiagonalSwing.Contains(previous))
                    {
                        if (previous == 4 || previous == 7) // Up-Left and Down-Right
                        {
                            if (lastNote.layer == note.layer - 1 && lastNote.line == note.line - 1) // Side temp
                            {
                                temp += Math.Abs(lastNote.line - note.line);
                            }
                            else if (lastNote.layer == note.layer + 1 && lastNote.line == note.line + 1) // Side temp
                            {
                                temp += Math.Abs(lastNote.line - note.line);
                            }
                            else if (lastNote.layer == note.layer - 2 && lastNote.line == note.line - 2) // Side temp
                            {
                                temp += Math.Abs(lastNote.line - note.line);
                            }
                            else if (lastNote.layer == note.layer + 2 && lastNote.line == note.line + 2) // Side temp
                            {
                                temp += Math.Abs(lastNote.line - note.line);
                            }

                            if (previous == 4 && lastNote.layer <= note.layer && lastNote.line >= note.line) // Inverted
                            {
                                temp += Math.Max(Math.Abs(lastNote.line - note.line), Math.Abs(lastNote.layer - note.layer)) * inverted;
                                count++;
                            }
                            else if (previous == 7 && lastNote.layer >= note.layer && lastNote.line <= note.line) // Inverted
                            {
                                temp += Math.Max(Math.Abs(lastNote.line - note.line), Math.Abs(lastNote.layer - note.layer)) * inverted;
                                count++;
                            }
                        }
                        else if (previous == 5 && previous == 6) // Up-Right and Down-Left
                        {
                            if (lastNote.layer == note.layer + 1 && lastNote.line == note.line - 1) // Side temp
                            {
                                temp += Math.Abs(lastNote.line - note.line);
                            }
                            else if (lastNote.layer == note.layer - 1 && lastNote.line == note.line + 1) // Side temp
                            {
                                temp += Math.Abs(lastNote.line - note.line);
                            }
                            else if (lastNote.layer == note.layer + 2 && lastNote.line == note.line - 2) // Side temp
                            {
                                temp += Math.Abs(lastNote.line - note.line);
                            }
                            else if (lastNote.layer == note.layer - 2 && lastNote.line == note.line + 2) // Side temp
                            {
                                temp += Math.Abs(lastNote.line - note.line);
                            }

                            if (previous == 5 && lastNote.layer <= note.layer && lastNote.line <= note.line) // Inverted
                            {
                                temp += Math.Max(Math.Abs(lastNote.line - note.line), Math.Abs(lastNote.layer - note.layer)) * inverted;
                                count++;
                            }
                            else if (previous == 6 && lastNote.layer >= note.layer && lastNote.line >= note.line) // Inverted
                            {
                                temp += Math.Max(Math.Abs(lastNote.line - note.line), Math.Abs(lastNote.layer - note.layer)) * inverted;
                                count++;
                            }
                        }
                    }
                    else if (VerticalSwing.Contains(previous))
                    {
                        if (lastNote.line != note.line) // Side temp
                        {
                            temp += Math.Abs(lastNote.line - note.line);
                        }

                        if (lastNote.layer < note.layer && previous == 0 && current == 1) // Inverted
                        {
                            temp += Math.Abs(lastNote.layer - note.layer) * inverted;
                            count++;
                        }
                        else if (lastNote.layer > note.layer && previous == 1 && current == 0) // Inverted
                        {
                            temp += Math.Abs(lastNote.layer - note.layer) * inverted;
                            count++;
                        }
                    }
                    else // Horizontal
                    {
                        if (lastNote.layer != note.layer)
                        {
                            temp += Math.Abs(lastNote.layer - note.layer);
                        }

                        if (lastNote.line > note.line && previous == 2 && current == 3) // Inverted
                        {
                            temp += Math.Abs(lastNote.line - note.line) * inverted;
                            count++;
                        }
                        else if (lastNote.line < note.line && previous == 3 && current == 2) // Inverted
                        {
                            temp += Math.Abs(lastNote.line - note.line) * inverted;
                            count++;
                        }
                    }
                }

                if (note.beat - lastNote.beat < 0.5 * (bpm / 100)) // Not sure if it's the right way to do it, maybe a static beat value would be better.
                {
                    movement += temp / (note.beat - lastNote.beat);
                }

                previous = current;
                lastNote = note;
            }

            // Log stuff
            if(Config.Instance.Log)
            {
                Plugin.Log.Info("Inverted: " + count);
            }
            
            return (float)Math.Round(movement / (notes.Count() - count2), 2);
        }

        public static float GetIntensity(List<ColorNoteData> notes, float bpm)
        {
            var count = 0;
            var intensity = 1f;
            ColorNoteData lastNote = notes[0];

            var speed = ((bpm * 1.75f) / 100); // Maybe

            foreach(var note in notes)
            {
                if (note.beat - lastNote.beat <= 0.15) // Skip pattern or stuff that are too fast
                {
                    lastNote = note;
                    count++;
                    continue;
                }

                intensity += speed / (note.beat - lastNote.beat); // Calculate intensity based on speed

                lastNote = note;
            }

            return (float)Math.Round(intensity / (notes.Count() - count), 2);
        }

        public static float GetTech(List<ColorNoteData> notes, float bpm)
        {
            var multiplier = 0f;
            bool assumed;
            var temp = 0f;
            int current;
            var previous = (int)notes[0].cutDirection;
            var linear = 1f;
            var semi = 1.25f;
            var tech = 5f;
            var dd = 6f;

            var count = 0;
            var count1 = 0;
            var count2 = 0;
            var count3 = 0;
            var count4 = 0;
            var count5 = 0;
            var count6 = 0;

            if (previous == 8) // Assume that the first note is a down
            {
                previous = 1;
            }

            for (int i = 1; i < notes.Count(); i++)
            {
                assumed = false;

                if (notes[i].beat - notes[i - 1].beat <= 0.15) // Skip pattern or stuff that are too fast
                {
                    count++;
                    continue;
                }

                current = (int)notes[i].cutDirection;

                if (current == 8) // Assume that next note is the reverse of the last one
                {
                    current = Helper.ReverseCutDirection(previous);
                    assumed = true;
                }

                switch (previous) // Try to get type of tech based on cut direction
                {
                    case 0: switch(current)
                        {
                            case 0:
                                temp = dd;
                                break;
                            case 1:
                                temp = linear;
                                break;
                            case 2: 
                                temp = tech;
                                break;
                            case 3: 
                                temp = tech;
                                break;
                            case 4: 
                                temp = dd;
                                break;
                            case 5:
                                temp = dd;
                                break;
                            case 6:
                                temp = semi;
                                break;
                            case 7:
                                temp = semi;
                                break;
                        }
                        break;
                    case 1:
                        switch (current)
                        {
                            case 0:
                                temp = linear;
                                break;
                            case 1:
                                temp = dd;
                                break;
                            case 2:
                                temp = tech;
                                break;
                            case 3:
                                temp = tech;
                                break;
                            case 4:
                                temp = semi;
                                break;
                            case 5:
                                temp = semi;
                                break;
                            case 6:
                                temp = dd;
                                break;
                            case 7:
                                temp = dd;
                                break;
                        }
                        break;
                    case 2:
                        switch (current)
                        {
                            case 0:
                                temp = tech;
                                break;
                            case 1:
                                temp = tech;
                                break;
                            case 2:
                                temp = dd;
                                break;
                            case 3:
                                temp = linear;
                                break;
                            case 4:
                                temp = dd;
                                break;
                            case 5:
                                temp = semi;
                                break;
                            case 6:
                                temp = dd;
                                break;
                            case 7:
                                temp = semi;
                                break;
                        }
                        break;
                    case 3:
                        switch (current)
                        {
                            case 0:
                                temp = tech;
                                break;
                            case 1:
                                temp = tech;
                                break;
                            case 2:
                                temp = linear;
                                break;
                            case 3:
                                temp = dd;
                                break;
                            case 4:
                                temp = semi;
                                break;
                            case 5:
                                temp = dd;
                                break;
                            case 6:
                                temp = semi;
                                break;
                            case 7:
                                temp = dd;
                                break;
                        }
                        break;
                    case 4:
                        switch (current)
                        {
                            case 0:
                                temp = dd;
                                break;
                            case 1:
                                temp = semi;
                                break;
                            case 2:
                                temp = dd;
                                break;
                            case 3:
                                temp = semi;
                                break;
                            case 4:
                                temp = dd;
                                break;
                            case 5:
                                temp = tech;
                                break;
                            case 6:
                                temp = tech;
                                break;
                            case 7:
                                temp = linear;
                                break;
                        }
                        break;
                    case 5:
                        switch (current)
                        {
                            case 0:
                                temp = dd;
                                break;
                            case 1:
                                temp = semi;
                                break;
                            case 2:
                                temp = semi;
                                break;
                            case 3:
                                temp = dd;
                                break;
                            case 4:
                                temp = tech;
                                break;
                            case 5:
                                temp = dd;
                                break;
                            case 6:
                                temp = linear;
                                break;
                            case 7:
                                temp = tech;
                                break;
                        }
                        break;
                    case 6:
                        switch (current)
                        {
                            case 0:
                                temp = semi;
                                break;
                            case 1:
                                temp = dd;
                                break;
                            case 2:
                                temp = dd;
                                break;
                            case 3:
                                temp = semi;
                                break;
                            case 4:
                                temp = tech;
                                break;
                            case 5:
                                temp = linear;
                                break;
                            case 6:
                                temp = dd;
                                break;
                            case 7:
                                temp = tech;
                                break;
                        }
                        break;
                    case 7:
                        switch (current)
                        {
                            case 0:
                                temp = semi;
                                break;
                            case 1:
                                temp = dd;
                                break;
                            case 2:
                                temp = semi;
                                break;
                            case 3:
                                temp = dd;
                                break;
                            case 4:
                                temp = linear;
                                break;
                            case 5:
                                temp = tech;
                                break;
                            case 6:
                                temp = tech;
                                break;
                            case 7:
                                temp = dd;
                                break;
                        }
                        break;
                }

                // Log stuff
                if (Config.Instance.Log)
                {
                    if (temp == linear)
                    {
                        count1++;
                    }
                    else if (temp == semi)
                    {
                        count2++;
                    }
                    else if (temp == tech)
                    {
                        count3++;
                    }
                    else if(temp == dd)
                    {
                        count4++;
                    }
                }

                if (temp >= tech && notes[i].beat - notes[i - 1].beat >= 0.4) // Nerf
                {
                    if(temp == dd) // DD
                    {
                        temp = linear;
                    }
                    else
                    {
                        temp -= (500 / bpm) * (notes[i].beat - notes[i - 1].beat);
                        if (temp < linear)
                        {
                            temp = linear;
                        }
                    }
                    
                    count5++;
                }

                if(temp >= tech && assumed) // Probably wrong flow from assuming any direction
                {
                    temp = semi;
                    count6++;
                }

                previous = current;

                multiplier += temp;
            }

            if(Config.Instance.Log)
            {
                Plugin.Log.Info("Linear: " + count1 + " Semi: " + count2 + " Tech: " + count3 + " DD: " + dd + " Nerf: " + count5 + " Assumed Dot: " + count6);
            }

            return (float)Math.Round(multiplier / (notes.Count() - count), 2);
        }
    }
}
