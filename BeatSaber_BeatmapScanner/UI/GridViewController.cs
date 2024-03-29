﻿using static BeatmapScanner.HarmonyPatches.BSPatch;
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

		private readonly string[] title = { "Linear", "Crouch", "V3", "Pattern", "EBPM", "BL ⭐", "Pass", "Tech", "SS ⭐" };

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

			if (Settings.Instance.ShowLinear)

			{
				_tiles[0].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[0].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowCrouch)
			{
				_tiles[1].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[1].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowV3)
			{
				_tiles[2].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[2].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowPattern)
			{
				_tiles[3].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[3].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowEBPM)
			{
				_tiles[4].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[4].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowBL)
			{
				_tiles[5].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[5].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowPass)
			{
				_tiles[6].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[6].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowTech)
			{
				_tiles[7].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[7].rectTransform.gameObject.SetActive(false);
			}
			if (Settings.Instance.ShowSS)
			{
				_tiles[8].rectTransform.gameObject.SetActive(true);
			}
			else
			{
				_tiles[8].rectTransform.gameObject.SetActive(false);
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
					case 0: // Linear
						if (Linear == 0)
						{
							texts[1].text = "X";
						}
						else
						{
							texts[1].text = Math.Round(Linear, 2).ToString();
						}

						continue;
					case 1: // Crouch
                        if (Crouch == 0)
                        {
                            texts[1].text = "X";
                        }
                        else
                        {
                            texts[1].text = Crouch.ToString();
                        }

						continue;
					case 2: // V3
                        if (V3 == 1)
                        {
                            texts[1].text = "O";
                        }
                        else
                        {
                            texts[1].text = "X";
                        }

                        continue;
					case 3: // Pattern
                        texts[1].text = Math.Round(Pattern, 2).ToString();
                        
						continue;
					case 4: // EBPM
                        texts[1].text = Math.Round(EBPM).ToString();

                        continue;
					case 5: // BL *
						if (BL == 0)
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
						if(Pass == 0)
                        {
							texts[1].text = "X";
						}
						else
                        {
							texts[1].text = Math.Round(Pass, 2).ToString();
						}

						if (Pass >= Settings.Instance.DColorC)
						{
							texts[1].color = Settings.Instance.D;
						}
						else if (Pass >= Settings.Instance.DColorB)
						{
							texts[1].color = Settings.Instance.C;
						}
						else if (Pass >= Settings.Instance.DColorA)
						{
							texts[1].color = Settings.Instance.B;
						}
						else
						{
							texts[1].color = Settings.Instance.A;
						}
						continue;
					case 7: // Tech
						if(Tech == 0)
                        {
							texts[1].text = "X";
						}
						else
                        {
							texts[1].text = Math.Round(Tech, 2).ToString();
						}

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
						if (SS == 0)
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
