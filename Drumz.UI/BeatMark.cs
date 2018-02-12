using SkiaSharp;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI
{
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
}
