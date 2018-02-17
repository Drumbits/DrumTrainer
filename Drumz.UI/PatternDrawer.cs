using System.Linq;
using SkiaSharp;
using Drumz.Common;
using Drumz.Common.Beats;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI
{
    public class PatternDrawer
    {
        private readonly GridDrawer gridDrawer;
        private readonly BeatsDrawer beatsDrawer;
        private readonly SummaryDrawer summaryDrawer;
        private PerformanceSummary summary = null;

        public PatternDrawer(Pattern pattern, GridDrawer.Settings settings, int subdivisions)
        {
            this.gridDrawer = new GridDrawer(settings, pattern.Info, subdivisions, pattern.Instruments.ToArray());
            beatsDrawer = new BeatsDrawer(pattern, gridDrawer);
            summaryDrawer = new SummaryDrawer(gridDrawer, pattern);
        }
        public void SetSummary(PerformanceSummary summary)
        {
            this.summary = summary;
        }
        public void Draw(SKSurface surface, SKRect rect, float t)
        {
            using (var canvas = surface.Canvas)
            {
                gridDrawer.Draw(canvas, rect);
                gridDrawer.DrawTimeMark(canvas, t);
                beatsDrawer.Draw(canvas);
                if (summary != null)
                {
                    summaryDrawer.Draw(canvas, gridDrawer.GridRect, summary);
                }
            }
        }
        public SKRect GridRect { get { return gridDrawer.GridRect; } }
        public void AddPlayedBeat(TimedBeatId timedBeat, IInstrumentId instrument)
        {
            beatsDrawer.AddPlayedBeat(timedBeat, instrument);
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
            summary = null;
        }
    }
}
