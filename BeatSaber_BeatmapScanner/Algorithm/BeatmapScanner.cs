using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BeatmapSaveDataVersion3.BeatmapSaveData;
using static BeatmapScanner.Algorithm.Data;

// Source of code taken for this project:
// Tech https://github.com/LackWiz/ppCurve/
// Bezier https://github.com/shamim-akhtar/bezier-curve

namespace BeatmapScanner.Algorithm
{
    internal class BeatmapScanner
    {
        public static (float star, float tech) Analyzer(List<ColorNoteData> notes, float bpm)
        {
            var point = 1f;
            var techList = new List<float>();
            var tech = 1f;
            var temp = 1f;
            var nps = 1f;

            if (notes.Count > 2 && bpm > 0)
            {
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
                    var LeftSwingData = Tech.ProcessSwing(red);
                    var LeftPatternData = Tech.SplitPattern(LeftSwingData);
                    LeftSwingData = Tech.PredictParity(LeftPatternData, true);
                    LeftSwingData = Tech.CalcSwingCurve(LeftSwingData, true);
                    FullData.AddRange(LeftSwingData);
                    techList = GetTechMultiplier(red);
                }

                if (blue.Count() > 0)
                {
                    var RightSwingData = Tech.ProcessSwing(blue);
                    var RightPatternData = Tech.SplitPattern(RightSwingData);
                    RightSwingData = Tech.PredictParity(RightPatternData, false);
                    RightSwingData = Tech.CalcSwingCurve(RightSwingData, false);
                    FullData.AddRange(RightSwingData);
                    techList.AddRange(GetTechMultiplier(blue));
                }

                techList = techList.OrderBy(o => o).ToList();
                FullData = FullData.OrderBy(o => o.Time).ToList();

                // Tech stuff
                var angleStain = 0f;

                for (int i = 0; i < FullData.Count(); i++)
                {
                    angleStain += (float)FullData[i].AngleStrain;
                }

                angleStain /= FullData.Count();

                for (int i = 0; i < techList.Count(); i++)
                {
                    tech += techList[i];
                }

                tech /= techList.Count();

                tech *= (angleStain * 2f);

                tech++;

                // Average of swing length as base with the Bezier curve.
                for (int i = 0; i < FullData.Count(); i++)
                {
                    temp += (float)FullData[i].Length;
                }
                temp /= FullData.Count();

                // Multiplied by Tech
                point = temp * tech;

                // Multiplied by NPS
                nps = (notes.Count() / GetActiveSecond(notes, bpm)) * 0.75f;
                point *= nps;
                
                // Rounded to two point decimal
                point = (float)Math.Round(point, 2);
            }

            return (point, (float)Math.Round(tech, 2));
        }

        public static float GetActiveSecond(List<ColorNoteData> notes, float bpm)
        {
            var beat = 0f;
            ColorNoteData lastNote = notes[0];

            foreach(var note in notes)
            {
                if (note.beat - lastNote.beat < 0.125)
                {
                    lastNote = note;
                    continue;
                }

                // Only calculate if there's more than one note every four beats
                if (note.beat - lastNote.beat <= 4)
                {
                    beat += note.beat - lastNote.beat;
                }
                else 
                {
                    beat += 0.25f;    
                }

                lastNote = note;
            }

            return MathUtil.ConvertBeat(beat, bpm) / 1000;
        }

        public static List<float> GetTechMultiplier(List<ColorNoteData> notes)
        {
            List<float> multiplier = new();
            var assumed = false;
            var temp = 0f;
            int current;
            var previous = (int)notes[0].cutDirection;
            var linear = 1f;
            var semi = 1.25f;
            var tech = 4f;
            var semidd = 5f;
            var dd = 6f;

            if (previous == 8) // Assume that the first note is a down
            {
                previous = 1;
            }

            for (int i = 1; i < notes.Count(); i++)
            {
                if (notes[i].beat - notes[i - 1].beat < 0.125)
                {
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

                if(temp > tech && notes[i].beat - notes[i - 1].beat >= 0.5) // Possibly bomb reset
                {
                    temp = tech;
                }

                if(temp > tech && assumed) // Probably wrong flow from assuming any direction
                {
                    temp = tech;
                }

                var type = 0;

                if(temp >= tech)
                {
                    type = 2;
                }

                if(Helper.DetectInverted(notes[i], notes[i - 1], type))
                {
                    temp += tech;
                }

                previous = current;

                multiplier.Add(temp);
            }

            return multiplier;
        }
    }
}
