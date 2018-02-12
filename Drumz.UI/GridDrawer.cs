using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using Drumz.Common.Beats;
using Drumz.Common;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI
{
    public interface IBeatMark
    {
        void Draw(SKCanvas canvas);
    }
    public class BeatMark
    {
        public readonly BeatId Id;
        public readonly SKPoint Coord;
        public SKPaint Paint;
        public readonly float Expiry;

        public BeatMark(BeatId id, SKPoint coord, SKPaint paint, float expiry)
        {
            Id = id;
            Coord = coord;
            Paint = paint;
            this.Expiry = expiry;
        }
    }
    public class BeatMarkPaints : IDisposable
    {
        private readonly Dictionary<BeatStatus, SKPaint> beatPaints = new Dictionary<BeatStatus, SKPaint>();

        public BeatMarkPaints()
        {
            beatPaints.Add(BeatStatus.Pending, new SKPaint { Color = SKColors.Gray.WithAlpha(125), IsAntialias = true });
            beatPaints.Add(BeatStatus.Correct, new SKPaint { Color = SKColors.Green.WithAlpha(125), IsAntialias = true });
            beatPaints.Add(BeatStatus.Early, new SKPaint { Color = SKColors.Orange.WithAlpha(125), IsAntialias = true });
            beatPaints.Add(BeatStatus.Late, new SKPaint { Color = SKColors.Violet.WithAlpha(125), IsAntialias = true });
            beatPaints.Add(BeatStatus.MissedPattern, new SKPaint { Color = SKColors.Red.WithAlpha(125), IsAntialias = true });
            beatPaints.Add(BeatStatus.MissedPlay, new SKPaint { Color = SKColors.Red.WithAlpha(125), IsAntialias = true });
        }

        public void Dispose()
        {
            foreach (var paint in beatPaints) paint.Value.Dispose();
        }

        public SKPaint Paint(BeatStatus status)
        {
            return beatPaints[status];
        }
    }
    public class PatternDrawer
    {
        private readonly GridDrawer gridDrawer;
        private readonly BeatsDrawer beatsDrawer;

        public PatternDrawer(Pattern pattern, GridDrawer.Settings settings, int subdivisions)
        {
            this.gridDrawer = new GridDrawer(settings, pattern.Info, subdivisions, pattern.Instruments.Select(i => i.Name).ToArray());
            beatsDrawer = new BeatsDrawer(pattern, gridDrawer);
        }
        public void Draw(SKSurface surface, float t)
        {
            using (var canvas = surface.Canvas)
            {
                gridDrawer.Draw(canvas);
                gridDrawer.DrawTimeMark(canvas, t);
                beatsDrawer.Draw(canvas);
            }
        }
        public void AddPlayedBeat(TimedBeat timedBeat, int instrumentIndexInPattern)
        {
            beatsDrawer.AddPlayedBeat(timedBeat, instrumentIndexInPattern);
        }
        public void SetPlayedBeatStatus(BeatId beatId, BeatStatus status)
        {
            beatsDrawer.SetPlayedBeatStatus(beatId, status);
        }
        public void Tick(float t)
        {
            beatsDrawer.Tick(t);
        }
        public void Clear()
        {
            beatsDrawer.ClearBeats();
        }
    }
    public class BeatsDrawer
    {
        private readonly Pattern pattern;
        private readonly SKPaint patternPaint;
        private readonly IGridCoordinatesProvider grid;
        private readonly BeatMarkPaints beatPaints = new BeatMarkPaints();
        private readonly List<SKPoint> patternBeats = new System.Collections.Generic.List<SKPoint>();
        private readonly Queue<BeatMark> beatMarks = new Queue<BeatMark>();
        private readonly IDictionary<IInstrumentId, int> instruments = new Dictionary<IInstrumentId, int>();
        
        public BeatsDrawer(Pattern pattern, IGridCoordinatesProvider grid)
        {
            this.pattern = pattern;
            this.grid = grid;
            this.patternPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.StrokeAndFill
            };
            for (int i = 0; i < pattern.Instruments.Count; ++i)
                instruments.Add(pattern.Instruments[i], i);
            foreach (var beat in pattern.AllBeats())
            {
                var coord = grid.Coordinates(beat.Instrument, beat.T);
                patternBeats.Add(coord);
            }
        }
        public void AddPlayedBeat(TimedBeat timedBeat, int instrumentIndexInPattern)
        {
            var coord = grid.Coordinates(instrumentIndexInPattern, timedBeat.T);
            var expiry = timedBeat.T + 0.75f*pattern.Info.TotalBeats;
            var mark = new BeatMark(timedBeat.Id, coord, beatPaints.Paint(BeatStatus.Pending), expiry);
            beatMarks.Enqueue(mark);
        }
        public void SetPlayedBeatStatus(BeatId beatId, BeatStatus status)
        {
            foreach (var mark in beatMarks)
                if (mark.Id.Index == beatId.Index)
                    mark.Paint = beatPaints.Paint(status);
        }
        public void ClearBeats()
        {
            beatMarks.Clear();
        }
        public void Tick(float t)
        {
            while (beatMarks.Count > 0 && beatMarks.Peek().Expiry <= t)
                beatMarks.Dequeue();
        }
        public void Draw(SKCanvas canvas)
        {
            foreach (var coord in patternBeats)
            {
                canvas.DrawCircle(coord.X, coord.Y, 5, patternPaint);
            }
            foreach (var beatMark in beatMarks)
            {
                canvas.DrawCircle(beatMark.Coord.X, beatMark.Coord.Y, 3, beatMark.Paint);
            }
        }
    }
    public interface IGridCoordinatesProvider
    {
        SKPoint Coordinates(int instrumentIndex, TimeInUnits t);
        SKPoint Coordinates(int instrumentIndex, float timeInBeats);
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
        private readonly string[] instrumentNames;
        private readonly int subdivisions;
        private readonly GridPaints gridPaints;
        private readonly SKPaint textPaint;
        private readonly SKPaint backgroundPaint;
        private readonly SKRect gridRect;

        private const int namesToFirstBeatMargin = 20;

        public GridDrawer(Settings settings, PatternInfo patternInfo, int subdivisions, string[] instrumentNames)
        {
            if (patternInfo.UnitsPerBeat.Index % subdivisions != 0)
                throw new ArgumentException("Subdivision not compatible with pattern");
            timeUnitWidth = (int)Math.Round(settings.BeatWidth / (float)patternInfo.UnitsPerBeat.Index);
            settings.BeatWidth = timeUnitWidth * patternInfo.UnitsPerBeat.Index;
            this.settings = settings;
            this.info = patternInfo;
            this.instrumentNames = instrumentNames;
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
            var instrumentNameAreaWidth = instrumentNames.Max(i => textPaint.MeasureText(i));
            var gridTop = settings.LineHeight + 1;
            var gridBottom = settings.LineHeight * (instrumentNames.Length) + gridTop;
            var gridLeft = instrumentNameAreaWidth + 20;
            var gridRight = gridLeft + (info.BeatsPerBar * info.BarsCount) * settings.BeatWidth;
            return new SKRect(gridLeft, gridTop, gridRight, gridBottom);
        }
        private SKPaint BuildBackgroudPaint()
        {
            var upColor = new SKColor(25, 146, 191);
            var downColor = new SKColor(37, 92, 123);
            var gradient = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, gridRect.Bottom), new[] { upColor, downColor }, null, SKShaderTileMode.Clamp);
            return new SKPaint { Shader = gradient };
        }
        public SKPoint Coordinates(int instrumentIndex, TimeInUnits t)
        {
            var y = gridRect.Top + (instrumentIndex + 1) * settings.LineHeight;
            var x = gridRect.Left + settings.BeatWidth * t.Index / (float)this.info.UnitsPerBeat.Index;

            return new SKPoint(x, y);
        }
        public SKPoint Coordinates(int instrumentIndex, float timeInBeats)
        {
            timeInBeats = RecenterTime(timeInBeats);
            var y = gridRect.Top + (instrumentIndex + 1) * settings.LineHeight;
            var x = gridRect.Left + settings.BeatWidth * timeInBeats;

            return new SKPoint(x, y);
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
        public void Draw(SKCanvas canvas)
        {
            canvas.DrawPaint(backgroundPaint);
            // drawing horizontal lines
            for (int i = 0; i < instrumentNames.Length; ++i)
            {
                float line = gridRect.Top + (i + 1) * settings.LineHeight;
                canvas.DrawText(instrumentNames[i], 0, line, textPaint);
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
