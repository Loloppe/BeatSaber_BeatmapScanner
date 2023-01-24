using static BeatmapScanner.HarmonyPatches.BSPatch;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System;

namespace BeatmapScanner.UI
{
	[HotReload(RelativePathToLayout = @"Views\gridView.bsml")]
	[ViewDefinition("BeatmapScanner.UI.Views.gridView.bsml")]
	internal class GridViewController : BSMLAutomaticViewController
	{
#pragma warning disable IDE0052 // Remove unread private members
        private DiContainer _diContainer;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly string[] title = { "Crouch", "Reset", "V3", "Peak BPM", "Slider", "BL ⭐", "Difficulty", "Tech", "SS ⭐" };

		[UIObject("tile-grid")]
		private readonly GameObject _tileGrid;
		[UIObject("tile-row")]
		private readonly GameObject _tileRow;
		[UIComponent("tile")]
		private readonly ClickableImage _tile;

		public static List<ClickableImage> _tiles = new();


		[Inject]
		internal void Construct(DiContainer diContainer)
		{
			_diContainer = diContainer;
		}

		[UIAction("#post-parse")]
		public void PostParse()
		{
			_tiles = new List<ClickableImage>();

			for (int i = 0; i < 3; i++)
			{
				ClickableImage tileInstance = Instantiate(_tile.gameObject, _tileRow.transform).GetComponent<ClickableImage>();
				_tiles.Add(tileInstance);
			}

			for (int i = 0; i < 2; i++)
			{
				GameObject tileRowInstance = Instantiate(_tileRow, _tileGrid.transform);
				tileRowInstance.transform.SetAsFirstSibling();
				_tiles.AddRange(tileRowInstance.GetComponentsInChildren<ClickableImage>());
			}

			for (int i = 0; i < _tiles.Count; i++)
			{
				FormattableText[] texts = _tiles[i].transform.GetComponentsInChildren<FormattableText>(true);

				texts[0].text = title[i];
				texts[1].text = "";
			}

			DestroyImmediate(_tile.gameObject);
		}

		public static void Show()
		{
			UICreator._floatingScreen.gameObject.SetActive(true);
		}

		public static void Hide()
        {
			UICreator._floatingScreen.gameObject.SetActive(false);
		}

		public static void Apply()
		{
			for (int i = 0; i < _tiles.Count; i++)
			{
				FormattableText[] texts = _tiles[i].transform.GetComponentsInChildren<FormattableText>(true);
				texts[0].color = Settings.Instance.TitleColor;
				texts[1].text = "";

				switch (i)
                {
					case 0: // Crouch
						if (Crouch == -1)
						{
							texts[1].text = "X";
						}
						else
						{
							texts[1].text = Crouch.ToString();
						}

						continue;
					case 1: // Reset
						if (Reset == -1)
						{
							texts[1].text = "X";
						}
						else
						{
							texts[1].text = Reset.ToString();
						}

						continue;
					case 2: // V3
						texts[1].text = V3.ToString();

						continue;
					case 3: // Peak BPM
                        texts[1].text = Math.Round(EBPM).ToString();
						continue;
					case 4: // Slider
						texts[1].text = Slider.ToString();

						continue;
					case 5: // BL *
						if (BL == -1)
						{
							texts[1].text = "X";
						}
						else
						{
							texts[1].text = BL.ToString();
						}

						if (BL >= Settings.Instance.DColorC)
						{
							texts[1].color = Settings.Instance.D;
						}
						else if (BL >= Settings.Instance.DColorB)
						{
							texts[1].color = Settings.Instance.C;
						}
						else if (BL >= Settings.Instance.DColorA)
						{
							texts[1].color = Settings.Instance.B;
						}
						else
						{
							texts[1].color = Settings.Instance.A;
						}
						continue;
					case 6: // Diff
						texts[1].text = Math.Round(Diff, 2).ToString();

						if (Diff >= Settings.Instance.DColorC)
						{
							texts[1].color = Settings.Instance.D;
						}
						else if (Diff >= Settings.Instance.DColorB)
						{
							texts[1].color = Settings.Instance.C;
						}
						else if (Diff >= Settings.Instance.DColorA)
						{
							texts[1].color = Settings.Instance.B;
						}
						else
						{
							texts[1].color = Settings.Instance.A;
						}
						continue;
					case 7: // Tech
						texts[1].text = Math.Round(Tech, 2).ToString();

						if (Tech >= Settings.Instance.TColorC)
						{
							texts[1].color = Settings.Instance.D;
						}
						else if (Tech >= Settings.Instance.TColorB)
						{
							texts[1].color = Settings.Instance.C;
						}
						else if (Tech >= Settings.Instance.TColorA)
						{
							texts[1].color = Settings.Instance.B;
						}
						else
						{
							texts[1].color = Settings.Instance.A;
						}
						continue;
					case 8: // SS *
						if(SS == -1)
                        {
							texts[1].text = "X";
						}
						else
                        {
							texts[1].text = SS.ToString();
						}

						if (SS >= Settings.Instance.DColorC)
						{
							texts[1].color = Settings.Instance.D;
						}
						else if (SS >= Settings.Instance.DColorB)
						{
							texts[1].color = Settings.Instance.C;
						}
						else if (SS >= Settings.Instance.DColorA)
						{
							texts[1].color = Settings.Instance.B;
						}
						else
						{
							texts[1].color = Settings.Instance.A;
						}
						continue;
				}
			}
		}
	}
}
