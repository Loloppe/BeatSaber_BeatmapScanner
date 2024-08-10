using System.Collections.Generic;

namespace BeatmapScanner.Utils
{
    internal class EBPM
    {
        public static float GetEBPM(List<NoteData> notes, float bpm)
        {
            var previous = 0f;
            var effectiveBPM = 10f;
            var peakBPM = 10f;
            var count = 0;
            List<float> timestamps = [];
            var bps = bpm / 60;
            foreach (var note in notes)
            {
                timestamps.Add(bps * note.time);
            }

            for (int i = 1; i < timestamps.Count; i++)
            {
                if (timestamps[i] - timestamps[i - 1] <= 0.125)
                {
                    continue;
                }

                var duration = (timestamps[i] - timestamps[i - 1]);

                if (duration > 0)
                {
                    if (previous >= duration - 0.01 && previous <= duration + 0.01 && duration < effectiveBPM)
                    {
                        count++;
                        if (count >= Settings.Instance.EBPM)
                        {
                            effectiveBPM = duration;
                        }
                    }
                    else
                    {
                        count = 0;
                    }

                    if (duration < peakBPM)
                    {
                        peakBPM = duration;
                    }

                    previous = duration;
                }
            }

            if (effectiveBPM == 10)
            {
                return bpm;
            }

            effectiveBPM = 0.5f / effectiveBPM * bpm;

            return effectiveBPM;
        }

    }
}
