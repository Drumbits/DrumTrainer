using System;
using System.Linq;
using Drumz.Common.Beats;
using Drumz.Common.Utils;

namespace Drumz.Common.PlayAnalysis
{
    public class AccuracySummary
    {
        private uint missOccurences = 0;
        private uint lateOccurences = 0;
        private uint earlyOccurences = 0;
        private double cumulatedAccuracy = 0;
        private readonly MeanEstimation absoluteAccuracy = new MeanEstimation();
        public void AddMiss()
        {
            ++missOccurences;
            absoluteAccuracy.Add(0);
        }
        public void Add(float accuracy)
        {
            cumulatedAccuracy += accuracy;
            if (accuracy < -0.1)
                ++earlyOccurences;
            else if (accuracy > 0.1)
                ++lateOccurences;
            absoluteAccuracy.Add(1 - Math.Abs(accuracy));
        }
        public double Miss { get { return missOccurences / (double)absoluteAccuracy.Count; } }
        public double Early { get { return earlyOccurences / (double)absoluteAccuracy.Count; } }
        public double Late { get { return lateOccurences / (double)absoluteAccuracy.Count; } }
        public double Average { get { return cumulatedAccuracy / (absoluteAccuracy.Count - missOccurences); } }
        public double Value { get { return absoluteAccuracy.Value; } }
    }

    public class PerformanceSummary
    {
        private readonly AccuracySummary[] summaries;
        public Pattern Pattern { get; private set; }

        public PerformanceSummary(Pattern patternBeats)
        {
            this.Pattern = patternBeats;
            this.summaries = patternBeats.Ids.Select(b => new AccuracySummary()).ToArray();
        }

        public AccuracySummary BeatSummary(BeatId patternBeatId)
        {
            return summaries[-patternBeatId.Index - 1];
        }
    }
}
