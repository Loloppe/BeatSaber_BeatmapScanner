using System.Collections.Generic;
using UnityEngine;
using static BeatmapSaveDataVersion3.BeatmapSaveData;

namespace BeatmapScanner.Algorithm
{
    internal class Helper
    {
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
                    Vector2 bn = MathUtil.Bernstein(N, i, t) * controlPoints[i];
                    p += bn;
                }
                points.Add(p);
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
    }
}
