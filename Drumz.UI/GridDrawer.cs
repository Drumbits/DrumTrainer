﻿using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using Drumz.Common.Beats;
using Drumz.Common;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI
{
    public class PatternDrawer
    {
        private readonly GridDrawer gridDrawer;
        private readonly BeatsDrawer beatsDrawer;

        public PatternDrawer(Pattern pattern, GridDrawer.Settings settings, int subdivisions)
        {
            this.gridDrawer = new GridDrawer(settings, pattern.Info, subdivisions, pattern.Instruments.Select(i => i.Name).ToArray());
            beatsDrawer = new BeatsDrawer(pattern, gridDrawer);
        }
        public void Draw(SKSurface surface, TimeInUnits? t)
        {
            using (var canvas = surface.Canvas)
            {
                gridDrawer.Draw(canvas, t);
                beatsDrawer.Draw(canvas, t ?? new TimeInUnits(-1));
            }
        }
        public void MatchFoundEventHandler(BeatsMatch match)
        {
            beatsDrawer.AddMatchedBeat(match);
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
        private TimeInUnits currentTime = new TimeInUnits(0);
        private readonly List<SKPoint> patternBeats = new System.Collections.Generic.List<SKPoint>();
        private readonly List<MatchedBeatMark> matchedPlayedBeats = new System.Collections.Generic.List<MatchedBeatMark>();
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
                var coord = grid.Coordinates(beat.Item2, beat.Item1);
                patternBeats.Add(coord);
            }
        }
        public void AddMatchedBeat(BeatsMatch match)
        {
            var coord = grid.Coordinates(match.InstrumentIndex, match.PlayedTime - match.LoopOffset);
            var obsoleteTime = Math.Min(match.PatternTime, match.PlayedTime) + pattern.Info.TotalBeats;
            var mark = new MatchedBeatMark(match.Accuracy, coord, obsoleteTime);
            lock (matchedPlayedBeats)
            {
                matchedPlayedBeats.Add(mark);
            }
        }
        public void ClearBeats()
        {
            matchedPlayedBeats.Clear();
        }
        public void Draw(SKCanvas canvas, TimeInUnits t)
        {
            foreach (var coord in patternBeats)
            {
                canvas.DrawCircle(coord.X, coord.Y, 5, patternPaint);
            }
            lock(matchedPlayedBeats)
            {
                for (int index = matchedPlayedBeats.Count - 1; index >= 0; --index)
                {
                    var match = matchedPlayedBeats[index];
                    if (match.DeprecateTime <= t.Index / (float)pattern.Info.UnitsPerBeat.Index)
                    {
                        Drumz.Common.Diagnostics.Logger.TellF(Common.Diagnostics.Logger.Level.Debug,
                   "Removing deprecated hit at coord ({0},{1})", match.Coord.X, match.Coord.Y);

                        matchedPlayedBeats.RemoveAt(index);
                    }
                    else
                    {
                        canvas.DrawCircle(match.Coord.X, match.Coord.Y, 3, match.Paint);
                    }
                }
            }
        }
        private class MatchedBeatMark
        {
            public readonly SKPoint Coord;
            public readonly SKPaint Paint;
            public readonly float DeprecateTime;
            public MatchedBeatMark(float accuracy, SKPoint coords, float deprecateTime)
            {
                this.DeprecateTime = deprecateTime;
                this.Coord = coords;
                var baseColor = SKColors.Gray;
                var lateColor = SKColors.DarkGreen;
                var earlyColor = SKColors.DarkBlue;
                var color = Mix(accuracy < 0 ? earlyColor : lateColor, baseColor, 1).WithAlpha(200);
                Paint = new SKPaint
                {
                    Color = color,
                    IsAntialias = true,
                    Style = SKPaintStyle.StrokeAndFill
                };
            }
            private static SKColor Mix(SKColor c1, SKColor c2, float accuracy)
            {
                return new SKColor(
                    (byte)(c1.Red * accuracy + c2.Red * (1 - accuracy)),
                    (byte)(c1.Green * accuracy + c2.Green * (1 - accuracy)),
                    (byte)(c1.Blue * accuracy + c1.Blue * (1 - accuracy)));
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
            var y = gridRect.Top + (instrumentIndex + 1) * settings.LineHeight;
            var x = gridRect.Left + settings.BeatWidth * timeInBeats;

            return new SKPoint(x, y);
        }
        public void Draw(SKCanvas canvas, TimeInUnits? t)
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
            if (t.HasValue && t.Value.Index > 0)
            {
                var index = (t.Value.Index - 1) % info.LastTime.Index;
                DrawVerticalLine(canvas, gridRect.Left + index * timeUnitWidth, gridPaints.timeMark);
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