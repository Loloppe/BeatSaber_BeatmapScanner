using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace BeatmapScanner.UI
{
	[HotReload(RelativePathToLayout = @"Views\gridView.bsml")]
	[ViewDefinition("BeatmapScanner.UI.Views.gridView.bsml")]
	internal class GridViewController : BSMLAutomaticViewController
	{
		// Tile indices, shared with the data array filled by BSPatch
		public const int Crouch = 0, Bomb = 1, Linear = 2, V3 = 3, EBPM = 4, BL = 5, Pass = 6, Tech = 7, SS = 8;
		public const int TileCount = 9;

		private static readonly string[] Titles = ["Crouch", "Bomb Av.", "Linear", "V3", "EBPM", "BL ⭐", "Pass", "Tech", "SS ⭐"];

#pragma warning disable CS0649 // Assigned by BSML
		[UIObject("tile-grid")]
		private readonly GameObject _tileGrid;
		[UIObject("tile-row")]
		private readonly GameObject _tileRow;
		[UIComponent("tile")]
		private readonly ClickableImage _tile;
#pragma warning restore CS0649

		private static List<ClickableImage> _tiles = null;
		// Cached per-tile [title, value] texts to avoid repeated GetComponentsInChildren calls
		private static FormattableText[][] _tileTexts = null;

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

			_tileTexts = new FormattableText[_tiles.Count][];
			for (int i = 0; i < _tiles.Count; i++)
			{
				_tileTexts[i] = _tiles[i].transform.GetComponentsInChildren<FormattableText>(true);
				_tileTexts[i][0].text = Titles[i];
				_tileTexts[i][1].text = "";
			}

			ApplyVisibility();
			DestroyImmediate(_tile.gameObject);
		}

		/// <summary>Applies Show* settings to each tile's active state. Call from PostParse and Settings.Changed.</summary>
		public static void ApplyVisibility()
		{
			if (_tiles == null) return;
			var settings = Settings.Instance;
			for (int i = 0; i < _tiles.Count; i++)
			{
				bool show = i switch
				{
					Crouch => settings.ShowCrouch,
					Bomb => settings.ShowBomb,
					Linear => settings.ShowLinear,
					V3 => settings.ShowV3,
					EBPM => settings.ShowEBPM,
					BL => settings.ShowBL,
					Pass => settings.ShowPass,
					Tech => settings.ShowTech,
					SS => settings.ShowSS,
					_ => true,
				};
				_tiles[i].gameObject.SetActive(show);
			}
		}

		public static void Apply(double[] data)
		{
			if (_tileTexts == null) return;
			var settings = Settings.Instance;
			for (int i = 0; i < _tileTexts.Length; i++)
			{
				FormattableText title = _tileTexts[i][0];
				FormattableText value = _tileTexts[i][1];
				double v = data[i];
				title.color = settings.TitleColor;
				value.text = Math.Round(v, 2).ToString();
				if (value.text == "0" && i != Crouch && i != Linear) value.text = "X";
				switch (i)
				{
					case V3:
						if (v == 1) value.text = "✔";
						break;
					case Pass:
						value.color = GetStepColor(v, settings.PColorA, settings.PColorB, settings.PColorC);
						break;
					case Tech:
						value.color = GetStepColor(v, settings.TColorA, settings.TColorB, settings.TColorC);
						break;
				}
			}
		}

		private static Color GetStepColor(double value, float mid, float high, float extreme)
		{
			var settings = Settings.Instance;
			if (value >= extreme) return settings.D;
			if (value >= high) return settings.C;
			if (value >= mid) return settings.B;
			return settings.A;
		}
	}
}
