using Analyzer.BeatmapScanner.Data;

namespace Analyzer.BeatmapScanner.Helper
{
    internal class IsSlider
    {
        public static bool SliderCond(Cube prev, Cube next, (double x, double y) sim, float bpm, float njs)
        {
            if(prev.CutDirection == 8)
            {
                if (next.Time - prev.Time <= 0.125)
                {
                    if (prev.Line == next.Line && prev.Layer == next.Layer && next.CutDirection == 8) return true;
                    if (IsSlid(sim.x, sim.y, next.Line, next.Layer, prev.Direction)) return true;
                }
                if ((next.Time - prev.Time) / (bpm / 60) * njs <= 1 && next.CutDirection == 8) return true;
                return false;
            }

            if (IsSlid(prev.Line, prev.Layer, next.Line, next.Layer, prev.Direction)) return true;
            return false;
        }

        public static bool IsSlid(double x1, double y1, double x2, double y2, double direction)
        {
            switch (direction)
            {
                case double d when d > 67.5 && d <= 112.5:
                    if (y1 < y2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 247.5 && d <= 292.5:
                    if (y1 > y2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 157.5 && d <= 202.5:
                    if (x1 > x2)
                    {
                        return true;
                    }
                    break;
                case double d when d <= 22.5 && d >= 0 || d > 337.5 && d < 360:
                    if (x1 < x2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 112.5 && d <= 157.5:
                    if (y1 < y2)
                    {
                        return true;
                    }
                    if (x1 > x2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 22.5 && d <= 67.5:
                    if (y1 < y2)
                    {
                        return true;
                    }
                    if (x1 < x2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 202.5 && d <= 247.5:
                    if (y1 > y2)
                    {
                        return true;
                    }
                    if (x1 > x2)
                    {
                        return true;
                    }
                    break;
                case double d when d > 292.5 && d <= 337.5:
                    if (y1 > y2)
                    {
                        return true;
                    }
                    if (x1 < x2)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}
