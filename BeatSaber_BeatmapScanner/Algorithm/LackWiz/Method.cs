using static BeatmapSaveDataVersion3.BeatmapSaveData;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BeatmapScanner.Algorithm.LackWiz
{
    internal class Method
    {
        public static int[] CutDirectionIndex = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };

        #region Main

        public static (double diff, double odiff, double tech, List<SwingData> data) UseLackWizAlgorithm(List<ColorNoteData> red, List<ColorNoteData> blue, double bpm)
        {
            double diff = 0;
            double odiff = 0;
            double tech = 0;
            List<SwingData> redSwingData;
            List<SwingData> blueSwingData;
            List<List<SwingData>> redPatternData = new();
            List<List<SwingData>> bluePatternData = new();
            List<SwingData> data = new();

            if (red.Count() > 2)
            {
                redSwingData = SwingProcesser(red);
                if(redSwingData != null)
                {
                    redPatternData = PatternSplitter(redSwingData);
                }
                if(redPatternData != null)
                {
                    redSwingData = ParityPredictor(redPatternData, false);
                }
                if (redSwingData != null)
                {
                    SwingCurveCalc(redSwingData, false);
                    (diff, odiff) = DiffToPass(redSwingData, bpm);
                }
                data.AddRange(redSwingData);
            }

            if (blue.Count() > 2)
            {
                blueSwingData = SwingProcesser(blue);
                if (blueSwingData != null)
                {
                    bluePatternData = PatternSplitter(blueSwingData);
                }
                if (bluePatternData != null)
                {
                    blueSwingData = ParityPredictor(bluePatternData, true);
                }
                if (blueSwingData != null)
                {
                    SwingCurveCalc(blueSwingData, true);
                    double temp, temp2;
                    (temp, temp2) = DiffToPass(blueSwingData, bpm);
                    diff = Math.Max(diff, temp);
                    odiff = Math.Max(odiff, temp2);
                }
                data.AddRange(blueSwingData);
            }

            if(data.Count() > 2)
            {
                var test = data.Select(c => c.AngleStrain + c.PathStrain).ToList();
                test.Sort();
                tech = Math.Round(test.Skip((int)(data.Count() * 0.25)).Average(), 3);
            }

            return (diff, odiff, tech, data);
        }

        #endregion

        #region SwingProcesser

        // Swing angle, entry/exit and timestamps
        public static List<SwingData> SwingProcesser(List<ColorNoteData> notes)
        {
            var swingData = new List<SwingData>();

            for(int i = 0; i < notes.Count; i++)
            {
                var isSlider = false;
                double sliderTime = 0;
                double guideAngle;
                var currentBeat = notes[i].beat;
                var currentAngle = (double)(CutDirectionIndex[(int)notes[i].cutDirection] + notes[i].angleOffset);
                (double x, double y) currentPosition = (notes[i].line, notes[i].layer);
                if(i > 0)
                {
                    #region Pre-caching
                    var previousBeat = notes[i - 1].beat;
                    var previousAngle = swingData.Last().Angle;
                    (double x, double y) previousPosition = (notes[i - 1].line, notes[i - 1].layer);
                    if((int)notes[i].cutDirection == 8)
                    {
                        if (currentBeat - previousBeat <= 0.03125)
                        {
                            currentAngle = previousAngle;
                        }
                        else
                        {
                            if (previousAngle >= 180)
                            {
                                currentAngle = previousAngle - 180;
                            }
                            else
                            {
                                currentAngle = previousAngle + 180;
                            }
                        }
                    }

                    #endregion

                    if(currentBeat - previousBeat >= 0.03125)
                    {
                        if(currentBeat - previousBeat > 0.125)
                        {
                            if(currentBeat - previousBeat > 0.5)
                            {
                                swingData.Add(new SwingData(notes[i], currentBeat, currentAngle));
                                (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = MathWiz.CalculateBaseEntryExit(currentPosition, currentAngle);
                            }
                            else
                            {
                                if(Math.Abs(currentAngle - previousAngle) < 112.5)
                                {
                                    var testAngleFromPosition = MathWiz.ConvertRadiansToDegrees((double)Math.Atan2(previousPosition.y - currentPosition.y, previousPosition.x - currentPosition.x)) % 360;
                                    var averageAngleOfBlocks = (currentAngle + previousAngle) / 2;
                                    if(Math.Abs(testAngleFromPosition - averageAngleOfBlocks) <= 56.25)
                                    {
                                        sliderTime = currentBeat - previousBeat;
                                        isSlider = true;
                                    }
                                    else
                                    {
                                        swingData.Add(new SwingData(notes[i], currentBeat, currentAngle));
                                        (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = MathWiz.CalculateBaseEntryExit(currentPosition, currentAngle);
                                    }
                                }
                                else
                                {
                                    swingData.Add(new SwingData(notes[i], currentBeat, currentAngle));
                                    (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = MathWiz.CalculateBaseEntryExit(currentPosition, currentAngle);
                                }
                            }
                        }
                        else 
                        {
                            if ((int)notes[i].cutDirection == 8 || Math.Abs(currentAngle - previousAngle) < 90)
                            {
                                sliderTime = 0.125;
                                isSlider = true;
                            }
                            else
                            {
                                swingData.Add(new SwingData(notes[i], currentBeat, currentAngle));
                                (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = MathWiz.CalculateBaseEntryExit(currentPosition, currentAngle);
                            }
                        }
                    }
                    else 
                    {
                        sliderTime = 0.03125;
                        isSlider = true;
                    }
                    if(isSlider)
                    {
                        for (int f = 1; f < notes.Count(); f++)
                        {
                            var blockIndex = i - f; 
                            if(blockIndex < 1)
                            {
                                break;  
                            }
                            if (notes[blockIndex].beat - notes[blockIndex - 1].beat > 2 * sliderTime)
                            {
                                previousBeat = notes[blockIndex].beat;
                                previousPosition = (notes[blockIndex].line, notes[blockIndex].layer);
                                break;
                            }
                        }
                        currentAngle = MathWiz.ConvertRadiansToDegrees(Math.Atan2(previousPosition.y - currentPosition.y, previousPosition.x - currentPosition.x)) % 360;
                        if(swingData.Count() > 1)
                        {
                            guideAngle = (swingData[swingData.Count() - 2].Angle - 180) % 360;
                        }
                        else
                        {
                            guideAngle = 270;
                        }
                        for(int f = 1; f < notes.Count(); f++)
                        {
                            var blockIndex = i - f;
                            if(blockIndex < 0)
                            {
                                break;
                            }
                            if (notes[blockIndex].beat < previousBeat)
                            {
                                break;
                            }
                            if ((int)notes[blockIndex].cutDirection != 8)
                            {
                                guideAngle = CutDirectionIndex[(int)notes[blockIndex].cutDirection];
                                break;
                            }
                        }
                        if(Math.Abs(currentAngle - guideAngle) > 90)
                        {
                            if(currentAngle >= 180)
                            {
                                currentAngle -= 180;
                            }
                            else
                            {
                                currentAngle += 180;
                            }
                        }

                        if(currentAngle < 0)
                        {
                            currentAngle += 180;
                        }
                        else if(currentAngle > 360)
                        {
                            currentAngle -= 180;
                        }

                        swingData.Last().Angle = currentAngle;

                        var xtest = (swingData.Last().EntryPosition.x - (currentPosition.x * 0.333333 - Math.Cos(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667)) * Math.Cos(MathWiz.ConvertDegreesToRadians(currentAngle));
                        var ytest = (swingData.Last().EntryPosition.y - (currentPosition.y * 0.333333 - Math.Sin(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667)) * Math.Sin(MathWiz.ConvertDegreesToRadians(currentAngle));

                        if (xtest <= 0.001 && ytest >= 0.001)
                        {
                            swingData.Last().EntryPosition = (currentPosition.x * 0.333333 - Math.Cos(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667, currentPosition.y * 0.333333 - Math.Sin(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.16667);
                        }
                        else
                        {
                            swingData.Last().ExitPosition =  (currentPosition.x * 0.333333 + Math.Cos(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667, currentPosition.y * 0.333333 + Math.Sin(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.16667);
                        }
                    }
                }
                else
                {
                    swingData.Add(new SwingData(notes[i], currentBeat, currentAngle));
                    (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = MathWiz.CalculateBaseEntryExit(currentPosition, currentAngle);
                }
            }

            return swingData;
        }

        #endregion

        #region PatternSplitter

        // Swing speed and pattern list
        public static List<List<SwingData>> PatternSplitter(List<SwingData> swingData)
        {
            if (swingData.Count() < 2)
            {
                return null;
            }

            for (int i = 0; i < swingData.Count(); i++)
            {
                if (i > 0 && i + 1 < swingData.Count())
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

            for (int i = 0; i < swingData.Count(); i++)
            {
                if (i > 0)
                {
                    if (1 / (swingData[i].Time - swingData[i - 1].Time) - swingData[i].SwingFrequency <= SFMargin)
                    {
                        if (!patternFound)
                        {
                            patternFound = true;
                            tempPList.RemoveAt(tempPList.Count() - 1);
                            if (tempPList.Count() > 0)
                            {
                                patternList.Add(tempPList);
                            }
                            tempPList = new()
                            {
                                swingData[i - 1]
                            };
                        }
                        tempPList.Add(swingData[i]);
                    }
                    else
                    {
                        if (tempPList.Count() > 0 && patternFound)
                        {
                            tempPList.Add(swingData[i]);
                            patternList.Add(tempPList);
                            tempPList = new();
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

            if (tempPList.Count > 0 && patternList.Count() == 0)
            {
                patternList.Add(tempPList);
            }

            return patternList;
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
                    Forehand = d.Forehand,
                    PathStrain = d.PathStrain,
                    AngleStrain = d.AngleStrain,
                    AnglePathStrain = d.AnglePathStrain,
                    PositionComplexity = d.PositionComplexity,
                    CurveComplexity = d.CurveComplexity,
                };

                cloneList.Add(clone);
            }
            return cloneList;
        }

        #endregion

        #region ParityPredictor

        // Forehand, Reset and AngleStrain
        public static List<SwingData> ParityPredictor(List<List<SwingData>> patternData, bool leftOrRight)
        {
            if (patternData.Count() < 1)
            {
                return null;
            }

            var newPatternData = new List<SwingData>();

            for (int p = 0; p < patternData.Count(); p++)
            {
                var testData1 = patternData[p];
                var testData2 = DeepCopy(patternData[p]);
                for (int i = 0; i < testData1.Count(); i++)
                {
                    if (i > 0)
                    {
                        if (Math.Abs(testData1[i].Angle - testData1[i - 1].Angle) > 45)
                        {
                            testData1[i].Forehand = !testData1[i - 1].Forehand;
                        }
                        else
                        {
                            testData1[i].Forehand = testData1[i - 1].Forehand;
                        }
                    }
                    else
                    {
                        testData1[0].Forehand = true;
                    }
                }
                for (int i = 0; i < testData2.Count(); i++)
                {
                    if (i > 0)
                    {
                        if (Math.Abs(testData2[i].Angle - testData2[i - 1].Angle) > 45)
                        {
                            testData2[i].Forehand = !testData2[i - 1].Forehand;
                        }
                        else
                        {
                            testData2[i].Forehand = testData2[i - 1].Forehand;
                        }
                    }
                    else
                    {
                        testData2[0].Forehand = false;
                    }
                }

                var forehandTest = MathWiz.SwingAngleStrainCalc(testData1, leftOrRight);
                var backhandTest = MathWiz.SwingAngleStrainCalc(testData2, leftOrRight);
                if (forehandTest <= backhandTest)
                {
                    newPatternData.AddRange(testData1);
                }
                else if (forehandTest > backhandTest)
                {
                    newPatternData.AddRange(testData2);
                }
            }
            for (int i = 0; i < newPatternData.Count(); i++)
            {
                newPatternData[i].AngleStrain = MathWiz.SwingAngleStrainCalc(new List<SwingData> { newPatternData[i] }, leftOrRight);
                if (i > 0)
                {
                    if (newPatternData[i].Forehand == newPatternData[i - 1].Forehand)
                    {
                        newPatternData[i].Reset = true;
                    }
                    else
                    {
                        newPatternData[i].Reset = false;
                    }
                }
                else
                {
                    newPatternData[i].Reset = false;
                }
            }

            return newPatternData;
        }

        #endregion

        #region SwingCurveCalc

        // Position Complexity, Curve Complexity, Angle Path Strain and Path Strain
        public static void SwingCurveCalc(List<SwingData> swingData, bool leftOrRight)
        {
            if (swingData.Count() < 2)
            {
                return;
            }

            double lookback;
            (double x, double y) simHandCurPos;
            (double x, double y) simHandPrePos;
            double positionDiff;
            double curveComplexity;
            double pathAngleStrain;
            double positionComplexity;

            if (swingData.Count() == 0)
            {
                return;
            }

            swingData[0].PathStrain = 0;

            for (int i = 1; i < swingData.Count() - 1; i++)
            {
                var point0 = swingData[i - 1].ExitPosition; 
                (double x, double y) point1;
                point1.x = point0.x + 0.5 * Math.Cos(MathWiz.ConvertDegreesToRadians(swingData[i - 1].Angle));
                point1.y = point0.y + 0.5 * Math.Sin(MathWiz.ConvertDegreesToRadians(swingData[i - 1].Angle));
                (double x, double y) point3 = swingData[i].EntryPosition; 
                (double x, double y) point2;
                point2.x = point3.x - 0.5 * Math.Cos(MathWiz.ConvertDegreesToRadians(swingData[i].Angle));
                point2.y = point3.y - 0.5 * Math.Sin(MathWiz.ConvertDegreesToRadians(swingData[i].Angle));

                List<(double x, double y)> points = new()
                {
                    point0,
                    point1,
                    point2,
                    point3
                };

                var point = MathWiz.PointList2(points, 0.04);

                List<double> angleChangeList = new();
                List<double> angleList = new();

                for (int f = 1; f < point.Count(); f++)
                {
                    angleList.Add(MathWiz.ConvertRadiansToDegrees(Math.Atan2(point[f].y - point[f - 1].y, point[f].x - point[f - 1].x)) % 360);
                    if(f > 1)
                    {
                        angleChangeList.Add(180 - Math.Abs(Math.Abs(angleList.Last() - angleList[angleList.Count() - 2]) - 180)); 
                    }
                }
                if (swingData[i].Reset) 
                {
                    lookback = 0.8;
                }
                else
                {
                    lookback = 0.333333;
                }
                if (i > 1)
                {
                    simHandCurPos = swingData[i].EntryPosition;
                    if (swingData[i].Forehand == swingData[i - 2].Forehand)
                    {
                        simHandPrePos = swingData[i - 2].EntryPosition;
                    }
                    else if (swingData[i].Forehand == swingData[i - 1].Forehand)
                    {
                        simHandPrePos = swingData[i - 1].EntryPosition;
                    }
                    else
                    {
                        simHandPrePos = simHandCurPos;
                    }
                    positionDiff = Math.Sqrt(Math.Pow(simHandCurPos.y - simHandPrePos.y, 2) + Math.Pow(simHandCurPos.x - simHandPrePos.x, 2));
                    positionComplexity = Math.Pow(positionDiff, 2);
                }
                else
                {
                    positionComplexity = 0;
                }
                curveComplexity = Math.Abs((angleChangeList.Count() * angleChangeList.Average() - 180) / 180);
                pathAngleStrain = MathWiz.BezierAngleStrainCalc(angleList.Skip((int)(angleList.Count() * lookback)).ToList(), swingData[i].Forehand, leftOrRight) / angleList.Count() * 2;

                swingData[i].PositionComplexity = positionComplexity;
                swingData[i].CurveComplexity = curveComplexity;
                swingData[i].AnglePathStrain = pathAngleStrain;
                swingData[i].PathStrain = curveComplexity + pathAngleStrain + positionComplexity;
            }
        }

        #endregion

        #region DiffToPass

        public static (double diff, double odiff) DiffToPass(List<SwingData> swingData, double bpm)
        {
            if(swingData == null)
            {
                return (0, 0);
            }

            var difficulty = 0d;
            var olddifficulty = 0d;

            var bps = bpm / 60;
            var SSSpeed = 0d; 
            var qSS = new Queue<double>();  
            var SSStress = 0d; 
            var qST = new Queue<double>();  
            var smoothing = 8;
            var difficultyIndex = new List<double>();
            var data = new List<SData>();

            for (int i = 1; i < swingData.Count(); i++) 
            {
                var xDist = swingData[i].ExitPosition.x - swingData[i - 1].ExitPosition.x;
                var yDist = swingData[i].ExitPosition.y - swingData[i - 1].ExitPosition.y;
                data.Add(new SData((double)Math.Sqrt(Math.Pow(xDist, 2) + Math.Pow(yDist, 2))));
                if (i > smoothing) 
                {
                    SSSpeed -= qSS.Dequeue();
                    SSStress -= qST.Dequeue();
                }
                qSS.Enqueue(swingData[i].SwingFrequency * data.Last().PreDistance * bps);
                SSSpeed += qSS.Last();
                data.Last().AverageSwingSpeed = SSSpeed / smoothing;

                qST.Enqueue(swingData[i].AngleStrain + swingData[i].PathStrain);
                SSStress += qST.Last();
                data.Last().AverageStress = SSStress / smoothing;

                difficulty = data.Last().AverageSwingSpeed * (data.Last().AverageStress + 0.6667);
                data.Last().Difficulty = difficulty;

                difficultyIndex.Add(difficulty);
            }

            if (difficultyIndex.Count() > 1)
            {
                difficultyIndex.Sort();
                difficultyIndex.Reverse();
                difficulty = difficultyIndex.Take(Math.Min(Math.Max(difficultyIndex.Count() / 16, 2), difficultyIndex.Count())).Average();
                olddifficulty = difficultyIndex.Take(Math.Min(smoothing * 8, difficultyIndex.Count())).Average();
            }

            return (difficulty, olddifficulty);
        }

        #endregion

    }
}
