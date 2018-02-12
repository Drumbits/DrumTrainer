using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Drumz.UI
{
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
}
