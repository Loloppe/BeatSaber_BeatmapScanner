using static BeatmapSaveDataVersion3.BeatmapSaveData;

namespace BeatmapScanner.Algorithm.Loloppe
{
    internal class Cube
    {
        public ColorNoteData Note { get; set; }
        public float Beat { get; set; } = 0;
        public int Line { get; set; } = 0;
        public int Layer { get; set; } = 0;
        public double Direction { get; set; } = 8;
        public bool Assumed { get; set; } = false;
        public bool Reset { get; set; } = false;
        public bool SoftReset { get; set; } = false;
        public bool Bomb { get; set; } = false;
        public bool Head { get; set; } = false;
        public bool Pattern { get; set; } = false;
        public bool Slider { get; set; } = false;
        

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
