using Analyzer.BeatmapScanner.Data;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.BeatmapScanner.Algorithm
{
    internal class Linear
    {
        public static bool IsInLinearPath(SwingData prev, SwingData curr, SwingData nxt)
        {
            var dxc = nxt.EntryPosition.x - prev.EntryPosition.x;
            var dyc = nxt.EntryPosition.y - prev.EntryPosition.y;
            var dxl = curr.EntryPosition.x - prev.EntryPosition.x;
            var dyl = curr.EntryPosition.y - prev.EntryPosition.y;
            var cross = dxc * dyl - dyc * dxl;
            if (cross == 0) return true;
            return false;
        }

        public static void CalculateLinear(List<SwingData> swings)
        {
            swings[0].Linear = true;
            swings[1].Linear = true;

            for (int i = 2; i < swings.Count; i++)
            {
                if (IsInLinearPath(swings[i - 2], swings[i - 1], swings[i]))
                {
                    swings[i].Linear = true;
                }
            }
        }
    }
}
