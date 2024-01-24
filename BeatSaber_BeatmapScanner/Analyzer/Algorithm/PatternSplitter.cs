using Analyzer.BeatmapScanner.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.BeatmapScanner.Algorithm
{
    internal class PatternSplitter
    {
        public static List<List<SwingData>> Split(List<SwingData> swingData)
        {
            if (swingData.Count < 2)
            {
                return null;
            }

            for (int i = 0; i < swingData.Count; i++)
            {
                if (i > 0 && i + 1 < swingData.Count)
                {
                    swingData[i].SwingFrequency = 2 / (swingData[i + 1].Time - swingData[i - 1].Time);
                }
                else
                {
                    swingData[i].SwingFrequency = 0;
                }
            }

            var patternFound = false;
            var SFList = swingData.Select(s => s.SwingFrequency);
            var SFMargin = SFList.Average() / 32;
            List<List<SwingData>> patternList = new();
            List<SwingData> tempPList = new();

            for (int i = 0; i < swingData.Count; i++)
            {
                if (i > 0)
                {
                    if (Math.Abs(1 / (swingData[i].Time - swingData[i - 1].Time) - swingData[i].SwingFrequency) <= SFMargin)
                    {
                        if (!patternFound)
                        {
                            patternFound = true;
                            tempPList.Remove(tempPList.Last());
                            if (tempPList.Count > 0)
                            {
                                patternList.Add(tempPList);
                            }
                            tempPList = new List<SwingData>()
                            {
                                swingData[i - 1]
                            };
                        }
                        tempPList.Add(swingData[i]);
                    }
                    else
                    {
                        if (tempPList.Count > 0 && patternFound)
                        {
                            tempPList.Add(swingData[i]);
                            patternList.Add(tempPList);
                            tempPList = new List<SwingData>();
                        }
                        else
                        {
                            patternFound = false;
                            tempPList.Add(swingData[i]);
                        }
                    }
                }
                else
                {
                    tempPList.Add(swingData[0]);
                }
            }

            if (tempPList.Count > 0 && patternList.Count == 0)
            {
                patternList.Add(tempPList);
            }

            return patternList;
        }
    }
}
