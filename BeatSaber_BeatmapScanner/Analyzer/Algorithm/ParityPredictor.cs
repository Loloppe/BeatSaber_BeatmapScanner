using Analyzer.BeatmapScanner.Data;
using System.Collections.Generic;
using System.Linq;
using static Analyzer.BeatmapScanner.Helper.IsSameDirection;
using static Analyzer.BeatmapScanner.Helper.SwingAngleStrain;

namespace Analyzer.BeatmapScanner.Algorithm
{
    internal class ParityPredictor
    {
        public static List<SwingData> Predict(List<List<SwingData>> patternData, bool leftOrRight)
        {
            if (patternData.Count < 1)
            {
                return null;
            }

            var newPatternData = new List<SwingData>();

            for (int p = 0; p < patternData.Count; p++)
            {
                var testData1 = patternData[p];
                var testData2 = SwingData.DeepCopy(patternData[p]);

                for (int i = 0; i < testData1.Count; i++)
                {
                    if (i > 0)
                    {
                        if (IsSameDir(testData1[i - 1].Angle, testData1[i].Angle))
                        {
                            testData1[i].Reset = true;
                            testData1[i].Forehand = testData1[i - 1].Forehand;
                        }
                        else
                        {
                            testData1[i].Reset = false;
                            testData1[i].Forehand = !testData1[i - 1].Forehand;
                        }
                    }
                    else
                    {
                        testData1[0].Reset = false;
                        testData1[0].Forehand = true;
                    }
                }
                for (int i = 0; i < testData2.Count; i++)
                {
                    if (i > 0)
                    {
                        if (IsSameDir(testData2[i - 1].Angle, testData2[i].Angle))
                        {
                            testData2[i].Reset = true;
                            testData2[i].Forehand = testData2[i - 1].Forehand;
                        }
                        else
                        {
                            testData2[i].Reset = false;
                            testData2[i].Forehand = !testData2[i - 1].Forehand;
                        }
                    }
                    else
                    {
                        testData2[0].Reset = false;
                        testData2[0].Forehand = false;
                    }
                }

                var forehandTest = SwingAngleStrainCalc(testData1, leftOrRight);
                var backhandTest = SwingAngleStrainCalc(testData2, leftOrRight);
                if (forehandTest <= backhandTest)
                {
                    newPatternData.AddRange(testData1);
                }
                else
                {
                    newPatternData.AddRange(testData2);
                }
            }
            for (int i = 0; i < newPatternData.Count; i++)
            {
                newPatternData[i].AngleStrain = SwingAngleStrainCalc(new List<SwingData> { newPatternData[i] }, leftOrRight) * 2;
            }

            return newPatternData;
        }
    }
}
