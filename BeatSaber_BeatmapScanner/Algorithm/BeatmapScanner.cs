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
        public static (float star, float tech, float intensity) Analyzer(List<ColorNoteData> notes, float bpm)
        {
            if (notes.Count > 2 && bpm > 0)
            {
                float point;
                var temp = 1f;
                var tech = 1f;
                var intensity = 1f;
                var inverted = 1f;

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
                    (tech, inverted) = GetTech(red);
                    intensity = GetIntensity(red, bpm);
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
                    float t;
                    float t2;
                    (t, t2) = GetTech(blue);
                    tech = (tech + t) / 2;
                    inverted = (inverted + t2) / 2;
                    intensity = GetIntensity(notes, bpm);
                }

                // Tech stuff
                var angleStain = 0f;

                for (int i = 0; i < FullData.Count(); i++)
                {
                    angleStain += (float)FullData[i].AngleStrain;
                }

                angleStain /= FullData.Count();

                tech *= (angleStain * 2f);

                tech += inverted;

                Math.Round(tech, 2);

                // Average of swing length as base with the Bezier curve.
                for (int i = 0; i < FullData.Count(); i++)
                {
                    temp += (float)FullData[i].Length;
                }
                temp /= FullData.Count();

                Math.Round(temp, 2);

                intensity /= 10;
                Math.Round(intensity, 2);

                point = (temp * (1 + tech) * (1 + (intensity * 5)));

                // Debug
                if(Config.Instance.Log)
                {
                    Plugin.Log.Info("Overall");
                    Plugin.Log.Info("Base: " + temp + " Tech: " + tech + " Intensity: " + intensity + " Inverted: " + inverted + " Final: " + point);
                }

                return ((float)Math.Round(point, 2), (float)Math.Round(tech, 2), (float)Math.Round(intensity, 2));
            }
            else
            {
                return (-1f, -1f, -1f);
            }
        }

        public static float GetIntensity(List<ColorNoteData> notes, float bpm)
        {
            var count = 0;
            var intensity = 1f;
            ColorNoteData lastNote = notes[0];

            var speed = ((bpm * 1.75f) / 100);

            foreach(var note in notes)
            {
                if (note.beat - lastNote.beat <= 0.15)
                {
                    lastNote = note;
                    count++;
                    continue;
                }

                intensity += speed / (note.beat - lastNote.beat);

                lastNote = note;
            }

            return (float)Math.Round(intensity / (notes.Count() - count), 2);
        }

        public static (float, float) GetTech(List<ColorNoteData> notes)
        {
            var multiplier = 0f;
            bool assumed;
            var temp = 0f;
            int current;
            var previous = (int)notes[0].cutDirection;
            var linear = 1f;
            var semi = 1.25f;
            var tech = 4f;
            var semidd = 4.5f;
            var dd = 5f;
            var inverted = 0f;

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

                if (notes[i].beat - notes[i - 1].beat <= 0.15)
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

                switch (previous)
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
                                temp = semidd;
                                break;
                            case 5:
                                temp = semidd;
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
                                temp = semidd;
                                break;
                            case 7:
                                temp = semidd;
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
                                temp = semidd;
                                break;
                            case 5:
                                temp = semi;
                                break;
                            case 6:
                                temp = semidd;
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
                                temp = semidd;
                                break;
                            case 6:
                                temp = semi;
                                break;
                            case 7:
                                temp = semidd;
                                break;
                        }
                        break;
                    case 4:
                        switch (current)
                        {
                            case 0:
                                temp = semidd;
                                break;
                            case 1:
                                temp = semi;
                                break;
                            case 2:
                                temp = semidd;
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
                                temp = semidd;
                                break;
                            case 1:
                                temp = semi;
                                break;
                            case 2:
                                temp = semi;
                                break;
                            case 3:
                                temp = semidd;
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
                                temp = semidd;
                                break;
                            case 2:
                                temp = semidd;
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
                                temp = semidd;
                                break;
                            case 2:
                                temp = semi;
                                break;
                            case 3:
                                temp = semidd;
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
                if(Config.Instance.Log)
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
                    else if(temp == semidd)
                    {
                        count4++;
                    }
                    else if(temp == dd)
                    {
                        count5++;
                    }
                }

                var type = 0;

                if (temp >= tech)
                {
                    type = 2;
                }

                if (Helper.DetectInverted(notes[i], notes[i - 1], type))
                {
                    inverted += 3f;
                    count6++; 
                }

                if (temp >= tech && notes[i].beat - notes[i - 1].beat >= 1) // Possibly bomb reset or just slow tech
                {
                    temp = tech;
                }
                else if (temp >= tech && notes[i].beat - notes[i - 1].beat >= 0.5)
                {
                    temp = tech;
                }

                if(temp >= tech && assumed) // Probably wrong flow from assuming any direction
                {
                    temp = semi;
                }

                previous = current;

                multiplier += temp;
            }

            if(Config.Instance.Log)
            {
                Plugin.Log.Info("Linear: " + count1 + " Semi: " + count2 + " Tech: " + count3 + " SemiDD: " + count4 + " DD: " + count5 + " Inverted: " + count6);
            }

            return ((float)Math.Round(multiplier / (notes.Count() - count), 2), (float)Math.Round(inverted / (notes.Count() - count), 2));
        }
    }
}
