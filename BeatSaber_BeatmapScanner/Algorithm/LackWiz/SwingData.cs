using static BeatmapSaveDataVersion3.BeatmapSaveData;

namespace BeatmapScanner.Algorithm
{
    internal class SwingData
    {
        public double Time { get; set; } = 0;
        public double Angle { get; set; } = 0;
        public (double x, double y) EntryPosition { get; set; } = (0, 0);
        public (double x, double y) ExitPosition { get; set; } = (0, 0);
        public double SwingFrequency { get; set; } = 0;
        public bool Forehand { get; set; } = true;
        public bool Reset { get; set; } = false;
        public double PathStrain { get; set; } = 0;
        public double AngleStrain { get; set; } = 0;
        public double AnglePathStrain { get; set; } = 0;
        public double PositionComplexity { get; set; } = 0;
        public double CurveComplexity { get; set; } = 0;

        public SwingData()
        {
            
        }

        public SwingData(double beat, double angle)
        {
            Time = beat;
            Angle = angle;
        }
    }

    internal class SData
    {
        public double PreDistance { get; set; } = 0;
        public double AverageStress { get; set; } = 0;
        public double AverageSwingSpeed { get; set; } = 0;
        public double Difficulty { get; set; } = 0;

        public SData(double pre)
        {
            PreDistance = pre;
        }
    }
}
