using static BeatmapSaveDataVersion3.BeatmapSaveData;
using System.Collections.Generic;
using System.Linq;
using System;
using BeatmapScanner.Algorithm.Loloppe;

namespace BeatmapScanner.Algorithm.LackWiz
{
    internal class Method
    {
        public static double[] CutDirectionIndex = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };

        #region Main

        public static (double diff, double tech, List<SwingData> data) UseLackWizAlgorithm(List<Cube> red, List<Cube> blue, double bpm)
        {
            double diff = 0;
            double tech = 0;
            double stamina = 0;
            List<SwingData> redSwingData;
            List<SwingData> blueSwingData;
            List<List<SwingData>> redPatternData = new List<List<SwingData>>();
            List<List<SwingData>> bluePatternData = new List<List<SwingData>>();
            List<SwingData> data = new List<SwingData>();

            if (red.Count() > 2)
            {
                redSwingData = SwingProcesser(red);
                if (redSwingData != null)
                {
                    redPatternData = PatternSplitter(redSwingData);
                }
                if (redSwingData != null && redPatternData != null)
                {
                    redSwingData = ParityPredictor(redPatternData, false);
                }
                if (redSwingData != null)
                {
                    SwingCurveCalc(redSwingData, false);
                    diff = DiffToPass(redSwingData, bpm);
                    stamina = StaminaCalc(redSwingData);
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
                if (blueSwingData != null && bluePatternData != null)
                {
                    blueSwingData = ParityPredictor(bluePatternData, true);
                }
                if (blueSwingData != null)
                {
                    SwingCurveCalc(blueSwingData, true);
                    diff = Math.Max(DiffToPass(blueSwingData, bpm), diff);
                    stamina = Math.Max(StaminaCalc(blueSwingData), stamina);
                }
                data.AddRange(blueSwingData);
            }

            if (data.Count() > 2)
            {
                var test = data.Select(c => c.AngleStrain + c.PathStrain).ToList();
                test.Sort();
                tech = test.Skip((int)(data.Count() * 0.25)).Average();
            }

            var balanced_tech = tech * (-1 * Math.Pow(1.4, -diff) + 1) * 10;
            var balanced_pass = diff * stamina;

            return (balanced_pass, balanced_tech, data);
        }

        #endregion

        #region SwingProcesser

        public static List<SwingData> SwingProcesser(List<Cube> cubes)
        {
            var swingData = new List<SwingData>();
            double first = 0;

            if (cubes.Count() == 0)
            {
                return swingData;
            }

            if ((int)cubes[0].Note.cutDirection == 8)
            {
                var c = cubes.Where(ca => (int)ca.Note.cutDirection != 8).FirstOrDefault();
                if (c != null)
                {
                    first = CutDirectionIndex[(int)c.Note.cutDirection] + c.Note.angleOffset;
                    for (int i = cubes.IndexOf(c); i > 1; i--)
                    {
                        if (cubes[i].Beat - cubes[i - 1].Beat >= 0.25)
                        {
                            first = Helper.ReverseCutDirection(first);
                        }
                    }
                }
                else
                {
                    if (cubes[0].Note.layer == 2)
                    {
                        first = 90;
                    }
                    else
                    {
                        first = 270;
                    }
                }
            }
            else
            {
                first = CutDirectionIndex[(int)cubes[0].Note.cutDirection] + cubes[0].Note.angleOffset;
            }

            swingData.Add(new SwingData(cubes[0].Note.beat, first)); ;
            (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = MathWiz.CalculateBaseEntryExit((cubes[0].Note.line, cubes[0].Note.layer), first);

            Helper.FixPatternHead(cubes);

            for (int i = 1; i < cubes.Count; i++)
            {
                if (cubes[0].Beat == cubes[i].Beat && (int)cubes[i].Note.cutDirection == 8 && (int)cubes[i - 1].Note.cutDirection == 8)
                {
                    continue;
                }

                var pattern = false;

                var previousBeat = cubes[i - 1].Beat;
                var previousAngle = swingData.Last().Angle;
                (double x, double y) previousPosition = (cubes[i - 1].Note.line, cubes[i - 1].Note.layer);

                var currentBeat = cubes[i].Beat;
                var currentAngle = CutDirectionIndex[(int)cubes[i].Note.cutDirection] + cubes[i].Note.angleOffset;
                (double x, double y) currentPosition = (cubes[i].Note.line, cubes[i].Note.layer);

                if (currentBeat - previousBeat < 0.245 && (currentAngle == previousAngle || (int)cubes[i].Note.cutDirection == 8
                    || (int)cubes[i - 1].Note.cutDirection == 8))
                {
                    pattern = true;
                }
                else if (currentBeat - previousBeat < 0.255 && Helper.IsSameDirection(previousAngle, currentAngle))
                {
                    pattern = true;
                }

                if ((int)cubes[i].Note.cutDirection == 8 && !pattern)
                {
                    swingData.Add(new SwingData(currentBeat, Helper.ReverseCutDirection(previousAngle)));
                    (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = MathWiz.CalculateBaseEntryExit(currentPosition, Helper.ReverseCutDirection(previousAngle));
                }
                else if (pattern)
                {
                    for (int f = i; f > 0; f--) // Find pattern head
                    {
                        if (cubes[f].Beat - cubes[f - 1].Beat >= 0.25)
                        {
                            previousBeat = cubes[f].Beat;
                            previousPosition = (cubes[f].Note.line, cubes[f].Note.layer);
                            break;
                        }
                        if (f == 1)
                        {
                            previousBeat = cubes[0].Beat;
                            previousPosition = (cubes[0].Note.line, cubes[0].Note.layer);
                        }
                    }

                    currentAngle = Helper.Mod(MathWiz.ConvertRadiansToDegrees(Math.Atan2(previousPosition.y - currentPosition.y, previousPosition.x - currentPosition.x)), 360);
                    double guideAngle = 270;
                    if (swingData.Count() > 1)
                    {
                        guideAngle = Helper.Mod((swingData[swingData.Count() - 2].Angle - 180), 360);
                    }

                    for (int f = i; f > 0; f--)
                    {
                        if (cubes[f].Beat < previousBeat)
                        {
                            break;
                        }
                        if ((int)cubes[f].Note.cutDirection != 8)
                        {
                            guideAngle = CutDirectionIndex[(int)cubes[f].Note.cutDirection] + cubes[f].Note.angleOffset;
                            break;
                        }
                    }
                    if (Math.Abs(currentAngle - guideAngle) > 90)
                    {
                        currentAngle = Helper.ReverseCutDirection(currentAngle);
                    }

                    swingData.Last().Angle = currentAngle;

                    var xtest = (swingData.Last().EntryPosition.x - (currentPosition.x * 0.333333 - Math.Cos(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667)) * Math.Cos(MathWiz.ConvertDegreesToRadians(currentAngle));
                    var ytest = (swingData.Last().EntryPosition.y - (currentPosition.y * 0.333333 - Math.Sin(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667)) * Math.Sin(MathWiz.ConvertDegreesToRadians(currentAngle));
                    if (xtest <= 0.001 && ytest >= 0.001)
                    {
                        swingData.Last().EntryPosition = (currentPosition.x * 0.333333 - Math.Cos(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667, currentPosition.y * 0.333333 - Math.Sin(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667);
                    }
                    else
                    {
                        swingData.Last().ExitPosition = (currentPosition.x * 0.333333 + Math.Cos(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667, currentPosition.y * 0.333333 + Math.Sin(MathWiz.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667);
                    }
                }
                else if (!pattern)
                {
                    swingData.Add(new SwingData(currentBeat, currentAngle));
                    (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = MathWiz.CalculateBaseEntryExit(currentPosition, currentAngle);
                }
            }

            return swingData;
        }

        #endregion

        #region PatternSplitter

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
                            tempPList.Remove(tempPList.Last());
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

        #endregion

        #region ParityPredictor

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
                };

                cloneList.Add(clone);
            }
            return cloneList;
        }

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
                        if (Helper.IsSameDirection(testData1[i - 1].Angle, testData1[i].Angle))
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
                        testData1[0].Forehand = true;
                        testData1[0].Reset = false;
                    }
                }
                for (int i = 0; i < testData2.Count(); i++)
                {
                    if (i > 0)
                    {
                        if (Helper.IsSameDirection(testData2[i - 1].Angle, testData2[i].Angle))
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
                        testData2[0].Forehand = false;
                        testData2[0].Reset = false;
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
            }

            return newPatternData;
        }

        #endregion

        #region SwingCurveCalc

        public static void SwingCurveCalc(List<SwingData> swingData, bool leftOrRight)
        {
            if (swingData.Count() < 2)
            {
                return;
            }

            double pathLookback;
            (double x, double y) simHandCurPos;
            (double x, double y) simHandPrePos;
            double positionDiff;
            double curveComplexity;
            double pathAngleStrain;
            double positionComplexity;

            swingData[0].PathStrain = 0;

            for (int i = 1; i < swingData.Count(); i++)
            {
                var point0 = swingData[i - 1].ExitPosition; 
                (double x, double y) point1;
                point1.x = point0.x + 1 * Math.Cos(MathWiz.ConvertDegreesToRadians(swingData[i - 1].Angle));
                point1.y = point0.y + 1 * Math.Sin(MathWiz.ConvertDegreesToRadians(swingData[i - 1].Angle));
                (double x, double y) point3 = swingData[i].EntryPosition; 
                (double x, double y) point2;
                point2.x = point3.x - 1 * Math.Cos(MathWiz.ConvertDegreesToRadians(swingData[i].Angle));
                point2.y = point3.y - 1 * Math.Sin(MathWiz.ConvertDegreesToRadians(swingData[i].Angle));

                List<(double x, double y)> points = new()
                {
                    point0,
                    point1,
                    point2,
                    point3
                };

                var point = MathWiz.PointList2(points, 0.04);

                positionComplexity = 0;
                List<double> angleChangeList = new();
                List<double> angleList = new();
                double distance = 0;
                for (int f = 1; f < point.Count(); f++)
                {
                    angleList.Add(Helper.Mod(MathWiz.ConvertRadiansToDegrees(Math.Atan2(point[f].y - point[f - 1].y, point[f].x - point[f - 1].x)), 360));
                    distance += Math.Sqrt(Math.Pow(point[f].y - point[f - 1].y, 2) + Math.Pow(point[f].x - point[f - 1].x, 2));
                    if (f > 1)
                    {
                        angleChangeList.Add(180 - Math.Abs(Math.Abs(angleList.Last() - angleList[angleList.Count() - 2]) - 180));
                    }
                }

                if (i > 1)
                {
                    simHandCurPos = swingData[i].EntryPosition;
                    if (!swingData[i].Reset && !swingData[i - 1].Reset)
                    {
                        simHandPrePos = swingData[i - 2].EntryPosition;
                    }
                    else if (!swingData[i].Reset && swingData[i - 1].Reset)
                    {
                        simHandPrePos = swingData[i - 1].EntryPosition;

                    }
                    else if (swingData[i].Reset)
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

                var lengthOfList = angleChangeList.Count() * 0.6;
                double first;
                double last;

                if (swingData[i].Reset)
                {
                    pathLookback = 0.9;
                    first = 0.5;
                    last = 1;
                }
                else
                {
                    pathLookback = 0.5;
                    first = 0.2;
                    last = 0.8;
                }
                var pathLookbackIndex = (int)(angleList.Count() * pathLookback);
                var firstIndex = (int)(angleChangeList.Count() * first) - 1;
                var lastIndex = (int)(angleChangeList.Count() * last) - 1;

                curveComplexity = Math.Abs((lengthOfList * angleChangeList.Take(lastIndex).Skip(firstIndex).Average() - 180) / 180);
                pathAngleStrain = MathWiz.BezierAngleStrainCalc(angleList.Skip(pathLookbackIndex).ToList(), swingData[i].Forehand, leftOrRight) / angleList.Count() * 2;

                swingData[i].PositionComplexity = positionComplexity;
                swingData[i].PreviousDistance = distance;
                swingData[i].CurveComplexity = curveComplexity;
                swingData[i].AnglePathStrain = pathAngleStrain;
                swingData[i].PathStrain = curveComplexity + pathAngleStrain + positionComplexity;
            }
        }

        #endregion

        #region DiffToPass

        public static double DiffToPass(List<SwingData> swingData, double bpm)
        {
            if (swingData.Count() < 2)
            {
                return 0;
            }

            var bps = bpm / 60;
            var qDiff = new Queue<double>();
            double windowDiff;
            double window = 50;
            var difficultyIndex = new List<double>();
            var data = new List<SData>();

            swingData[0].SwingDiff = 0;
            for (int i = 1; i < swingData.Count(); i++)
            {
                var distanceDiff = swingData[i].PreviousDistance / (swingData[i].PreviousDistance + 3) + 1;
                data.Add(new(swingData[i].SwingFrequency * distanceDiff * bps));
                if (swingData[i].Reset)
                {
                    data.Last().SwingSpeed *= 2;
                }
                var xHitDist = swingData[i].EntryPosition.x - swingData[i].ExitPosition.x;
                var yHitDist = swingData[i].EntryPosition.y - swingData[i].ExitPosition.y;
                data.Last().HitDistance = Math.Sqrt(Math.Pow(xHitDist, 2) + Math.Pow(yHitDist, 2));
                data.Last().HitDiff = data.Last().HitDistance / (data.Last().HitDistance + 2) + 1;
                data.Last().Stress = (swingData[i].AngleStrain + swingData[i].PathStrain) * data.Last().HitDiff;
                swingData[i].SwingDiff = data.Last().SwingSpeed * (-1 * Math.Pow(1.4, -data.Last().SwingSpeed) + 1) * (data.Last().Stress / (data.Last().Stress + 2) + 1);

                if (i > window)
                {
                    qDiff.Dequeue();
                }
                qDiff.Enqueue(swingData[i].SwingDiff);
                List<double> tempList = qDiff.ToList();
                tempList.Sort();
                tempList.Reverse();

                var temp = tempList.Take((int)(tempList.Count() * 25d / window));
                if(temp.Count() > 1)
                {
                    windowDiff = temp.Sum() / 25 * 0.8;
                }
                else
                {
                    windowDiff = 0;
                }
                difficultyIndex.Add(windowDiff);
            }

            if(difficultyIndex.Count > 0)
            {
                return difficultyIndex.Max();
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region StaminaCalc

        public static double StaminaCalc(List<SwingData> swingData)
        {
            if (swingData.Count() < 16)
            {
                return 0;
            }

            var swingDiffList = swingData.Select(s => s.SwingDiff).ToList();
            swingDiffList.Sort();
            swingDiffList.Reverse();
            var averageDiff = swingDiffList.Take((int)(swingDiffList.Count() * 0.5)).Average();
            var burstDiff = swingDiffList.Take(Math.Min(swingDiffList.Count() / 8, 2)).Average();
            if(burstDiff == 0)
            {
                return 0;
            }
            var staminaRatio = averageDiff / burstDiff;
            return 1 / (10 + Math.Pow(4, -64 * (staminaRatio - 0.875))) + 0.9 + staminaRatio / 20;
        }

        #endregion

    }
}
