using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatmapScanner.UI
{
    /// <summary>
    /// A pure-code ViewController that renders a smoothed line graph of swing data
    /// (either SwingDiff or SwingTech over Seconds) onto a Texture2D displayed via RawImage,
    /// with dynamically-scaled TMPro Y labels and time-based X labels.
    /// </summary>
    internal class SwingGraphViewController : ViewController
    {
        private const int TexWidth     = 256;
        private const int TexHeight    = 128;
        private const int SmoothWindow = 12;

        // Fraction of the panel reserved for axis labels
        private const float LeftFrac   = 0.18f;
        private const float BottomFrac = 0.17f;

        // Y label pool size — large enough to cover any reasonable tick count
        private const int MaxYLabels = 7;
        // X label count (0 %, 25 %, 50 %, 75 %, 100 % of duration)
        private const int XLabelCount = 5;

        private static readonly Color32 BgColor  = new Color32(18, 18, 22, 220);
        private static readonly Color32 GridColor = new Color32(55, 55, 65, 255);

        // Per-instance colors (set via Initialize)
        private Color32 _lineColor = new Color32(100, 220, 130, 255);
        private Color32 _fillColor = new Color32(60, 160, 90, 60);

        private RawImage        _graphImage;
        private Texture2D       _texture;
        private TextMeshProUGUI _overlayTitle;
        private Color32[]       _blankPixels;

        private string _graphTitle = "";

        // Y scale — computed dynamically from data in SetData
        private double _yMax = 1.0;

        private TextMeshProUGUI[] _yLabelTexts;
        private TextMeshProUGUI[] _xLabelTexts;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called once after the floating screen is created.
        /// Must be called regardless of whether DidActivate has fired yet.
        /// </summary>
        public void Initialize(string title, Color32 lineColor, Color32 fillColor)
        {
            _graphTitle = title;
            _lineColor  = lineColor;
            _fillColor  = fillColor;
            if (_overlayTitle != null) _overlayTitle.text = title;
        }

        /// <summary>Clears the graph to the blank background and shows the title placeholder.</summary>
        public void ClearGraph()
        {
            if (_texture == null) return;
            _texture.SetPixels32(_blankPixels);
            _texture.Apply();
            if (_overlayTitle != null) _overlayTitle.gameObject.SetActive(true);
            if (_xLabelTexts != null)
                foreach (var lbl in _xLabelTexts)
                    if (lbl != null) lbl.text = "";
        }

        /// <summary>Draws a smoothed line graph from the given time/value pairs with a dynamic Y scale.</summary>
        public void SetData(List<double> times, List<double> values)
        {
            if (_texture == null || times == null || values == null || times.Count < 2) return;
            if (_overlayTitle != null) _overlayTitle.gameObject.SetActive(false);

            // ── Smooth (O(n) sliding-window sum instead of O(n×W)) ────────────
            int n    = values.Count;
            int half = SmoothWindow / 2;
            var smoothed = new double[n];
            double windowSum = 0.0;
            int    windowStart = 0;
            int    windowEnd   = -1;
            for (int i = 0; i < n; i++)
            {
                int s = Mathf.Max(0, i - half);
                int e = Mathf.Min(n - 1, i + half);
                // Extend window right
                while (windowEnd < e) { windowEnd++; windowSum += values[windowEnd]; }
                // Shrink window left
                while (windowStart < s) { windowSum -= values[windowStart]; windowStart++; }
                smoothed[i] = windowSum / (windowEnd - windowStart + 1);
            }

            // ── Dynamic Y scale ───────────────────────────────────────────────
            double rawMax = 0.0;
            for (int i = 0; i < smoothed.Length; i++)
                if (smoothed[i] > rawMax) rawMax = smoothed[i];
            if (rawMax < 0.0001) rawMax = 1.0;

            double step    = NiceStep(rawMax / 4.0);
            _yMax          = Math.Ceiling(rawMax / step) * step;
            int tickCount  = Math.Min((int)Math.Round(_yMax / step) + 1, MaxYLabels);

            // Update Y labels — show computed ticks, hide the rest of the pool
            if (_yLabelTexts != null)
            {
                for (int i = 0; i < _yLabelTexts.Length; i++)
                {
                    if (_yLabelTexts[i] == null) continue;
                    if (i < tickCount)
                    {
                        double val = step * i;
                        _yLabelTexts[i].text = FormatAxisValue(val);
                        float yFrac   = (float)(val / _yMax);
                        float screenY = BottomFrac + yFrac * (1f - BottomFrac);
                        var rt = _yLabelTexts[i].rectTransform;
                        float labelTop = Mathf.Min(screenY + 0.05f, 0.96f);
                        float labelBot = Mathf.Max(screenY - 0.05f, BottomFrac);
                        rt.anchorMin = new Vector2(0f,               labelBot);
                        rt.anchorMax = new Vector2(LeftFrac - 0.02f, labelTop);
                        rt.offsetMin = rt.offsetMax = Vector2.zero;
                        _yLabelTexts[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        _yLabelTexts[i].gameObject.SetActive(false);
                    }
                }
            }

            // ── X axis labels ─────────────────────────────────────────────────
            double tMin     = times[0];
            double tMax     = times[times.Count - 1];
            double duration = tMax - tMin;
            if (_xLabelTexts != null)
                for (int i = 0; i < _xLabelTexts.Length; i++)
                    if (_xLabelTexts[i] != null)
                    {
                        double t = (double)i / (_xLabelTexts.Length - 1) * duration;
                        _xLabelTexts[i].text = FormatTime(t);
                    }

            // ── Pixel helpers ─────────────────────────────────────────────────
            const int mL = 2, mR = 2, mT = 2, mB = 2;
            int dW = TexWidth  - mL - mR;
            int dH = TexHeight - mT - mB;

            // Precompute pixel X for each sample once (used in fill + line loops)
            double tRange = tMax - tMin;
            var pixelXs = new int[n];
            for (int i = 0; i < n; i++)
                pixelXs[i] = mL + (int)((times[i] - tMin) / tRange * (dW - 1));

            int ToPixelY(double v) =>
                mB + (int)(Mathf.Clamp01((float)(v / _yMax)) * (dH - 1));

            // Reset pixel buffer
            var pixels = (Color32[])_blankPixels.Clone();

            // ── Dashed Y grid lines at each non-zero tick ─────────────────────
            for (int i = 1; i < tickCount; i++)
            {
                int py = ToPixelY(step * i);
                for (int px = mL; px < TexWidth - mR; px++)
                    if ((px / 3) % 2 == 0)
                        pixels[py * TexWidth + px] = GridColor;
            }

            // ── Fill area under the curve ─────────────────────────────────────
            // Precompute fill-blend constants once (_fillColor is constant for the call)
            float fillA  = _fillColor.a / 255f;
            float fillAi = 1f - fillA;
            byte  fillR  = (byte)(_fillColor.r * fillA);
            byte  fillG  = (byte)(_fillColor.g * fillA);
            byte  fillB  = (byte)(_fillColor.b * fillA);

            for (int i = 0; i < smoothed.Length - 1; i++)
            {
                int x0 = pixelXs[i],               x1 = pixelXs[i + 1];
                int y0 = ToPixelY(smoothed[i]),     y1 = ToPixelY(smoothed[i + 1]);
                int xS = Mathf.Min(x0, x1),         xE = Mathf.Max(x0, x1);
                for (int px = xS; px <= xE; px++)
                {
                    float ft   = (xE == xS) ? 0f : (float)(px - xS) / (xE - xS);
                    int   yTop = Mathf.RoundToInt(Mathf.Lerp(y0, y1, ft));
                    for (int py = mB; py <= yTop; py++)
                    {
                        if (px < 0 || px >= TexWidth || py < 0 || py >= TexHeight) continue;
                        int   idx = py * TexWidth + px;
                        Color32 dst = pixels[idx];
                        pixels[idx] = new Color32(
                            (byte)(fillR + dst.r * fillAi),
                            (byte)(fillG + dst.g * fillAi),
                            (byte)(fillB + dst.b * fillAi),
                            255);
                    }
                }
            }

            // ── Vertical dashed red line at smoothed peak ─────────────────────
            int peakIdx = 0;
            for (int i = 1; i < smoothed.Length; i++)
                if (smoothed[i] > smoothed[peakIdx]) peakIdx = i;
            int peakX     = pixelXs[peakIdx];
            var peakColor = new Color32(220, 60, 60, 255);
            for (int py = mB; py < TexHeight - mT; py++)
                if ((py / 4) % 2 == 0)
                    SetPixel(pixels, peakX, py, peakColor);

            // ── Data line ─────────────────────────────────────────────────────
            for (int i = 0; i < smoothed.Length - 1; i++)
                DrawLine(pixels,
                    pixelXs[i],          ToPixelY(smoothed[i]),
                    pixelXs[i + 1],      ToPixelY(smoothed[i + 1]),
                    _lineColor);

            _texture.SetPixels32(pixels);
            _texture.Apply();
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (!firstActivation) return;

            // Graph image — inset to leave room for axis labels
            var graphGo   = new GameObject("GraphImage");
            graphGo.transform.SetParent(transform, false);
            var graphRect = graphGo.AddComponent<RectTransform>();
            graphRect.anchorMin = new Vector2(LeftFrac, BottomFrac);
            graphRect.anchorMax = Vector2.one;
            graphRect.offsetMin = graphRect.offsetMax = Vector2.zero;
            _graphImage = graphGo.AddComponent<RawImage>();

            _texture = new Texture2D(TexWidth, TexHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };
            _graphImage.texture  = _texture;
            // Use Beat Saber's no-glow material so the RawImage is not affected by the bloom post-process.
            var noGlowMat = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "UINoGlow");
            if (noGlowMat != null) _graphImage.material = noGlowMat;

            _blankPixels = new Color32[TexWidth * TexHeight];
            for (int i = 0; i < _blankPixels.Length; i++) _blankPixels[i] = BgColor;

            // Centered overlay title (shown when no data)
            var overlayGo   = new GameObject("OverlayTitle");
            overlayGo.transform.SetParent(transform, false);
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = new Vector2(LeftFrac, BottomFrac);
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            _overlayTitle = overlayGo.AddComponent<TextMeshProUGUI>();
            _overlayTitle.text      = _graphTitle;
            _overlayTitle.fontSize  = 7f;
            _overlayTitle.alignment = TextAlignmentOptions.Center;
            _overlayTitle.color     = new Color(1f, 1f, 1f, 0.4f);
            _overlayTitle.fontStyle = FontStyles.Bold;

            // Y label pool — pre-allocate MaxYLabels slots, all hidden initially.
            // Texts and positions are set dynamically in SetData so the scale always
            // matches the actual data regardless of call order with Initialize.
            _yLabelTexts = new TextMeshProUGUI[MaxYLabels];
            for (int i = 0; i < MaxYLabels; i++)
            {
                var go  = new GameObject("YLabel_" + i);
                go.transform.SetParent(transform, false);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text      = "";
                tmp.fontSize  = 4f;
                tmp.alignment = TextAlignmentOptions.Right;
                tmp.color     = Color.white;
                _yLabelTexts[i] = tmp;
                go.SetActive(false); // hidden until SetData populates them
            }

            // X axis labels — positions are fixed, text is filled in SetData
            _xLabelTexts = new TextMeshProUGUI[XLabelCount];
            for (int i = 0; i < XLabelCount; i++)
            {
                float xFrac   = (float)i / (XLabelCount - 1);
                float screenX = LeftFrac + xFrac * (1f - LeftFrac);

                // Keep a fixed-width window and slide it inward if it overflows either edge
                const float halfW = 0.07f;
                const float rightEdge = 0.99f;
                float rawMin = screenX - halfW;
                float rawMax = screenX + halfW;
                if (rawMax > rightEdge) { float shift = rawMax - rightEdge; rawMax -= shift; rawMin -= shift; }
                if (rawMin < LeftFrac)  rawMin = LeftFrac;
                float minX = rawMin;
                float maxX = rawMax;

                var go  = new GameObject("XLabel_" + i);
                go.transform.SetParent(transform, false);
                var rt  = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(minX, 0f);
                rt.anchorMax = new Vector2(maxX, BottomFrac - 0.01f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text      = "";
                tmp.fontSize  = 4f;
                tmp.alignment = i == 0              ? TextAlignmentOptions.Left
                              : i == XLabelCount - 1 ? TextAlignmentOptions.Right
                              :                        TextAlignmentOptions.Center;
                tmp.color     = Color.white;
                _xLabelTexts[i] = tmp;
            }

            ClearGraph();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a "nice" step size for axis ticks (rounds to 1, 2, or 5 × 10^n).
        /// </summary>
        private static double NiceStep(double raw)
        {
            if (raw <= 0) return 1;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double fraction  = raw / magnitude;
            double nice      = fraction < 1.5 ? 1 : fraction < 3.5 ? 2 : fraction < 7.5 ? 5 : 10;
            return nice * magnitude;
        }

        /// <summary>Formats a Y axis tick: integers without decimal, others to 1 dp.</summary>
        private static string FormatAxisValue(double v) =>
            (v == Math.Floor(v)) ? ((int)v).ToString() : v.ToString("F1");

        /// <summary>Formats elapsed seconds as "Xs" (under 60 s) or "m:ss".</summary>
        private static string FormatTime(double seconds)
        {
            if (seconds < 60) return ((int)Math.Round(seconds)) + "s";
            int m = (int)(seconds / 60);
            int s = (int)(seconds % 60);
            return m + ":" + s.ToString("D2");
        }

        private static Color32 Blend(Color32 dst, Color32 src)
        {
            float a = src.a / 255f;
            return new Color32(
                (byte)(src.r * a + dst.r * (1f - a)),
                (byte)(src.g * a + dst.g * (1f - a)),
                (byte)(src.b * a + dst.b * (1f - a)),
                255);
        }

        private static void DrawLine(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                SetPixel(pixels, x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>Writes a 1-wide 2-tall pixel so the line stays visible at small scale.</summary>
        private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x < 0 || x >= TexWidth || y < 0 || y >= TexHeight) return;
            pixels[y * TexWidth + x] = color;
            if (y + 1 < TexHeight) pixels[(y + 1) * TexWidth + x] = color;
        }
    }
}
