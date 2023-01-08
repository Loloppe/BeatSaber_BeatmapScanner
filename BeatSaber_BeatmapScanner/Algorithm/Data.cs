using UnityEngine;

namespace BeatmapScanner.Algorithm
{
    internal class Data
    {
        internal class SwingData
        {
            public float Time { get; set; } = 0;
            public int Angle { get; set; } = 0;
            public float Frequency { get; set; } = 0;
            public bool Forehand { get; set; } = true;
            public double AngleStrain { get; set; } = 0;
            public double PathStrain { get; set; } = 0;
            public double CurveComplexity { get; set; } = 0;
            public double Length { get; set; } = 0;
            public bool Reset { get; set; } = false;
            public Vector2 EntryPosition { get; set; } = new Vector2();
            public Vector2 ExitPosition { get; set; } = new Vector2();

            public SwingData(float t, int a)
            {
                Time = t;
                Angle = a;
            }

            public SwingData(SwingData data)
            {
                Time = data.Time;
                Angle = data.Angle;
                Frequency = data.Frequency;
                Forehand = data.Forehand; ;
                AngleStrain = data.AngleStrain;
                PathStrain = data.PathStrain;
                CurveComplexity = data.CurveComplexity;
                Reset = data.Reset;
                EntryPosition = data.EntryPosition;
                ExitPosition = data.ExitPosition;
                Length = data.Length;
            }
        }
    }
}
