using System;
using System.Linq;
using SkiaSharp;
using Drumz.Common;
using Drumz.Common.Utils;
using Drumz.Common.Beats;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI
{

    public class SummaryDrawer
    {
        private readonly SKPaint mainPaint = new SKPaint { Color = SKColors.White, TextSize = 12, IsAntialias=true,LcdRenderText=true };
        private readonly IGridCoordinatesProvider gridCoordinates;
        private readonly Pattern pattern;

        public SummaryDrawer(IGridCoordinatesProvider gridCoordinates, Pattern pattern)
        {
            this.gridCoordinates = gridCoordinates;
            this.pattern = pattern;
        }

        private void PrintBeatSummary(SKCanvas canvas, SKPoint center, AccuracySummary accuracy)
        {
            var s = Math.Round(100 * accuracy.Value, 0).ToString();
            var width = mainPaint.MeasureText(s);
            var bl = new SKPoint(center.X - 0.5f * width, center.Y + 0.5f * mainPaint.TextSize);
            canvas.DrawText(s, bl.X, bl.Y, mainPaint);
        }
        public void Draw(SKCanvas canvas, SKRect gridRect, PerformanceSummary summary)
        {
            canvas.Save();
            canvas.Translate(0, gridRect.Height);
            foreach (var beat in pattern.Ids)
            {
                var beatInfo = pattern.Beat(beat);
                var summaryForBeat = summary.BeatSummary(beat);
                var point = gridCoordinates.Coordinates(beatInfo.Instrument, beatInfo.T);
                PrintBeatSummary(canvas, point, summaryForBeat);
            }
            canvas.Restore();
        }
    }
    public class GridDrawer : IGridCoordinatesProvider
    {
        public class Settings
        {
            public int LineHeight;
            public int BeatWidth;
            public int FontSize;
        }
        private class GridPaints
        {
            public SKPaint horizontal;
            public SKPaint horizontalHeaderUnderline;
            public SKPaint verticalOnBeat;
            public SKPaint verticalOffBeat;
            public SKPaint timeMark;
        }
        private readonly PatternInfo info;
        private readonly Settings settings;
        private readonly int timeUnitWidth;
        private readonly IInstrumentId[] instruments;
        private readonly int subdivisions;
        private readonly GridPaints gridPaints;
        private readonly SKPaint textPaint;
        private readonly SKPaint backgroundPaint;
        private readonly SKRect gridRect;

        private const int namesToFirstBeatMargin = 20;

        public GridDrawer(Settings settings, PatternInfo patternInfo, int subdivisions, IInstrumentId[] instruments)
        {
            if (patternInfo.UnitsPerBeat.Index % subdivisions != 0)
                throw new ArgumentException("Subdivision not compatible with pattern");
            timeUnitWidth = (int)Math.Round(settings.BeatWidth / (float)patternInfo.UnitsPerBeat.Index);
            settings.BeatWidth = timeUnitWidth * patternInfo.UnitsPerBeat.Index;
            this.settings = settings;
            this.info = patternInfo;
            this.instruments = instruments;
            this.subdivisions = subdivisions;
            this.textPaint = BuildInstrumentNamePaint();
            this.gridRect = BuildGridRect();
            this.gridPaints = BuildGridPaints();
            this.backgroundPaint = BuildBackgroudPaint();
        }
        private GridPaints BuildGridPaints()
        {
            var gridPaint = new SKPaint();
            gridPaint.Color = SKColors.White;
            gridPaint.IsAntialias = false;
            gridPaint.FakeBoldText = false;
            gridPaint.IsDither = false;
            gridPaint.StrokeWidth = 0;
            gridPaint.Style = SKPaintStyle.Stroke;
            var underline = gridPaint.Clone();
            underline.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(gridRect.Left - namesToFirstBeatMargin, 0),
                new[] { gridPaint.Color.WithAlpha(0), gridPaint.Color.WithAlpha(125) }, null, SKShaderTileMode.Clamp);
            var horiz = gridPaint.Clone();
            horiz.Shader = SKShader.CreateLinearGradient(new SKPoint(gridRect.Left, 0), new SKPoint(gridRect.Right, 0),
                new[] { gridPaint.Color.WithAlpha(125), gridPaint.Color.WithAlpha(125), gridPaint.Color.WithAlpha(0) }, 
                new[] { 0f, 1f-timeUnitWidth/(float)gridRect.Width, 1f }, SKShaderTileMode.Clamp);
            var offBeat = gridPaint.Clone();
            offBeat.PathEffect = SKPathEffect.CreateDash(new[] { 1f, settings.LineHeight - 1 }, 1f);
            var onBeat = gridPaint.Clone();
            onBeat.Color = gridPaint.Color.WithAlpha(125);
            onBeat.Shader = SKShader.CreateLinearGradient(new SKPoint(0, gridRect.Top + 1), new SKPoint(0, gridRect.Bottom + 20),
                new[] { gridPaint.Color.WithAlpha(0), gridPaint.Color.WithAlpha(125), gridPaint.Color.WithAlpha(125), gridPaint.Color.WithAlpha(0) },
                new[] { 0f, 10f / (gridRect.Height + 20), 1f - 10f / (gridRect.Height + 20), 1f }, SKShaderTileMode.Clamp);
            var timeMark = gridPaint.Clone();
            timeMark.Shader = SKShader.CreateLinearGradient(new SKPoint(0, gridRect.Top + 1), new SKPoint(0, gridRect.Bottom + 20),
                new[] { gridPaint.Color.WithAlpha(0), gridPaint.Color, gridPaint.Color, gridPaint.Color.WithAlpha(0) },
                new[] { 0f, 10f / (gridRect.Height + 20), 1f - 10f / (gridRect.Height + 20), 1f }, SKShaderTileMode.Clamp);
            timeMark.StrokeWidth = 3;
            return new GridPaints
            {
                horizontalHeaderUnderline = underline,
                horizontal = horiz,
                verticalOnBeat = onBeat,
                verticalOffBeat = offBeat,
                timeMark = timeMark
            };
        }
        private SKPaint BuildInstrumentNamePaint()
        {
            var paint = new SKPaint();
            paint.Color = SKColors.White;
            paint.IsAntialias = true;
            paint.FakeBoldText = false;
            paint.IsDither = false;
            paint.StrokeWidth = 1;
            paint.Style = SKPaintStyle.StrokeAndFill;
            paint.Typeface = SKTypeface.FromFamilyName("Arial Black", SKTypefaceStyle.Normal);
            paint.LcdRenderText = true;
            paint.TextAlign = SKTextAlign.Left;
            paint.TextSize = (int)(settings.LineHeight * 0.8);
            return paint;
        }
        private SKRect BuildGridRect()
        {
            var instrumentNameAreaWidth = instruments.Max(i => textPaint.MeasureText(i.Name));
            var gridTop = settings.LineHeight + 1;
            var gridBottom = settings.LineHeight * (instruments.Length) + gridTop;
            var gridLeft = instrumentNameAreaWidth + 20;
            var gridRight = gridLeft + (info.BeatsPerBar * info.BarsCount) * settings.BeatWidth;
            return new SKRect(gridLeft, gridTop, gridRight, gridBottom);
        }
        private SKPaint BuildBackgroudPaint()
        {
            var upColor = new SKColor(25, 146, 191);
            var downColor = new SKColor(37, 92, 123);
            var gradient = SKShader.CreateLinearGradient(new SKPoint(0, 0),
                new SKPoint(0, 100/*gridRect.Bottom*/), 
                new[] { upColor, downColor }, null, SKShaderTileMode.Clamp);
            return new SKPaint { Shader = gradient };
        }
        public SKRect GridRect { get { return gridRect; } }
        public SKPoint Coordinates(IInstrumentId instrument, TimeInUnits t)
        {
            var instrumentIndex = InstrumentIndex(instrument);
            var y = gridRect.Top + (instrumentIndex + 1) * settings.LineHeight;
            var x = gridRect.Left + settings.BeatWidth * t.Index / (float)this.info.UnitsPerBeat.Index;

            return new SKPoint(x, y);
        }
        public SKPoint Coordinates(IInstrumentId instrument, float timeInBeats)
        {
            var instrumentIndex = InstrumentIndex(instrument);
            timeInBeats = RecenterTime(timeInBeats);
            var y = gridRect.Top + (instrumentIndex + 1) * settings.LineHeight;
            var x = gridRect.Left + settings.BeatWidth * timeInBeats;

            return new SKPoint(x, y);
        }
        private int InstrumentIndex(IInstrumentId instrument)
        {
            var instrumentIndex = instruments.IndexOf(instrument);
            if (instrumentIndex == -1)
            {
                Drumz.Common.Diagnostics.Logger.TellF(Common.Diagnostics.Logger.Level.Error, "Instument not in pattern: {0} not in {1}", instrument.Name, instruments.ToNiceString());
                instrumentIndex = instruments.Length;
            }
            return instrumentIndex;
        }
        private float RecenterTime(float t)
        {
            int nbLoops = (int)Math.Floor(Math.Round(t / (info.BarsCount * info.BeatsPerBar), 4));
            return t - nbLoops * info.BarsCount * info.BeatsPerBar;
        }
        public void DrawTimeMark(SKCanvas canvas, float t)
        {
            t = RecenterTime(t);
            DrawVerticalLine(canvas, gridRect.Left + t * settings.BeatWidth, gridPaints.timeMark);
        }
        public void Draw(SKCanvas canvas, SKRect rect)
        {
            canvas.Save();
            canvas.Scale(rect.Height / 100f);
            canvas.DrawPaint(backgroundPaint);
            canvas.Restore();
            // drawing horizontal lines
            for (int i = 0; i < instruments.Length; ++i)
            {
                float line = gridRect.Top + (i + 1) * settings.LineHeight;
                canvas.DrawText(instruments[i].Name, 0, line, textPaint);
                DrawHorizontalLine(canvas, line);
            }
            int col = (int)gridRect.Left;
            float step = settings.BeatWidth / (float)subdivisions;
            for (int bar = 0; bar < info.BarsCount; ++bar)
                for (int beatIndex = 1; beatIndex <= info.BeatsPerBar; ++beatIndex)
                {
                    DrawVerticalLine(canvas, col, beatIndex);
                    for (var subDivIndex = 1; subDivIndex < subdivisions; ++subDivIndex)
                        DrawVerticalLine(canvas, col + subDivIndex * step, (int?)null);
                    col += settings.BeatWidth;
                }
        }
        private void DrawHorizontalLine(SKCanvas canvas, float baseLine)
        {
            canvas.DrawLine(0, baseLine, gridRect.Left, baseLine, gridPaints.horizontalHeaderUnderline);
            canvas.DrawLine(gridRect.Left - 1, baseLine, gridRect.Right, baseLine, gridPaints.horizontal);
        }
        private void DrawVerticalLine(SKCanvas canvas, float baseColumn, int? beatIndex)
        {
            var paint = beatIndex.HasValue ? gridPaints.verticalOnBeat : gridPaints.verticalOffBeat;
            DrawVerticalLine(canvas, baseColumn, paint);
            if (beatIndex.HasValue)
            {
                var textWidth = textPaint.MeasureText(beatIndex.Value.ToString());
                canvas.DrawText(beatIndex.Value.ToString(), baseColumn - textWidth / 2, settings.LineHeight, textPaint);
            }
        }
        private void DrawVerticalLine(SKCanvas canvas, float col, SKPaint paint)
        {
            canvas.DrawLine(col, gridRect.Top + 1, col, gridRect.Bottom + 20, paint);
        }
    }
}
