using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BeatmapScanner.Utils
{
	/// <summary>Analysis output for one difficulty at one song speed.</summary>
	internal sealed class AnalysisResult
	{
		/// <summary>Indexed by GridViewController tile. SS/BL stars are not stored, they are always read live from SongDetailsCache.</summary>
		public double[] Values;
		public float[] Times;
		public float[] Diffs;
		public float[] Techs;
	}

	/// <summary>
	/// Persistent cache of analysis results, stored as one file per map hash in UserData\BeatmapScanner\Cache.
	/// The whole cache is discarded when the bundled Analyzer or Parser changes (see the AnalyzerHash build step).
	/// An updated map gets a new hash, so it never hits a stale entry.
	/// </summary>
	internal static class AnalysisCache
	{
		// Bump when the file layout or the meaning of the stored values changes
		private const int FormatVersion = 1;
		private const int MaxMapsInMemory = 50;
		private const int MaxArrayLength = 1_000_000;

		private static readonly string CacheDir = Path.Combine(UnityGame.UserDataPath, "BeatmapScanner", "Cache");
		// Map hash -> (cache key -> result). Only accessed under lock (Memory).
		private static readonly Dictionary<string, Dictionary<string, AnalysisResult>> Memory = new();
		// Serializes all cache file reads and writes
		private static readonly SemaphoreSlim Io = new(1, 1);
		private static Task<bool> _diskReady = Task.FromResult(false);

		public static void Initialize()
		{
			var analyzerHash = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "AnalyzerHash")?.Value;
			if (string.IsNullOrEmpty(analyzerHash))
			{
				Plugin.Log.Warn("Analyzer hash missing from build, analysis results will only be cached in memory");
				return;
			}
			_diskReady = Task.Run(() => PrepareDirectory($"{FormatVersion}|{analyzerHash}"));
		}

		public static string MakeKey(string characteristic, string difficulty, GameplayModifiers.SongSpeed songSpeed) =>
			$"{characteristic}|{difficulty}|{songSpeed}";

		/// <summary>Only proper map hashes are cached, since they are used as file names.</summary>
		public static bool IsCacheable(string hash) =>
			!string.IsNullOrEmpty(hash) && hash.All(Uri.IsHexDigit);

		public static async Task<AnalysisResult> TryGetAsync(string hash, string key)
		{
			Dictionary<string, AnalysisResult> entries;
			lock (Memory) Memory.TryGetValue(hash, out entries);
			entries ??= await Task.Run(async () =>
			{
				bool diskReady = await _diskReady;
				await Io.WaitAsync();
				try { return GetOrLoadMap(hash, diskReady); }
				finally { Io.Release(); }
			});
			lock (Memory) return entries.TryGetValue(key, out var result) ? result : null;
		}

		/// <summary>Stores a result in memory and writes the map's cache file in the background.</summary>
		public static void Save(string hash, string key, AnalysisResult result)
		{
			Task.Run(async () =>
			{
				bool diskReady = await _diskReady;
				await Io.WaitAsync();
				try
				{
					// Loads the existing file first so its other entries are kept
					var entries = GetOrLoadMap(hash, diskReady);
					List<KeyValuePair<string, AnalysisResult>> snapshot;
					lock (Memory)
					{
						entries[key] = result;
						snapshot = entries.ToList();
					}
					if (diskReady) WriteMap(hash, snapshot);
				}
				catch (Exception ex)
				{
					Plugin.Log.Error($"Could not save analysis cache for {hash}: {ex}");
				}
				finally
				{
					Io.Release();
				}
			});
		}

		/// <summary>Ensures the cache folder exists, and wipes it if it was written by a different Analyzer/Parser.</summary>
		private static bool PrepareDirectory(string identity)
		{
			try
			{
				var versionPath = Path.Combine(CacheDir, "version.txt");
				if (Directory.Exists(CacheDir) && (!File.Exists(versionPath) || File.ReadAllText(versionPath) != identity))
				{
					Plugin.Log.Info("Analyzer changed, clearing the analysis cache");
					Directory.Delete(CacheDir, true);
				}
				Directory.CreateDirectory(CacheDir);
				if (!File.Exists(versionPath)) File.WriteAllText(versionPath, identity);
				return true;
			}
			catch (Exception ex)
			{
				Plugin.Log.Error($"Could not prepare the analysis cache, results will only be cached in memory: {ex}");
				return false;
			}
		}

		/// <summary>Returns the in-memory entries for a map, reading its cache file on first access. Caller must hold <see cref="Io"/>.</summary>
		private static Dictionary<string, AnalysisResult> GetOrLoadMap(string hash, bool diskReady)
		{
			lock (Memory)
				if (Memory.TryGetValue(hash, out var cached)) return cached;

			var entries = diskReady ? ReadMap(hash) : new Dictionary<string, AnalysisResult>();
			lock (Memory)
			{
				if (Memory.Count >= MaxMapsInMemory) Memory.Clear();
				Memory[hash] = entries;
			}
			return entries;
		}

		private static string GetPath(string hash) => Path.Combine(CacheDir, hash.ToUpperInvariant() + ".dat");

		private static Dictionary<string, AnalysisResult> ReadMap(string hash)
		{
			var entries = new Dictionary<string, AnalysisResult>();
			var path = GetPath(hash);
			if (!File.Exists(path)) return entries;
			try
			{
				using var reader = new BinaryReader(File.OpenRead(path));
				if (reader.ReadInt32() != FormatVersion) return entries;
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					string key = reader.ReadString();
					entries[key] = new AnalysisResult
					{
						Values = ReadDoubles(reader),
						Times = ReadFloats(reader),
						Diffs = ReadFloats(reader),
						Techs = ReadFloats(reader),
					};
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.Warn($"Ignoring unreadable cache file {path}: {ex.Message}");
				entries.Clear();
			}
			return entries;
		}

		private static void WriteMap(string hash, List<KeyValuePair<string, AnalysisResult>> entries)
		{
			var path = GetPath(hash);
			var tmp = path + ".tmp";
			using (var writer = new BinaryWriter(File.Create(tmp)))
			{
				writer.Write(FormatVersion);
				writer.Write(entries.Count);
				foreach (var entry in entries)
				{
					writer.Write(entry.Key);
					WriteDoubles(writer, entry.Value.Values);
					WriteFloats(writer, entry.Value.Times);
					WriteFloats(writer, entry.Value.Diffs);
					WriteFloats(writer, entry.Value.Techs);
				}
			}
			// Write-then-swap so a crash mid-write never leaves a truncated file behind
			File.Delete(path);
			File.Move(tmp, path);
		}

		private static int ReadLength(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length < 0 || length > MaxArrayLength) throw new InvalidDataException($"Invalid array length {length}");
			return length;
		}

		private static double[] ReadDoubles(BinaryReader reader)
		{
			var array = new double[ReadLength(reader)];
			for (int i = 0; i < array.Length; i++) array[i] = reader.ReadDouble();
			return array;
		}

		private static float[] ReadFloats(BinaryReader reader)
		{
			var array = new float[ReadLength(reader)];
			for (int i = 0; i < array.Length; i++) array[i] = reader.ReadSingle();
			return array;
		}

		private static void WriteDoubles(BinaryWriter writer, double[] array)
		{
			writer.Write(array.Length);
			foreach (var v in array) writer.Write(v);
		}

		private static void WriteFloats(BinaryWriter writer, float[] array)
		{
			writer.Write(array.Length);
			foreach (var v in array) writer.Write(v);
		}
	}
}
