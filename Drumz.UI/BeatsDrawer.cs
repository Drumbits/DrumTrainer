using System.Collections.Generic;
using SkiaSharp;
using Drumz.Common.Beats;
using Drumz.Common;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI
{
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
        public void AddPlayedBeat(TimedBeatId timedBeat, IInstrumentId instrument)
        {
            var coord = grid.Coordinates(instrument, timedBeat.T);
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
}
