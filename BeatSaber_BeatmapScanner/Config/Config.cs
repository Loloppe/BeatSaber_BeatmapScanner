using System.Runtime.CompilerServices;
using IPA.Config.Stores.Attributes;
using BeatmapScanner.Algorithm;
using BeatmapScanner.Patches;
using IPA.Config.Stores;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BeatmapScanner
{
    internal class Config
    {
        public static Config Instance;
        public virtual bool ImageCoverExpander { get; set; } = true;
        public virtual bool OldValue { get; set; } = false;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color A { get; set; } = new Color(0.62f, 0.62f, 0.62f);
        [UseConverter(typeof(ColorConverter))]
        public virtual Color B { get; set; } = Color.yellow;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color C { get; set; } = Color.green;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color D { get; set; } = new Color(1f, 0f, 1f);
        public virtual float DColorA { get; set; } = 5f;
        public virtual float DColorB { get; set; } = 7f;
        public virtual float DColorC { get; set; } = 9f;
        public virtual float TColorA { get; set; } = 0.2f;
        public virtual float TColorB { get; set; } = 0.3f;
        public virtual float TColorC { get; set; } = 0.4f;
        
        

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
            if(SoloMenuPatch.Instance != null && ((ImageCoverExpander && !SoloMenuPatch.ImageCover) || (!ImageCoverExpander && SoloMenuPatch.ImageCover))) // Reload cover
            {
                SoloMenuPatch.Instance.ShowContent((StandardLevelDetailViewController.ContentType)1); // This reload SS leaderboard for some reason, probably no cache for that (yet)
            }

            if(SoloMenuPatch.HoverTXT.Count > 0) // Reload text
            {
                SoloMenuPatch.ApplyText();
                SoloMenuPatch.ApplyColor();
            }
            
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
