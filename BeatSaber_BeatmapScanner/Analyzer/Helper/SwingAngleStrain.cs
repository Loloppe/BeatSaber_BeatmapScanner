using Analyzer.BeatmapScanner.Data;
using System;
using System.Collections.Generic;

namespace Analyzer.BeatmapScanner.Helper
{
    internal class SwingAngleStrain
    {
        public static double SwingAngleStrainCalc(List<SwingData> swingData, bool leftOrRight)
        {
            double strainAmount = 0;

            for (int i = 0; i < swingData.Count; i++)
            {
                if (swingData[i].Forehand)
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }

        public static double BezierAngleStrainCalc(List<double> angleData, bool forehand, bool leftOrRight)
        {
            var strainAmount = 0d;

            for (int i = 0; i < angleData.Count; i++)
            {
                if (forehand)
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - angleData[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - angleData[i]) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - angleData[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - angleData[i]) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }
    }
}
