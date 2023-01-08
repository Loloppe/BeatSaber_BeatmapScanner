using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BeatmapSaveDataVersion3.BeatmapSaveData;
using static BeatmapScanner.Algorithm.Data;

namespace BeatmapScanner.Algorithm
{
    internal class Tech
    {
        public static int[] CutDirectionIndex = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };

        public static List<SwingData> ProcessSwing(List<ColorNoteData> notes)
        {
            List<SwingData> data = new();
            try
            {
                for (int i = 0; i < notes.Count(); i++)
                {
                    // Slider variable
                    var slider = false;
                    var sliderDuration = 0f;
                    // Current note variable
                    var beat = notes[i].beat;
                    var angle = CutDirectionIndex[(int)notes[i].cutDirection] + notes[i].angleOffset;
                    var position = (notes[i].line, notes[i].layer);
                    if (i > 0)
                    {
                        // Previous note variable
                        var previousBeat = notes[i - 1].beat;
                        var previousAngle = data.Last().Angle;
                        var previousPosition = (notes[i - 1].line, notes[i - 1].layer);
                        // If it's any direction, we assume it's the opposite direction. The angle must stay positive.
                        if ((int)notes[i].cutDirection == 8)
                        {
                            previousAngle = data.Last().Angle;
                            if (beat - previousBeat <= 0.03125)
                            {
                                angle = previousAngle;
                            }
                            else
                            {
                                if (previousAngle >= 180)
                                {
                                    angle = previousAngle - 180;
                                }
                                else
                                {
                                    angle = previousAngle + 180;
                                }
                            }
                        }
                        // Check for pattern (sliders, etc)
                        if (beat - previousBeat >= 0.03125)
                        {
                            if (beat - previousBeat > 0.125)
                            {
                                if (beat - previousBeat > 0.5)
                                {
                                    data.Add(new SwingData(beat, angle));
                                    (data.Last().EntryPosition, data.Last().ExitPosition) = MathUtil.CalculateBaseEntryExit(position, angle);
                                }
                                else // 1/2
                                {
                                    if (Math.Abs(angle - previousAngle) < 112.5)
                                    {
                                        var testAngle = MathUtil.ConvertRadiansToDegrees(Math.Atan2(previousPosition.layer - position.layer, previousPosition.line - position.line)) % 360;
                                        var averageAngle = (angle + previousAngle) / 2;
                                        if (Math.Abs(testAngle - averageAngle) <= 56.25) // Probably a slider
                                        {
                                            sliderDuration = beat - previousBeat;
                                            slider = true;
                                        }
                                        else
                                        {
                                            data.Add(new SwingData(beat, angle));
                                            (data.Last().EntryPosition, data.Last().ExitPosition) = MathUtil.CalculateBaseEntryExit(position, angle);
                                        }
                                    }
                                    else
                                    {
                                        data.Add(new SwingData(beat, angle));
                                        (data.Last().EntryPosition, data.Last().ExitPosition) = MathUtil.CalculateBaseEntryExit(position, angle);
                                    }
                                }
                            }
                            else // 1/8
                            {
                                if ((int)notes[i].cutDirection == 8 || Math.Abs(angle - previousAngle) < 90) // Probably a slider
                                {
                                    sliderDuration = 0.125f;
                                    slider = true;
                                }
                                else
                                {
                                    data.Add(new SwingData(beat, angle));
                                    (data.Last().EntryPosition, data.Last().ExitPosition) = MathUtil.CalculateBaseEntryExit(position, angle);
                                }
                            }
                        }
                        else // 1/32
                        {
                            sliderDuration = 0.03125f;
                            slider = true;
                        }

                        if (slider)
                        {
                            for (int j = 1; j < notes.Count; j++)
                            {
                                var index = i - j;
                                if (index < 1)
                                {
                                    break;
                                }
                                if (notes[index].beat - notes[index - 1].beat > 2 * sliderDuration) // First note of the slider
                                {
                                    previousBeat = notes[index].beat;
                                    //previousAngle = CutDirectionIndex[(int)notes[index].cutDirection] + notes[index].angleOffset;
                                    previousPosition = (notes[index].line, notes[index].layer);
                                    break;
                                }
                            }

                            angle = (int)MathUtil.ConvertRadiansToDegrees(Math.Atan2(previousPosition.layer - position.layer, previousPosition.line - position.line)) % 360;
                            var guideAngle = -1;
                            for (int j = 1; j < notes.Count; j++)
                            {
                                var index = i - j;
                                if (index < 1)
                                {
                                    break;
                                }
                                if (notes[index].beat < previousBeat)
                                {
                                    break;
                                }
                                if ((int)notes[index].cutDirection != 8)
                                {
                                    guideAngle = CutDirectionIndex[(int)notes[index].cutDirection];
                                    break;
                                }
                            }
                            if (guideAngle != -1)
                            {
                                if (Math.Abs(angle - guideAngle) > 90)
                                {
                                    if (angle >= 180)
                                    {
                                        angle -= 180;
                                    }
                                    else
                                    {
                                        angle += 180;
                                    }
                                }
                            }
                            data.Last().Angle = angle;

                            var x = (data.Last().EntryPosition.x - (position.line * 0.333333 - Math.Cos(MathUtil.ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667)) * Math.Cos(MathUtil.ConvertDegreesToRadians(angle));
                            var y = (data.Last().EntryPosition.y - (position.layer * 0.333333 - Math.Sin(MathUtil.ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667)) * Math.Sin(MathUtil.ConvertDegreesToRadians(angle));
                            if (x <= 0.001 && y >= 0.001) // Replace either the entry point or exit point for the slider
                            {
                                data.Last().EntryPosition = new Vector2((float)(position.line * 0.333333 - Math.Cos(MathUtil.ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667), (float)(position.layer * 0.333333 - Math.Sin(MathUtil.ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667));
                            }
                            else
                            {
                                data.Last().ExitPosition = new Vector2((float)(position.line * 0.333333 + Math.Cos(MathUtil.ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667), (float)(position.layer * 0.333333 + Math.Sin(MathUtil.ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667));
                            }
                        }
                    }
                    else // First note
                    {
                        data.Add(new SwingData(beat, angle));
                        (data.Last().EntryPosition, data.Last().ExitPosition) = MathUtil.CalculateBaseEntryExit(position, angle);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }

            return data;
        }

        public static List<List<SwingData>> SplitPattern(List<SwingData> data)
        {
            List<List<SwingData>> patternList = new();

            try
            {
                for (int i = 0; i < data.Count(); i++)
                {
                    if (i > 0 && i + 1 < data.Count())
                    {
                        data[i].Frequency = 2 / (data[i + 1].Time - data[i - 1].Time);
                    }
                    else
                    {
                        data[i].Frequency = 0f;
                    }
                }

                var pattern = false;
                List<float> frequencyList = new();
                foreach (var d in data)
                {
                    frequencyList.Add(d.Frequency);
                }
                var fAverage = frequencyList.Average() / 32;

                List<SwingData> tempList = new();

                for (int i = 0; i < data.Count(); i++)
                {
                    if (i > 0)
                    {
                        if (1 / (data[i].Time - data[i - 1].Time) - data[i].Frequency <= fAverage)
                        {
                            if (!pattern)
                            {
                                pattern = true;
                                tempList.Remove(tempList.Last());
                                if (tempList.Count() > 0)
                                {
                                    patternList.Add(new List<SwingData>(tempList));
                                }
                                tempList.Clear();
                                tempList.Add(data[i - 1]);
                            }
                            tempList.Add(data[i]);
                        }
                        else
                        {
                            if (tempList.Count() > 0 && pattern)
                            {
                                tempList.Add(data[i]);
                                patternList.Add(new List<SwingData>(tempList));
                                tempList.Clear();
                            }
                            else
                            {
                                pattern = false;
                                tempList.Add(data[i]);
                            }
                        }
                    }
                    else
                    {
                        tempList.Add(data[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }

            return patternList;
        }

        public static List<SwingData> PredictParity(List<List<SwingData>> data, bool left)
        {
            List<SwingData> newData = new();

            try
            {
                for (int i = 0; i < data.Count(); i++)
                {
                    var testData1 = data[i];
                    var testData2 = testData1.ConvertAll(o => new SwingData(o));

                    for (int j = 0; j < testData1.Count(); j++)
                    {
                        if (j > 0)
                        {
                            if (Math.Abs(testData1[j].Angle - testData1[j - 1].Angle) > 45)
                            {
                                testData1[j].Forehand = !testData1[j - 1].Forehand;
                            }
                            else
                            {
                                testData1[j].Forehand = testData1[j - 1].Forehand;
                            }
                        }
                        else
                        {
                            testData1[0].Forehand = true;
                        }
                    }
                    for (int j = 0; j < testData2.Count(); j++)
                    {
                        if (j > 0)
                        {
                            if (Math.Abs(testData2[j].Angle - testData2[j - 1].Angle) > 45)
                            {
                                testData2[j].Forehand = !testData2[j - 1].Forehand;
                            }
                            else
                            {
                                testData2[j].Forehand = testData2[j - 1].Forehand;
                            }
                        }
                        else
                        {
                            testData2[0].Forehand = false;
                        }
                    }
                    var forehandTest = MathUtil.SwingAngleStrainCalc(testData1, left);
                    var backhandTest = MathUtil.SwingAngleStrainCalc(testData2, left);
                    if (forehandTest <= backhandTest)
                    {
                        newData.AddRange(testData1);
                    }
                    else if (forehandTest > backhandTest)
                    {
                        newData.AddRange(testData2);
                    }
                }
                for (int i = 0; i < newData.Count(); i++)
                {
                    var temp = new List<SwingData>
                    {
                        newData[i]
                    };
                    newData[i].AngleStrain = MathUtil.SwingAngleStrainCalc(temp, left);
                    if (i > 0)
                    {
                        if (newData[i].Forehand == newData[i - 1].Forehand)
                        {
                            newData[i].Reset = true;
                        }
                        else
                        {
                            newData[i].Reset = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }

            return newData;
        }

        public static List<SwingData> CalcSwingCurve(List<SwingData> data, bool left)
        {
            try
            {
                if (data.Count() == 0)
                {
                    return data;
                }

                data[0].PathStrain = 0;

                for (int i = 1; i < data.Count(); i++)
                {
                    var point0 = data[i - 1].ExitPosition; // Start of the curve
                    Vector2 point1; // Control point
                    point1.x = (float)(point0.x + 0.5 * Math.Cos(MathUtil.ConvertDegreesToRadians(data[i - 1].Angle)));
                    point1.y = (float)(point0.y + 0.5 * Math.Sin(MathUtil.ConvertDegreesToRadians(data[i - 1].Angle)));
                    var point3 = data[i].EntryPosition; // End of the curve
                    Vector2 point2; // Control point
                    point2.x = (float)(point3.x - 0.5 * Math.Cos(MathUtil.ConvertDegreesToRadians(data[i].Angle)));
                    point2.y = (float)(point3.y - 0.5 * Math.Sin(MathUtil.ConvertDegreesToRadians(data[i].Angle)));
                    List<Vector2> points = new()
                    {
                        point0,
                        point1,
                        point2,
                        point3
                    };
                    points = Helper.PointList3(points, 0.02f); // 50 points
                    for(int j = 0; j < points.Count() - 1; j++)
                    {
                        data[i].Length += Vector2.Distance(points[j], points[j + 1]);
                    }
                    points.Reverse();
                    List<double> speedList = new();
                    List<double> angleList = new();
                    double lookback;
                    for (int j = 1; j < points.Count(); j++)
                    {
                        speedList.Add(Math.Sqrt(Math.Pow(points[j].y - points[j - 1].y, 2) + Math.Pow((points[j].x - points[j - 1].x), 2)));
                        angleList.Add((MathUtil.ConvertRadiansToDegrees(Math.Atan2(points[j].y - points[j - 1].y, points[j].x - points[j - 1].x)) % 360));
                    }
                    if (data[i].Reset)
                    {
                        lookback = 0.8;
                    }
                    else
                    {
                        lookback = 0.333333;
                    }
                    int index = (int)(speedList.Count() * lookback);
                    var temp = new List<double>();
                    var temp2 = new List<double>();
                    for (int j = index; j < speedList.Count(); j++)
                    {
                        temp.Add(speedList[j]);
                        temp2.Add(angleList[j]);
                    }

                    var curveComplexity = speedList.Count() * temp.Average() / 20;
                    var pathAngleStrain = MathUtil.BerzierAngleStrainCalc(temp2, data[i].Forehand, left) / angleList.Count() * 2;

                    data[i].CurveComplexity = curveComplexity;
                    data[i].PathStrain = pathAngleStrain;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }

            return data;
        }
    }
}
