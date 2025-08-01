﻿using BeatmapScanner.HarmonyPatches;
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
        public virtual float EBPM { get; set; } = 4;
        public virtual bool ShowPass { get; set; } = true;
        public virtual bool ShowTech { get; set; } = true;
        public virtual bool ShowSS { get; set; } = true;
        public virtual bool ShowEBPM { get; set; } = true;
        public virtual bool ShowBL { get; set; } = true;
        public virtual bool ShowV3 { get; set; } = true;
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
            if (UICreator._floatingScreen != null) 
            {
                if (Enabled)
                {
                    UICreator._floatingScreen.ShowHandle = ShowHandle;
                    if (ShowCrouch)
                    {
                        GridViewController._tiles[0].rectTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        GridViewController._tiles[0].rectTransform.gameObject.SetActive(false);
                    }
                    if (ShowV3)
                    {
                        GridViewController._tiles[3].rectTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        GridViewController._tiles[3].rectTransform.gameObject.SetActive(false);
                    }
                    if (ShowEBPM)
                    {
                        GridViewController._tiles[4].rectTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        GridViewController._tiles[4].rectTransform.gameObject.SetActive(false);
                    }
                    if (ShowBL)
                    {
                        GridViewController._tiles[5].rectTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        GridViewController._tiles[5].rectTransform.gameObject.SetActive(false);
                    }
                    if (ShowPass)
                    {
                        GridViewController._tiles[6].rectTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        GridViewController._tiles[6].rectTransform.gameObject.SetActive(false);
                    }
                    if (ShowTech)
                    {
                        GridViewController._tiles[7].rectTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        GridViewController._tiles[7].rectTransform.gameObject.SetActive(false);
                    }
                    if (ShowSS)
                    {
                        GridViewController._tiles[8].rectTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        GridViewController._tiles[8].rectTransform.gameObject.SetActive(false);
                    }
                }
                else
                {
                    UICreator._floatingScreen.gameObject.SetActive(false);
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
