﻿using static BeatmapScanner.Algorithm.BeatmapScanner;
using System;

namespace BeatmapScanner.Algorithm
{
    internal class MathUtil
    {
        public static float ReduceWithExponentialCurve(float currentValue, float lowerBound, float upperBound, float curve)
        {
            float mappedValue = (currentValue - lowerBound) / (upperBound - lowerBound);
            return lowerBound + (upperBound - lowerBound) * (float)Math.Pow(mappedValue, curve);
        }

        public static float NormalizeVariable(float variable)
        {
            float OldMax = MaxNerfMS;
            float OldMin = MinNerfMS;
            float NewMax = -NormalizedMax;
            float NewMin = NormalizedMin;
            float OldRange = (OldMax - OldMin);
            float NewRange = (NewMax - NewMin);
            float NewValue = (((variable - OldMin) * NewRange) / OldRange) + NewMin;
            return NewValue;
        }

        public static int ConvertBeatToMS(float beat, float bpm)
        {
            return (int)Math.Round(beat / bpm * 60 * 1000);
        }
    }
}
