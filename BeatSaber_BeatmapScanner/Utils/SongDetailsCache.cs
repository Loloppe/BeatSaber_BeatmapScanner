using System.Threading.Tasks;
using SongDetailsCache;

namespace BeatmapScanner.Utils
{
	static class SongDetailsUtil
	{
		public class AntiBox
		{
			public readonly SongDetails instance;

			public AntiBox(SongDetails instance)
			{
				this.instance = instance;
			}
		}

		public static bool FinishedInitAttempt { get; private set; } = false;
		public static bool AttemptedToInit { get; private set; } = false;

		static bool CheckAvailable()
		{
			var v = IPA.Loader.PluginManager.GetPluginFromId("SongDetailsCache");

			if (v == null)
				return false;

			return v.HVersion >= new Hive.Versioning.Version("1.1.5");
		}

		private static bool? _isAvailableCached = null;
		public static bool IsAvailable => _isAvailableCached ??= CheckAvailable();
		//public static object instance { get; private set; }
		public static AntiBox songDetails = null;

		public static async Task<AntiBox> TryGet()
		{
			if (!FinishedInitAttempt)
			{
				AttemptedToInit = true;
				try
				{
					if (IsAvailable)
						return songDetails = new AntiBox(await SongDetails.Init());
				}
				catch { }
				finally
				{
					FinishedInitAttempt = true;
				}
			}
			return songDetails;
		}
	}
}
