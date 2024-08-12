using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace BeatmapScanner.UI
{
	[HotReload(RelativePathToLayout = @"Views\gridView.bsml")]
	[ViewDefinition("BeatmapScanner.UI.Views.gridView.bsml")]
	internal class GridViewController : BSMLAutomaticViewController
	{
#pragma warning disable IDE0052 // Remove unread private members
		private DiContainer _diContainer;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly string[] title = ["V3", "EBPM", "BL ⭐", "Pass", "Tech", "SS ⭐"];

        [UIObject("tile-grid")]
		private readonly GameObject _tileGrid;
		[UIObject("tile-row")]
		private readonly GameObject _tileRow;
		[UIComponent("tile")]
		private readonly ClickableImage _tile;

		public static List<ClickableImage> _tiles = null;


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

			for (int i = 0; i < 1; i++)
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

			if (Settings.Instance.ShowV3)
			{
				_tiles[0].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[0].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowEBPM)
			{
				_tiles[1].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[1].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowBL)
			{
				_tiles[2].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[2].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowPass)
			{
				_tiles[3].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[3].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowTech)
			{
				_tiles[4].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[4].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowSS)
			{
				_tiles[5].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[5].rectTransform.gameObject.SetActive(false);
			}

            DestroyImmediate(_tile.gameObject);
		}

        public static void Apply(List<double> data)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                FormattableText[] texts = _tiles[i].transform.GetComponentsInChildren<FormattableText>(true);
                texts[0].color = Settings.Instance.TitleColor;
                texts[1].text = Math.Round(data[i], 2).ToString();
                if (texts[1].text == "0" && i != 0) texts[1].text = "X";
                switch (i)
                {
                    case 0: // V3
                        if (data[i] == 1)
                        {
                            texts[1].text = "X";
                        }
                        continue;
                    case 3: // Pass
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
                    case 4: // Tech
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
