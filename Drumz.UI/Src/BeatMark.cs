using SkiaSharp;
using Drumz.Common.Beats;

namespace Drumz.UI
{
    public class BeatMark
    {
        public readonly BeatId Id;
        public readonly SKPoint Coord;
        public SKPaint Paint;
        public readonly float Expiry;
        public readonly string Mark;

        public BeatMark(BeatId id, SKPoint coord, SKPaint paint, float expiry, string mark)
        {
            Id = id;
            Coord = coord;
            Paint = paint;
            this.Expiry = expiry;
            this.Mark = mark;
        }
    }
}
