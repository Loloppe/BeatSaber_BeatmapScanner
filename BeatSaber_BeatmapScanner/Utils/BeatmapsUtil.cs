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
    }
}
