using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BeatmapSaveDataVersion3.BeatmapSaveData;
using static BeatmapScanner.Algorithm.Data;

// Source of code taken for this project:
// Tech https://github.com/LackWiz/ppCurve/
// Bezier https://github.com/shamim-akhtar/bezier-curve
// GetDistance https://github.com/tmokmss/Osu2Saber

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

                // Average of swing length as base with the Bezier curve.
                for(int i = 0; i < FullData.Count(); i++)
                {
                    point += (float)FullData[i].Length;
                }
                point /= FullData.Count();
                // Multiplied by NPS
                var nps = (notes.Count() / GetActiveSecond(notes, bpm));
                point *= nps;
                // Multiplied by X factor and rounded to two point decimal
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
                    beat += 0.5f;    
                }

                lastNote = note;
            }

            return MathUtil.ConvertBeat(beat, bpm) / 1000;
        }
    }
}
