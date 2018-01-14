using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drumz.Common.Diagnostics;

namespace Drumz.Common
{
    public class PatternDiscretization
    {
        public static readonly PatternDiscretization Default = new PatternDiscretization(4, 2, 2);
        private const int maxSubBeatsPerBeat = 24;
        public static PatternDiscretization ForPattern(Pattern pattern)
        {
            int subBeatsPerBeat = 1;
            bool failedOnSomeBeats = false;
            foreach (var instrumentAndBeats in pattern.Beats)
            {
                foreach (var beat in instrumentAndBeats.Value)
                {
                    if (SafeIndex(beat.Time, subBeatsPerBeat).HasValue) continue;
                    int multiplier = 2;
                    while (true)
                    {
                        var newDiscretization = multiplier * subBeatsPerBeat;
                        if (newDiscretization > maxSubBeatsPerBeat)
                        {
                            failedOnSomeBeats = true;
                            break;
                        }
                        if (SafeIndex(beat.Time, newDiscretization).HasValue)
                        {
                            subBeatsPerBeat = newDiscretization;
                            break;
                        }
                        ++multiplier;
                    }
                }
            }
            if (failedOnSomeBeats)
                Logger.Instance.Tell(Logger.Level.Warning, "Optimal grid unit calculation failed on some beats");
            return new PatternDiscretization(pattern.beatsPerBar, pattern.BarsCount, subBeatsPerBeat);
        }
        public int BeatsPerBar { get; private set; }
        public int BarsCount { get; private set; }
        public int SubBeatsPerBeat { get; private set; }
        public double gridUnit { get { return 1.0 / SubBeatsPerBeat; } }
        public PatternDiscretization(int beatsPerBar, int barsCount, int subBeatsPerBeat)
        {
            this.BeatsPerBar = beatsPerBar;
            this.BarsCount = barsCount;
            this.SubBeatsPerBeat = subBeatsPerBeat;
        }
        public int GridSize
        {
            get
            {
                return BeatsPerBar * BarsCount * SubBeatsPerBeat;
            }
        }
        public double Time(int index)
        {
            return index / (double)SubBeatsPerBeat;
        }
        public int Index(double time)
        {
            var index = SafeIndex(time);
            if (!index.HasValue)
                throw new ArgumentException("Time not supported by discretization: " + time);
            return index.Value;
        }
        public int? SafeIndex(double time)
        {
            return SafeIndex(time, SubBeatsPerBeat);
        }
        private static int? SafeIndex(double time, int subBeatsPerBeat)
        {
            var indexAsDouble = time * subBeatsPerBeat;
            var index = (int)Math.Round(indexAsDouble, 0);
            if (Math.Abs(indexAsDouble - index) > 1e-8)
                return null;
            return index;
        }
    }
}
