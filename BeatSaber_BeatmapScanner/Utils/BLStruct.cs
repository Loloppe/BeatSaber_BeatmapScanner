namespace BeatmapScanner.Utils
{
    internal class BLStruct
    {
        public string? difficultyName { get; set; }
        public string? modeName { get; set; }
        public float? stars { get; set; }
        public int status { get; set; }
        public int? type { get; set; }
        public float[]? votes { get; set; }
        public ModifiersMap modifierValues { get; set; }
    }

    internal class ModifiersMap
    {
        public int modifierId { get; set; }
        public float da { get; set; }
        public float fs { get; set; }
        public float ss { get; set; }
        public float sf { get; set; }
        public float gn { get; set; }
        public float na { get; set; }
        public float nb { get; set; }
        public float nf { get; set; }
        public float no { get; set; }
        public float pm { get; set; }
        public float sc { get; set; }
        public float sa { get; set; }
    }
}
