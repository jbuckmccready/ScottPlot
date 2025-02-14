﻿using ScottPlot.Drawing;
using System.Collections.Generic;
using System.Drawing;

namespace ScottPlot.Plottable
{
    /// <summary>
    /// A colorbar translates numeric intensity values to colors.
    /// The Colorbar plot type displays a Colorbar along an edge of the plot.
    /// </summary>
    public class Colorbar : IPlottable
    {
        public Renderable.Edge Edge = Renderable.Edge.Right;

        private Colormap Colormap;
        private Bitmap BmpScale;
        private readonly List<string> TickLabels = new();
        private readonly List<double> TickFractions = new();

        public bool IsVisible { get; set; } = true;
        public int XAxisIndex { get => 0; set { } }
        public int YAxisIndex { get => 0; set { } }
        public int Width = 20;

        public readonly Drawing.Font TickLabelFont = new();
        public Color TickMarkColor = Color.Black;
        public float TickMarkLength = 3;
        public float TickMarkWidth = 1;

        public Colorbar(Colormap colormap = null)
        {
            UpdateColormap(colormap ?? Colormap.Viridis);
        }

        /// <summary>
        /// Clear all tick marks and labels
        /// </summary>
        public void ClearTicks()
        {
            TickFractions.Clear();
            TickLabels.Clear();
        }

        /// <summary>
        /// Add a tick mark and label
        /// </summary>
        /// <param name="fraction">from 0 (darkest) to 1 (brightest)</param>
        /// <param name="label">string displayed beside the tick</param>
        public void AddTick(double fraction, string label)
        {
            TickFractions.Add(fraction);
            TickLabels.Add(label);
        }

        /// <summary>
        /// Add tick marks and labels
        /// </summary>
        /// <param name="fractions">from 0 (darkest) to 1 (brightest)</param>
        /// <param name="labels">strings displayed beside the ticks</param>
        public void AddTicks(double[] fractions, string[] labels)
        {
            if (fractions.Length != labels.Length)
                throw new("fractions and labels must have the same length");

            for (int i = 0; i < fractions.Length; i++)
            {
                TickFractions.Add(fractions[i]);
                TickLabels.Add(labels[i]);
            }
        }

        /// <summary>
        /// Define a tick marks and labels
        /// </summary>
        /// <param name="fractions">from 0 (darkest) to 1 (brightest)</param>
        /// <param name="labels">strings displayed beside the ticks</param>
        public void SetTicks(double[] fractions, string[] labels)
        {
            ClearTicks();
            AddTicks(fractions, labels);
        }

        public LegendItem[] GetLegendItems() => null;

        public AxisLimits GetAxisLimits() => new(double.NaN, double.NaN, double.NaN, double.NaN);

        public void ValidateData(bool deep = false)
        {
            if (TickLabels.Count != TickFractions.Count)
                throw new("Tick labels and positions must have the same length");
        }

        /// <summary>
        /// Re-Render the colorbar using a new colormap
        /// </summary>
        public void UpdateColormap(Colormap newColormap)
        {
            Colormap = newColormap;
            UpdateBitmap();
        }

        private void UpdateBitmap()
        {
            BmpScale?.Dispose();
            BmpScale = GetBitmap();
        }

        /// <summary>
        /// Return a Bitmap of just the color portion of the colorbar.
        /// The width is defined by the Width field
        /// The height will be 256
        /// </summary>
        /// <returns></returns>
        public Bitmap GetBitmap() =>
            Colormap.Colorbar(Colormap, Width, 256, true);

        /// <summary>
        /// Return a Bitmap of just the color portion of the colorbar
        /// </summary>
        /// <param name="width">width of the Bitmap</param>
        /// <param name="height">height of the Bitmap</param>
        /// <param name="vertical">if true, colormap will be vertically oriented (tall and skinny)</param>
        /// <returns></returns>
        public Bitmap GetBitmap(int width, int height, bool vertical = true) =>
            Colormap.Colorbar(Colormap, width, height, vertical);

        public void Render(PlotDimensions dims, Bitmap bmp, bool lowQuality = false)
        {
            if (BmpScale is null)
                UpdateBitmap();

            RectangleF colorbarRect = RenderColorbar(dims, bmp);
            RenderTicks(dims, bmp, lowQuality, colorbarRect);
        }

        private RectangleF RenderColorbar(PlotDimensions dims, Bitmap bmp)
        {
            float scaleLeftPad = 10;

            PointF location = new(dims.DataOffsetX + dims.DataWidth + scaleLeftPad, dims.DataOffsetY);
            SizeF size = new(Width, dims.DataHeight);
            RectangleF rect = new(location, size);

            using (Graphics gfx = GDI.Graphics(bmp, dims, lowQuality: true, clipToDataArea: false))
            using (var pen = GDI.Pen(Color.Black))
            {
                gfx.DrawImage(BmpScale, location.X, location.Y, size.Width, size.Height + 1);
                gfx.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }

            return rect;
        }

        private void RenderTicks(PlotDimensions dims, Bitmap bmp, bool lowQuality, RectangleF colorbarRect)
        {
            float tickLeftPx = colorbarRect.Right;
            float tickRightPx = tickLeftPx + TickMarkLength;
            float tickLabelPx = tickRightPx + 2;

            using Graphics gfx = GDI.Graphics(bmp, dims, lowQuality, false);
            using var tickMarkPen = GDI.Pen(TickMarkColor, TickMarkWidth);
            using var tickLabelBrush = GDI.Brush(TickLabelFont.Color);
            using var tickFont = GDI.Font(TickLabelFont);
            using var sf = new StringFormat() { LineAlignment = StringAlignment.Center };

            for (int i = 0; i < TickLabels.Count; i++)
            {
                float y = colorbarRect.Top + (float)((1 - TickFractions[i]) * colorbarRect.Height);
                gfx.DrawLine(tickMarkPen, tickLeftPx, y, tickRightPx, y);
                gfx.DrawString(TickLabels[i], tickFont, tickLabelBrush, tickLabelPx, y, sf);
            }
        }
    }
}
