using Analyzer.BeatmapScanner.Data;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Analyzer.BeatmapScanner.Algorithm
{
    internal class Analyze
    {
        public static List<double> UseLackWizAlgorithm(List<Cube> red, List<Cube> blue, float bpm, float njs)
        {
            double leftDiff = 0;
            double rightDiff = 0;
            double tech = 0;
            List<double> value = new();
            List<SwingData> redSwingData = new();
            List<SwingData> blueSwingData = new();
            List<List<SwingData>> redPatternData = new();
            List<List<SwingData>> bluePatternData = new();
            List<SwingData> data = new();

            if (red.Count() > 2)
            {
                FlowDetector.Detect(red, bpm, njs, false);
                redSwingData = SwingProcesser.Process(red);
                if (redSwingData != null)
                {
                    redPatternData = PatternSplitter.Split(redSwingData);
                }
                if (redSwingData != null && redPatternData != null)
                {
                    redSwingData = ParityPredictor.Predict(redPatternData, false);
                }
                if (redSwingData != null)
                {
                    SwingCurve.Calc(redSwingData, false);
                    Linear.CalculateLinear(redSwingData);
                }
                if (redSwingData != null)
                {
                    data.AddRange(redSwingData);
                }
            }

            if (blue.Count() > 2)
            {
                FlowDetector.Detect(blue, bpm, njs, true);
                blueSwingData = SwingProcesser.Process(blue);
                if (blueSwingData != null)
                {
                    bluePatternData = PatternSplitter.Split(blueSwingData);
                }
                if (blueSwingData != null && bluePatternData != null)
                {
                    blueSwingData = ParityPredictor.Predict(bluePatternData, true);
                }
                if (blueSwingData != null)
                {
                    SwingCurve.Calc(blueSwingData, true);
                    Linear.CalculateLinear(blueSwingData);
                }
                if (blueSwingData != null)
                {
                    data.AddRange(blueSwingData);
                }
            }

            if (redSwingData != null)
            {
                redSwingData = DiffToPass.CalcSwingDiff(redSwingData, bpm);
                leftDiff = DiffToPass.CalcAverage(redSwingData, 8);
                leftDiff += DiffToPass.CalcAverage(redSwingData, 16);
                leftDiff += DiffToPass.CalcAverage(redSwingData, 32);
                leftDiff += DiffToPass.CalcAverage(redSwingData, 48);
                leftDiff += DiffToPass.CalcAverage(redSwingData, 96);
                leftDiff /= 5;
            }
            if (blueSwingData != null)
            {
                blueSwingData = DiffToPass.CalcSwingDiff(blueSwingData, bpm);
                rightDiff = DiffToPass.CalcAverage(blueSwingData, 8);
                rightDiff += DiffToPass.CalcAverage(blueSwingData, 16);
                rightDiff += DiffToPass.CalcAverage(blueSwingData, 32);
                rightDiff += DiffToPass.CalcAverage(blueSwingData, 48);
                rightDiff += DiffToPass.CalcAverage(blueSwingData, 96);
                rightDiff /= 5;
            }

            if (data.Count() > 2)
            {
                var test = data.Select(c => c.AngleStrain + c.PathStrain).ToList();
                test.Sort();
                tech = test.Skip((int)(data.Count() * 0.25)).Average();
            }

            double balanced_pass = Math.Max(leftDiff, rightDiff) * 0.8 + Math.Min(leftDiff, rightDiff) * 0.2;

            value.Add(balanced_pass);
            double balanced_tech = tech * (-(Math.Pow(Math.Abs(-1.4), -balanced_pass)) + 1);
            value.Add(balanced_tech);
            double low_note_nerf = 1 / (1 + Math.Pow(Math.E, -0.6 * (data.Count() / 100 + 1.5)));
            value.Add(low_note_nerf);

            if (data.Count() > 2)
            {
                double linear = data.Where(x => x.Linear == true).Count() / (double)data.Count();
                value.Add(linear);
                double pattern = data.Select(x => x.Pattern).Average();
                value.Add(pattern);
            }
            else
            {
                value.Add(0);
                value.Add(0);
            }

            return value;
        }
    }
}
