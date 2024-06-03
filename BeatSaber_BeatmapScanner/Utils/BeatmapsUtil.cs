namespace BeatmapScanner.Utils
{
    // Source: https://github.com/kinsi55/BeatSaber_BetterSongList/blob/master/Util/BeatmapsUtil.cs
    static class BeatmapsUtil
    {
        public static string GetHashOfLevel(BeatmapLevel level)
        {
            return level == null ? null : GetHashOfLevelId(level.levelID);
        }

        private static string GetHashOfLevelId(string id)
        {
            if (id.Length < 53)
                return null;

            if (id[12] != '_') // custom_level_<hash, 40 chars>
                return null;

            return id.Substring(13, 40);
        }

        public static int GetCharacteristicFromDifficulty(BeatmapKey diff)
        {
            var d = diff.beatmapCharacteristic?.sortingOrder;

            if (d == null || d > 4)
                return 0;

            // 360 and 90 are "flipped" as far as the enum goes
            if (d == 3)
                d = 4;
            else if (d == 4)
                d = 3;

            return (int)d + 1;
        }
    }
}
