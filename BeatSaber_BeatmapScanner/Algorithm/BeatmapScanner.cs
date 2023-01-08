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
        public static float Analyzer(List<ColorNoteData> notes, float bpm)
        {
            var point = 0f;

            if (notes.Count > 0 && bpm > 0)
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
                }

                if (blue.Count() > 0)
                {
                    var RightSwingData = Tech.ProcessSwing(blue);
                    var RightPatternData = Tech.SplitPattern(RightSwingData);
                    RightSwingData = Tech.PredictParity(RightPatternData, false);
                    RightSwingData = Tech.CalcSwingCurve(RightSwingData, false);
                    FullData.AddRange(RightSwingData);
                }

                FullData = FullData.OrderBy(o => o.Time).ToList();

                float tech = 0f;

                for (int i = FullData.Count() - (int)(FullData.Count() * 0.3); i < FullData.Count(); i++)
                {
                    tech += (float)FullData[i].AngleStrain;
                }

                tech /= FullData.Count() - (int)(FullData.Count() * 0.2);

                // Average of swing length as base with the Bezier curve.
                for (int i = 0; i < FullData.Count(); i++)
                {
                    point += (float)FullData[i].Length;
                }
                // Add tech
                point /= FullData.Count();
                var nps = (notes.Count() / GetActiveSecond(notes, bpm));
                point += (tech * 10);
                // Multiplied by NPS
                if(nps > 1)
                {
                    if(nps >= 2)
                    {
                        nps -= 1;
                    }
                    point *= nps * 0.8f;
                }
                
                // Rounded to two point decimal
                point = (float)Math.Round(point, 2);
            }

            return point;
        }

        public static float GetActiveSecond(List<ColorNoteData> notes, float bpm)
        {
            var beat = 0f;
            ColorNoteData lastNote = notes[0];

            foreach(var note in notes)
            {
                // Only calculate if there's more than one note every four beats
                if(note.beat - lastNote.beat <= 4)
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
    }
}
