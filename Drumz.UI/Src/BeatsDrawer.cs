using System.Collections.Generic;
using SkiaSharp;
using Drumz.Common.Beats;
using Drumz.Common;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI
{
    public class BeatMarkDrawer
    {
        public static void Draw(string mark, SKCanvas canvas, SKPoint center, SKPaint paint)
        {
            switch (mark)
            {
                case "x":
                    paint.StrokeWidth = 3;
                    paint.Style = SKPaintStyle.StrokeAndFill;
                    canvas.DrawLine(center.X - 3, center.Y - 3, center.X + 3, center.Y + 3, paint);
                    canvas.DrawLine(center.X - 3, center.Y + 3, center.X + 3, center.Y - 3, paint);
                    return;
                case "o":
                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawCircle(center.X, center.Y, 5, paint);
                    return;
                case "*":
                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.StrokeAndFill;
                    canvas.DrawCircle(center.X, center.Y, 5, paint);
                    return;
                default:
                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.StrokeAndFill;
                    canvas.DrawCircle(center.X, center.Y, 5, paint);
                    return;
            }
        }
    }
    public sealed class PointAndMark
    {
        public readonly SKPoint Point;
        public readonly string Mark;

        public PointAndMark(SKPoint point, string mark)
        {
            Point = point;
            Mark = mark;
        }
    }
    public class BeatsDrawer
    {
        private readonly Pattern pattern;
        private readonly SKPaint patternPaint;
        private readonly IGridCoordinatesProvider grid;
        private readonly BeatMarkPaints beatPaints = new BeatMarkPaints();
        private readonly List<PointAndMark> patternBeats = new List<PointAndMark>();
        private readonly Queue<BeatMark> beatMarks = new Queue<BeatMark>();
        private readonly SoundsList patternSounds;

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
            this.patternSounds = pattern.Sounds;
            foreach (var beat in pattern.AllBeats())
            {
                var coord = grid.Coordinates(beat.Sound.Instrument, beat.T);
                patternBeats.Add(new PointAndMark(coord, beat.Sound.Mark));
            }
        }
        public void AddPlayedBeat(TimedBeatId timedBeat, ISoundId sound)
        {
            var coord = grid.Coordinates(sound.Instrument, timedBeat.T);
            var expiry = timedBeat.T + 0.75f * pattern.Info.TotalBeats;
            var mark = new BeatMark(timedBeat.Id, coord, beatPaints.Paint(BeatStatus.Pending), expiry, sound.Mark);
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
                BeatMarkDrawer.Draw(coord.Mark, canvas, coord.Point, patternPaint);
            }
            foreach (var beatMark in beatMarks)
            {
                BeatMarkDrawer.Draw(beatMark.Mark, canvas, beatMark.Coord, beatMark.Paint);
            }
        }
    }
}