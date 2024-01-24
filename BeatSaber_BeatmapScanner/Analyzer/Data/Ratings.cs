using System.Collections.Generic;

namespace beatleader_analyzer.BeatmapScanner.Data
{
    public class Ratings
    {
        public string Characteristic { get; set; }
        public string Difficulty { get; set; }
        public double Pass { get; set; }
        public double Tech { get; set; }
        public double Nerf { get; set; }
        public double Linear { get; set; }
        public double Multi { get; set; }

        public Ratings(string characteristic, string difficulty, List<double> ratings)
        {
            Characteristic = characteristic;
            Difficulty = difficulty;
            Pass = ratings[0];
            Tech = ratings[1];
            Nerf = ratings[2];
            Linear = ratings[3];
            Multi = ratings[4];
        }
    }
}
