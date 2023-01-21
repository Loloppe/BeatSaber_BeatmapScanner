using BeatmapScanner.HarmonyPatches;
using System.Runtime.CompilerServices;
using IPA.Config.Stores.Attributes;
using BeatmapScanner.Algorithm;
using IPA.Config.Stores;
using BeatmapScanner.UI;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BeatmapScanner
{
    internal class Settings
    {
        public static Settings Instance;
        public virtual bool Enabled { get; set; } = true;
        public virtual bool ShowHandle { get; set; } = false;
        public virtual bool ImageCoverExpander { get; set; } = false;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color TitleColor { get; set; } = Color.white;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color A { get; set; } = Color.white;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color B { get; set; } = Color.yellow;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color C { get; set; } = Color.green;
        [UseConverter(typeof(ColorConverter))]
        public virtual Color D { get; set; } = new(1f, 0f, 1f);
        public virtual float DColorA { get; set; } = 5f;
        public virtual float DColorB { get; set; } = 7f;
        public virtual float DColorC { get; set; } = 9f;
        public virtual float TColorA { get; set; } = 0.2f;
        public virtual float TColorB { get; set; } = 0.3f;
        public virtual float TColorC { get; set; } = 0.4f;
        public virtual Vector3 UIPosition { get; set; } = new(2f, 2.9f, 3.7f);
        public virtual Quaternion UIRotation { get; set; } = Quaternion.Euler(new Vector3(350, 28, 360));


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
            if (ICEPatch.Instance != null && ((ImageCoverExpander && !ICEPatch.ImageCover) || (!ImageCoverExpander && ICEPatch.ImageCover))) // Reload cover
            {
                ICEPatch.Instance.ShowContent((StandardLevelDetailViewController.ContentType)1);
            }

            if (UICreator._floatingScreen != null) 
            {
                if (Enabled)
                {
                    UICreator._floatingScreen.ShowHandle = ShowHandle;
                    GridViewController.Apply(); // Reload text
                    GridViewController.Show(); // Show
                }
                else
                {
                    GridViewController.Hide(); // Hide
                }
            }
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(Settings other)
        {
            // This instance's members populated from other
        }
    }
}
