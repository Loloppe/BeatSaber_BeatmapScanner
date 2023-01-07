using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BeatmapSaveDataVersion3.BeatmapSaveData;

// Source of code taken for this project:
// Tech https://github.com/LackWiz/ppCurve/
// Bezier https://github.com/shamim-akhtar/bezier-curve
// GetDistance https://github.com/tmokmss/Osu2Saber

namespace BeatmapScanner.Algorithm
{
    internal class BeatmapScanner
    {
        public static int[] CutDirectionIndex = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };

        public static float Analyzer(List<ColorNoteData> notes, float bpm)
        {
            var point = 0f;

            if (notes.Count > 0 && bpm > 0)
            {
                List<ColorNoteData> red = new();
                List<ColorNoteData> blue = new();

                foreach (var note in notes)
                {
                    if (note.color == NoteColorType.ColorA && (int)note.cutDirection != 9)
                    {
                        red.Add(note);
                    }
                    else if (note.color == NoteColorType.ColorB && (int)note.cutDirection != 9)
                    {
                        blue.Add(note);
                    }
                }

                var FullData = new List<SwingData>();

                // Find position and angle
                // Find pattern and frequency
                // Find parity and angle strain
                // Find path complexity and strain
                // Combine and order by time

                var techFactor = 0.0d;

                if (notes.Count > 20)
                {
                    if (red.Count() > 0)
                    {
                        var LeftSwingData = ProcessSwing(red);
                        var LeftPatternData = SplitPattern(LeftSwingData);
                        LeftSwingData = PredictParity(LeftPatternData, true);
                        LeftSwingData = CalcSwingCurve(LeftSwingData, true);
                        FullData.AddRange(LeftSwingData);
                    }

                    if (blue.Count() > 0)
                    {
                        var RightSwingData = ProcessSwing(blue);
                        var RightPatternData = SplitPattern(RightSwingData);
                        RightSwingData = PredictParity(RightPatternData, false);
                        RightSwingData = CalcSwingCurve(RightSwingData, false);
                        FullData.AddRange(RightSwingData);
                    }

                    FullData = FullData.OrderBy(o => o.Time).ToList();

                    var StrainList = new List<double>();
                    foreach (var fd in FullData)
                    {
                        StrainList.Add(fd.AngleStrain + fd.PathStrain);
                    }
                    StrainList = StrainList.OrderBy(o => o).ToList();

                    for (int i = (int)(StrainList.Count * 0.2); i < StrainList.Count(); i++)
                    {
                        techFactor += StrainList[i];
                    }

                    techFactor /= (StrainList.Count() - (int)(StrainList.Count() * 0.2));
                }
                else
                {
                    techFactor = 0.5f;
                }

                var nps = (notes.Count() / GetActiveSecond(notes, bpm));

                // Calculate points for each hands, then divide by seconds of active mapping
                var pointList = GetScorePerHand(red, bpm, nps);
                pointList.AddRange(GetScorePerHand(blue, bpm, nps));
                pointList = pointList.OrderBy(o => o).ToList();

                for (int i = (int)(pointList.Count * 0.2); i < pointList.Count(); i++)
                {
                    point += pointList[i];
                }

                // Average per note
                point /= notes.Count() - (notes.Count() * 0.2f);
                // Multiplied by NPS
                point *= (1 + nps);
                // Multiplied by tech
                point *= (1 + (float)techFactor);
                // Divided by X factor to make it closer to known value
                point /= 10;
                point = (float)Math.Round(point, 2);
            }

            return point;
        }

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
                            if (beat - previousBeat <= 1 / 32)
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
                        if (beat - previousBeat >= 1 / 32)
                        {
                            if (beat - previousBeat > 1 / 8)
                            {
                                if (beat - previousBeat > 1 / 2)
                                {
                                    data.Add(new SwingData(beat, angle));
                                    (data.Last().EntryPosition, data.Last().ExitPosition) = CalculateBaseEntryExit(position, angle);
                                }
                                else // 1/2
                                {
                                    if (Math.Abs(angle - previousAngle) < 112.5)
                                    {
                                        var testAngle = ConvertRadiansToDegrees(Math.Atan2(previousPosition.layer - position.layer, previousPosition.line - position.line)) % 360;
                                        var averageAngle = (angle + previousAngle) / 2;
                                        if (Math.Abs(testAngle - averageAngle) <= 56.25) // Probably a slider
                                        {
                                            sliderDuration = beat - previousBeat;
                                            slider = true;
                                        }
                                        else
                                        {
                                            data.Add(new SwingData(beat, angle));
                                            (data.Last().EntryPosition, data.Last().ExitPosition) = CalculateBaseEntryExit(position, angle);
                                        }
                                    }
                                    else
                                    {
                                        data.Add(new SwingData(beat, angle));
                                        (data.Last().EntryPosition, data.Last().ExitPosition) = CalculateBaseEntryExit(position, angle);
                                    }
                                }
                            }
                            else // 1/8
                            {
                                if ((int)notes[i].cutDirection == 8 || Math.Abs(angle - previousAngle) < 90) // Probably a slider
                                {
                                    sliderDuration = 1 / 8;
                                    slider = true;
                                }
                                else
                                {
                                    data.Add(new SwingData(beat, angle));
                                    (data.Last().EntryPosition, data.Last().ExitPosition) = CalculateBaseEntryExit(position, angle);
                                }
                            }
                        }
                        else // 1/32
                        {
                            sliderDuration = 1 / 32;
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

                            angle = (int)ConvertRadiansToDegrees(Math.Atan2(previousPosition.layer - position.layer, previousPosition.line - position.line)) % 360;
                            var guideAngle = -1;
                            for (int j = 1; j < notes.Count; j++)
                            {
                                var index = i - j;
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

                            var x = (data.Last().EntryPosition.x - (position.line * 0.333333 - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667)) * Math.Cos(ConvertDegreesToRadians(angle));
                            var y = (data.Last().EntryPosition.y - (position.layer * 0.333333 - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667)) * Math.Sin(ConvertDegreesToRadians(angle));
                            if (x <= 0.001 && y >= 0.001) // Replace either the entry point or exit point for the slider
                            {
                                data.Last().EntryPosition = new Vector2((float)(position.line * 0.333333 - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667), (float)(position.layer * 0.333333 - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667));
                            }
                            else
                            {
                                data.Last().ExitPosition = new Vector2((float)(position.line * 0.333333 + Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667), (float)(position.layer * 0.333333 + Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667));
                            }
                        }
                    }
                    else // First note
                    {
                        data.Add(new SwingData(beat, angle));
                        (data.Last().EntryPosition, data.Last().ExitPosition) = CalculateBaseEntryExit(position, angle);
                    }
                }
            }
            catch(Exception e)
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
            catch(Exception e)
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
                    var forehandTest = SwingAngleStrainCalc(testData1, left);
                    var backhandTest = SwingAngleStrainCalc(testData2, left);
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
                    newData[i].AngleStrain = SwingAngleStrainCalc(newData[i], left);
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
            catch(Exception e)
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
                    point1.x = (float)(point0.x + 0.5 * Math.Cos(ConvertDegreesToRadians(data[i - 1].Angle)));
                    point1.y = (float)(point0.y + 0.5 * Math.Sin(ConvertDegreesToRadians(data[i - 1].Angle)));
                    var point3 = data[i].EntryPosition; // End of the curve
                    Vector2 point2; // Control point
                    point2.x = (float)(point3.x - 0.5 * Math.Cos(ConvertDegreesToRadians(data[i].Angle)));
                    point2.y = (float)(point3.y - 0.5 * Math.Sin(ConvertDegreesToRadians(data[i].Angle)));
                    List<Vector2> points = new()
                    {
                        point0,
                        point1,
                        point2,
                        point3
                    };
                    points = PointList3(points, 0.02f); // 50 points
                    points.Reverse();
                    List<double> speedList = new();
                    List<double> angleList = new();
                    double lookback;
                    for (int j = 1; j < points.Count(); j++)
                    {
                        speedList.Add(Math.Sqrt(Math.Pow(points[j].y - points[j - 1].y, 2) + Math.Pow((points[j].x - points[j - 1].x), 2)));
                        angleList.Add((ConvertRadiansToDegrees(Math.Atan2(points[j].y - points[j - 1].y, points[j].x - points[j - 1].x)) % 360));
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
                    var pathAngleStrain = BerzierAngleStrainCalc(temp2, data[i].Forehand, left) / angleList.Count() * 2;

                    data[i].CurveComplexity = curveComplexity;
                    data[i].PathStrain = pathAngleStrain;
                }
            }
            catch(Exception e)
            {
                Debug.Log(e.ToString());
            }

            return data;
        }

        // https://github.com/shamim-akhtar/bezier-curve

        public static List<Vector2> PointList3(List<Vector2> controlPoints, float interval = 0.01f)
        {
            int N = controlPoints.Count - 1;
            if (N > 16)
            {
                Debug.Log("You have used more than 16 control points.");
                Debug.Log("The maximum control points allowed is 16.");
                controlPoints.RemoveRange(16, controlPoints.Count - 16);
            }

            List<Vector2> points = new();
            for (float t = 0.0f; t <= 1.0f + interval - 0.0001f; t += interval)
            {
                Vector2 p = new();
                for (int i = 0; i < controlPoints.Count; ++i)
                {
                    Vector2 bn = Bernstein(N, i, t) * controlPoints[i];
                    p += bn;
                }
                points.Add(p);
            }

            return points;
        }

        private static float Bernstein(int n, int i, float t)
        {
            float t_i = Mathf.Pow(t, i);
            float t_n_minus_i = Mathf.Pow((1 - t), (n - i));

            float basis = Binomial(n, i) * t_i * t_n_minus_i;
            return basis;
        }

        public static readonly float[] Factorial = new float[]
        {
                1.0f,
                1.0f,
                2.0f,
                6.0f,
                24.0f,
                120.0f,
                720.0f,
                5040.0f,
                40320.0f,
                362880.0f,
                3628800.0f,
                39916800.0f,
                479001600.0f,
                6227020800.0f,
                87178291200.0f,
                1307674368000.0f,
                20922789888000.0f,
        };

        public static float Binomial(int n, int i)
        {
            float ni;
            float a1 = Factorial[n];
            float a2 = Factorial[i];
            float a3 = Factorial[n - i];
            ni = a1 / (a2 * a3);
            return ni;
        }

        [Serializable]
        internal class SwingData
        {
            public float Time { get; set; }
            public int Angle { get; set; }
            public float Frequency { get; set; }
            public bool Forehand { get; set; }
            public double AngleStrain { get; set; }
            public double PathStrain { get; set; }
            public double CurveComplexity { get; set; }
            public bool Reset { get; set; }
            public Vector2 EntryPosition { get; set; }
            public Vector2 ExitPosition { get; set; }

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
            }
        }

        public static (Vector2, Vector2) CalculateBaseEntryExit((int line, int layer) position, int angle)
        {
            Vector2 entry = new((float)(position.line * 0.333333 - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667), (float)(position.layer * 0.333333 - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667));
            Vector2 exit = new((float)(position.line * 0.333333 + Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667), (float)(position.layer * 0.333333 + Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.16667));
            
            return (entry, exit);
        }

        public static double SwingAngleStrainCalc(List<SwingData> data, bool left)
        {
            var strainAmount = 0d;

            for(int i = 0; i < data.Count(); i++)
            {
                if (data[i].Forehand)
                {
                    if(left)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - data[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - data[i].Angle) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (left)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - data[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - data[i].Angle) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }

        public static double BerzierAngleStrainCalc(List<double> angle, bool forehand, bool left)
        {
            var strainAmount = 0d;

            for(int i = 0; i < angle.Count; i++)
            {
                if (forehand)
                {
                    if (left)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - angle[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - angle[i]) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (left)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - angle[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - angle[i]) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }

        public static double SwingAngleStrainCalc(SwingData data, bool left)
        {
            if (data.Forehand)
            {
                if (left)
                {
                    return 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - data.Angle) - 180)) / 180, 2);
                }
                else
                {
                    return 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - data.Angle) - 180)) / 180, 2);
                }
            }
            else
            {
                if (left)
                {
                    return 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - data.Angle) - 180)) / 180, 2);
                }
                else
                {
                    return 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - data.Angle) - 180)) / 180, 2);
                }
            }
        }

        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static double ConvertRadiansToDegrees(double radians)
        {
            double degrees = (180 / Math.PI) * radians;
            return (degrees);
        }

        public static List<float> GetScorePerHand(List<ColorNoteData> notes, float bpm, float nps)
        {
            List<float> points = new();
            bool pattern;
            float multiplier = 0f;
            ColorNoteData lastNote = null;
            int lastCutDirection = 0;
            foreach (var note in notes)
            {
                pattern = false;

                if (lastNote != null)
                {
                    if (note.beat - lastNote.beat <= 0.1)
                    {
                        pattern = true;
                    }
                    else
                    {
                        int typeOfSwing;
                        switch (typeOfSwing = BeatmapScanner.GetTypeOfSwing((int)note.cutDirection, lastCutDirection))
                        {
                            case 0: // Linear
                                multiplier = 1.5f;
                                break;
                            case 1: // Tech
                                multiplier = 7f;
                                break;
                            case 2: // DD
                                multiplier = 9f;
                                break;
                            case 3: // Semi
                                multiplier = 3f;
                                break;
                        }
                        if(DetectInverted(note, lastNote, typeOfSwing))
                        {
                            multiplier += 5f;
                        }
                        if(nps <= 5f)
                        {
                            if(multiplier >= 5f)
                            {
                                multiplier /= 2f;
                            }
                            if(nps <= 2f)
                            {
                                multiplier = 1.5f;
                            }
                        }
                        // Add the point of that swing
                        points.Add(GetDistance(note, lastNote, bpm, lastCutDirection) * multiplier / 2);
                    }
                }
                else
                {
                    points.Add(1);
                }

                lastNote = note;
                if((int)lastNote.cutDirection != 8)
                {
                    lastCutDirection = (int)lastNote.cutDirection;
                }
                else if(!pattern)
                {
                    lastCutDirection = ReverseCutDirection(lastCutDirection);
                }
            }

            return points;
        }

        public static bool DetectInverted(ColorNoteData now, ColorNoteData last, int type)
        {
            switch (last.cutDirection)
            {
                case NoteCutDirection.Up:
                    return (last.layer < now.layer && type == 0) || (last.layer > now.layer && type == 2);
                case NoteCutDirection.Down:
                    return (last.layer > now.layer && type == 0) || (last.layer < now.layer && type == 2);
                case NoteCutDirection.Left:
                    return (last.line > now.line && type == 0) || (last.line < now.line && type == 2);
                case NoteCutDirection.Right:
                    return (last.line < now.line && type == 0) || (last.line > now.line && type == 2);
                case NoteCutDirection.UpLeft:
                    return (last.layer < now.layer && type == 0) || (last.layer > now.layer && type == 2) || (last.line > now.line && type == 0) || (last.line < now.line && type == 2);
                case NoteCutDirection.UpRight:
                    return (last.layer < now.layer && type == 0) || (last.layer > now.layer && type == 2) || (last.line < now.line && type == 0) || (last.line > now.line && type == 2);
                case NoteCutDirection.DownLeft:
                    return (last.layer > now.layer && type == 0) || (last.layer < now.layer && type == 2) || (last.line > now.line && type == 0) || (last.line < now.line && type == 2);
                case NoteCutDirection.DownRight:
                    return (last.layer > now.layer && type == 0) || (last.layer < now.layer && type == 2) || (last.line < now.line && type == 0) || (last.line > now.line && type == 2);
                default:
                    return false;
            }
        }

        public static int GetTypeOfSwing(int nowCutDirection, int lastCutDirection)
        {
            if (nowCutDirection == 8)
            {
                return 0;
            }

            int direction = 0;

            switch (lastCutDirection)
            {
                case 0:
                    {
                        switch (nowCutDirection)
                        {
                            case 0:
                                direction = 2;
                                break;
                            case 1:
                                direction = 0;
                                break;
                            case 2:
                                direction = 1;
                                break;
                            case 3:
                                direction = 1;
                                break;
                            case 4:
                                direction = 2;
                                break;
                            case 5:
                                direction = 2;
                                break;
                            case 6:
                                direction = 3;
                                break;
                            case 7:
                                direction = 3;
                                break;
                        }
                        break;
                    }
                case 1:
                    {
                        switch (nowCutDirection)
                        {
                            case 0:
                                direction = 0;
                                break;
                            case 1:
                                direction = 2;
                                break;
                            case 2:
                                direction = 1;
                                break;
                            case 3:
                                direction = 1;
                                break;
                            case 4:
                                direction = 3;
                                break;
                            case 5:
                                direction = 3;
                                break;
                            case 6:
                                direction = 2;
                                break;
                            case 7:
                                direction = 2;
                                break;
                        }
                        break;
                    }
                case 2:
                    {
                        switch (nowCutDirection)
                        {
                            case 0:
                                direction = 1;
                                break;
                            case 1:
                                direction = 1;
                                break;
                            case 2:
                                direction = 2;
                                break;
                            case 3:
                                direction = 0;
                                break;
                            case 4:
                                direction = 2;
                                break;
                            case 5:
                                direction = 3;
                                break;
                            case 6:
                                direction = 2;
                                break;
                            case 7:
                                direction = 3;
                                break;
                        }
                        break;
                    }
                case 3:
                    {
                        switch (nowCutDirection)
                        {
                            case 0:
                                direction = 1;
                                break;
                            case 1:
                                direction = 1;
                                break;
                            case 2:
                                direction = 0;
                                break;
                            case 3:
                                direction = 2;
                                break;
                            case 4:
                                direction = 3;
                                break;
                            case 5:
                                direction = 2;
                                break;
                            case 6:
                                direction = 3;
                                break;
                            case 7:
                                direction = 2;
                                break;
                        }
                        break;
                    }
                case 4:
                    {
                        switch (nowCutDirection)
                        {
                            case 0:
                                direction = 2;
                                break;
                            case 1:
                                direction = 3;
                                break;
                            case 2:
                                direction = 2;
                                break;
                            case 3:
                                direction = 3;
                                break;
                            case 4:
                                direction = 2;
                                break;
                            case 5:
                                direction = 1;
                                break;
                            case 6:
                                direction = 1;
                                break;
                            case 7:
                                direction = 0;
                                break;
                        }
                        break;
                    }
                case 5:
                    {
                        switch (nowCutDirection)
                        {
                            case 0:
                                direction = 2;
                                break;
                            case 1:
                                direction = 3;
                                break;
                            case 2:
                                direction = 3;
                                break;
                            case 3:
                                direction = 2;
                                break;
                            case 4:
                                direction = 1;
                                break;
                            case 5:
                                direction = 2;
                                break;
                            case 6:
                                direction = 0;
                                break;
                            case 7:
                                direction = 1;
                                break;
                        }
                        break;
                    }
                case 6:
                    {
                        switch (nowCutDirection)
                        {
                            case 0:
                                direction = 3;
                                break;
                            case 1:
                                direction = 2;
                                break;
                            case 2:
                                direction = 2;
                                break;
                            case 3:
                                direction = 3;
                                break;
                            case 4:
                                direction = 1;
                                break;
                            case 5:
                                direction = 0;
                                break;
                            case 6:
                                direction = 2;
                                break;
                            case 7:
                                direction = 1;
                                break;
                        }
                        break;
                    }
                case 7:
                    {
                        switch (nowCutDirection)
                        {
                            case 0:
                                direction = 3;
                                break;
                            case 1:
                                direction = 2;
                                break;
                            case 2:
                                direction = 3;
                                break;
                            case 3:
                                direction = 2;
                                break;
                            case 4:
                                direction = 0;
                                break;
                            case 5:
                                direction = 1;
                                break;
                            case 6:
                                direction = 1;
                                break;
                            case 7:
                                direction = 2;
                                break;
                        }
                        break;
                    }
            }

            return direction;
        }

        public static int ReverseCutDirection(int direction)
        {
            return direction switch
            {
                0 => 1,
                1 => 0,
                2 => 3,
                3 => 2,
                4 => 7,
                5 => 6,
                6 => 5,
                7 => 4,
                _ => 8,
            };
        }

        public static float GetActiveSecond(List<ColorNoteData> notes, float bpm)
        {
            var beat = 0f;
            ColorNoteData lastNote = notes[0];

            foreach(var note in notes)
            {
                // Only calculate if there's more than one note every two beats
                if(note.beat - lastNote.beat <= 4)
                {
                    beat += note.beat - lastNote.beat;
                }
                else 
                {
                    beat += 0.5f;    
                }

                lastNote = note;
            }

            return ConvertBeat(beat, bpm) / 1000;
        }

        public static readonly float Sqr2 = (float)Math.Sqrt(2) / 2;
        public static readonly float[] lineDiff = new float[] { 0, 0, -1, 1, -Sqr2, Sqr2, -Sqr2, Sqr2 };
        public static readonly float[] layerDiff = new float[] { 1, -1, 0, 0, Sqr2, Sqr2, -Sqr2, -Sqr2 };

        public static float GetDistance(ColorNoteData now, ColorNoteData before, float bpm, int lastcut)
        {
            var swingAmount = Math.Pow(ConvertBeat(now.beat - before.beat, bpm) * 1.0 / 1500, 1.0 / 2);
            swingAmount = Math.Max(swingAmount, 2.5);
            var currentX = (float)(before.line + lineDiff[lastcut] * swingAmount);
            var currentY = (float)(before.layer + layerDiff[lastcut] * swingAmount);
            return Vector2.Distance(new Vector2(currentX, currentY), new Vector2(now.line, now.layer));
        }

        public static int ConvertBeat(double beat, float bpm)
        {
            return (int)Math.Round(beat / bpm * 60 * 1000);
        }
    }
}
