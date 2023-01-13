using UnityEngine;
using static BeatmapSaveDataVersion3.BeatmapSaveData;

namespace BeatmapScanner.Algorithm.Loloppe
{
    internal class Cube
    {
        public ColorNoteData Note { get; set; }
        public float Beat { get; set; } = 0;
        public int Line { get; set; } = 0;
        public int Layer { get; set; } = 0;
        public int Direction { get; set; } = 8;
        public bool Assumed { get; set; } = false;
        public bool Reset { get; set; } = false;
        public bool SoftReset { get; set; } = false;
        public bool Bomb { get; set; } = false;
        public bool Forehand { get; set; } = true;
        public bool PalmUp { get; set; } = false;
        public bool Head { get; set; } = false;
        public bool Pattern { get; set; } = false;
        public float Angle { get; set; } = 0;
        public Vector2 EntryPosition { get; set; } = new Vector2();
        public Vector2 ExitPosition { get; set; } = new Vector2();
        public float PathAngleStrain { get; set; } = 0;
        public float CurveComplexity { get; set; } = 0;
        

        public Cube(ColorNoteData note)
        {
            Note = note;
            Beat = note.beat;
            Line = note.line;
            Layer = note.layer;
            Direction = (int)note.cutDirection;
            if(Direction == 8)
            {
                Assumed = true;
            }
        }
    }
}
