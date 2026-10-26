using System.Runtime.CompilerServices;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores;
using BeatmapScanner.UI;
using BeatmapScanner.Utils;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BeatmapScanner
{
    internal class Settings
    {
        public static Settings Instance;
        public virtual bool Enabled { get; set; } = true;
        public virtual bool AllowMoving { get; set; } = true;
        public virtual bool ShowDataScreen { get; set; } = true;
        public virtual bool ShowPassGraph { get; set; } = true;
        public virtual bool ShowTechGraph { get; set; } = true;
        public virtual bool ShowPass { get; set; } = true;
        public virtual bool ShowTech { get; set; } = true;
        public virtual bool ShowSS { get; set; } = true;
        public virtual bool ShowEBPM { get; set; } = true;
        public virtual bool ShowBL { get; set; } = true;
        public virtual bool ShowV3 { get; set; } = true;
        public virtual bool ShowBomb { get; set; } = true;
        public virtual bool ShowLinear { get; set; } = true;
        public virtual bool ShowCrouch { get; set; } = true;
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
        public virtual float PColorA { get; set; } = 6f;
        public virtual float PColorB { get; set; } = 9f;
        public virtual float PColorC { get; set; } = 12f;
        public virtual float TColorA { get; set; } = 5f;
        public virtual float TColorB { get; set; } = 7f;
        public virtual float TColorC { get; set; } = 9f;
        public virtual Vector3 UIPosition { get; set; } = new(1.9f, 2.6f, 3.7f);
        public virtual Quaternion UIRotation { get; set; } = Quaternion.Euler(new Vector3(350, 28, 360));
        public virtual Vector3 SwingDiffGraphPosition { get; set; } = new(-1.2f, 0.2f, 2.0f);
        public virtual Quaternion SwingDiffGraphRotation { get; set; } = Quaternion.Euler(new Vector3(75, -20, 0));
        public virtual Vector3 SwingTechGraphPosition { get; set; } = new(1.2f, 0.2f, 2.0f);
        public virtual Quaternion SwingTechGraphRotation { get; set; } = Quaternion.Euler(new Vector3(75, 20, 0));

        /// <summary>
        /// Called by BSIPA whenever a value is changed (including from the settings UI).
        /// </summary>
        public virtual void Changed() => UICreator.ApplySettings();
    }
}
