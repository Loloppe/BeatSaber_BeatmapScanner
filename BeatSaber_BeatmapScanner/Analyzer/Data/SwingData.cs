using System.Collections.Generic;

namespace Analyzer.BeatmapScanner.Data
{
    public class SwingData
    {
        public Cube Start { get; set; } = null;
        public double Time { get; set; } = 0;
        public double Angle { get; set; } = 0;
        public (double x, double y) EntryPosition { get; set; } = (0, 0);
        public (double x, double y) ExitPosition { get; set; } = (0, 0);
        public double SwingFrequency { get; set; } = 0;
        public double SwingDiff { get; set; } = 0;
        public bool Forehand { get; set; } = true;
        public bool Reset { get; set; } = false;
        public bool Linear { get; set; } = false;
        public double Pattern { get; set; } = 0;
        public double PathStrain { get; set; } = 0;
        public double AngleStrain { get; set; } = 0;
        public double AnglePathStrain { get; set; } = 0;
        public double PreviousDistance { get; set; } = 0;
        public double PositionComplexity { get; set; } = 0;
        public double CurveComplexity { get; set; } = 0;

        public SwingData()
        {

        }

        public SwingData(double beat, double angle, Cube start)
        {
            Time = beat;
            Angle = angle;
            Start = start;
        }

        public static List<SwingData> DeepCopy(List<SwingData> data)
        {
            List<SwingData> cloneList = new();
            foreach (var d in data)
            {
                SwingData clone = new()
                {
                    EntryPosition = d.EntryPosition,
                    ExitPosition = d.ExitPosition,
                    Time = d.Time,
                    Angle = d.Angle,
                    SwingFrequency = d.SwingFrequency,
                    SwingDiff = d.SwingDiff,
                    Forehand = d.Forehand,
                    Reset = d.Reset,
                    PathStrain = d.PathStrain,
                    AngleStrain = d.AngleStrain,
                    AnglePathStrain = d.AnglePathStrain,
                    PreviousDistance = d.PreviousDistance,
                    PositionComplexity = d.PositionComplexity,
                    CurveComplexity = d.CurveComplexity,
                    Start = d.Start
                };

                cloneList.Add(clone);
            }
            return cloneList;
        }
    }

    internal class SData
    {
        public double HitDistance { get; set; } = 0;
        public double HitDiff { get; set; } = 0;
        public double Stress { get; set; } = 0;
        public double SwingSpeed { get; set; } = 0;

        public SData(double ss)
        {
            SwingSpeed = ss;
        }
    }
    internal class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
