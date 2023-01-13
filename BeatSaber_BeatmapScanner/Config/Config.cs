using System.Runtime.CompilerServices;
using BeatmapScanner.Algorithm;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BeatmapScanner
{
    internal class Config
    {
        public static Config Instance;
        public virtual bool Enabled { get; set; } = true;
        public virtual bool ImageCoverExpander { get; set; } = true;
        public virtual bool StarLimiter { get; set; } = true;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color A { get; set; } = new Color(0.62f, 0.62f, 0.62f);
        [UseConverter(typeof(ColorConverter))]
        public virtual Color B { get; set; } = Color.yellow;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color C { get; set; } = Color.green;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color D { get; set; } = Color.red;
        
        /// <summary>
		/// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
		/// </summary>
		public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(Config other)
        {
            // This instance's members populated from other
        }
    }
}
