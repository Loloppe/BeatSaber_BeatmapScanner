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

		private readonly string[] title = ["Crouch", "Bomb Av.", "Linear", "V3", "EBPM", "BL ⭐", "Pass", "Tech", "SS ⭐"];

		[UIObject("tile-grid")]
		private readonly GameObject _tileGrid;
		[UIObject("tile-row")]
		private readonly GameObject _tileRow;
		[UIComponent("tile")]
		private readonly ClickableImage _tile;

		public static List<ClickableImage> _tiles = null;
		// Cached per-tile FormattableText pairs to avoid repeated GetComponentsInChildren calls
		private static FormattableText[][] _tileTexts = null;


        [Inject]
		internal void Construct(DiContainer diContainer)
		{
			_diContainer = diContainer;
		}

		[UIAction("#post-parse")]
			public void PostParse()
			{
				_tiles = [];

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

				// Cache FormattableText components once to avoid repeated hierarchy traversals in Apply()
				_tileTexts = new FormattableText[_tiles.Count][];
				for (int i = 0; i < _tiles.Count; i++)
				{
					_tileTexts[i] = _tiles[i].transform.GetComponentsInChildren<FormattableText>(true);
					_tileTexts[i][0].text = title[i];
					_tileTexts[i][1].text = "";
				}

				ApplyVisibility();
				DestroyImmediate(_tile.gameObject);
			}

			/// <summary>Applies Show* settings to each tile's active state. Call from PostParse and Settings.Changed.</summary>
			public static void ApplyVisibility()
			{
				if (_tiles == null) return;
				bool[] show = [
					Settings.Instance.ShowCrouch,
					Settings.Instance.ShowBomb,
					Settings.Instance.ShowLinear,
					Settings.Instance.ShowV3,
					Settings.Instance.ShowEBPM,
					Settings.Instance.ShowBL,
					Settings.Instance.ShowPass,
					Settings.Instance.ShowTech,
					Settings.Instance.ShowSS,
				];
				for (int i = 0; i < _tiles.Count; i++)
					_tiles[i].rectTransform.gameObject.SetActive(show[i]);
			}

        public static void Apply(List<double> data)
        {
            for (int i = 0; i < _tileTexts.Length; i++)
            {
                FormattableText[] texts = _tileTexts[i];
                texts[0].color = Settings.Instance.TitleColor;
                texts[1].text = Math.Round(data[i], 2).ToString();
                if (texts[1].text == "0" && i != 0 && i != 2) texts[1].text = "X";
                switch (i)
                {
                    case 3: // V3
                        if (data[i] == 1)
                        {
                            texts[1].text = "✔";
                        }
                        continue;
                    case 6: // Pass
                        if (data[i] >= Settings.Instance.PColorC)
                        {
                            texts[1].color = Settings.Instance.D;
                        }
                        else if (data[i] >= Settings.Instance.PColorB)
                        {
                            texts[1].color = Settings.Instance.C;
                        }
                        else if (data[i] >= Settings.Instance.PColorA)
                        {
                            texts[1].color = Settings.Instance.B;
                        }
                        else
                        {
                            texts[1].color = Settings.Instance.A;
                        }
                        continue;
                    case 7: // Tech
                        if (data[i] >= Settings.Instance.TColorC)
                        {
                            texts[1].color = Settings.Instance.D;
                        }
                        else if (data[i] >= Settings.Instance.TColorB)
                        {
                            texts[1].color = Settings.Instance.C;
                        }
                        else if (data[i] >= Settings.Instance.TColorA)
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
