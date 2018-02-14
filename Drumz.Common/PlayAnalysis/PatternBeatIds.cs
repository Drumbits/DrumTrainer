using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Drumz.Common.Beats;

namespace Drumz.Common.PlayAnalysis
{
    /// <summary>
    /// (tricky) convention: pattern beat ids are negative, starting from -1
    /// This means there should be not overlap with played beats (which are positive starting from 1)
    /// </summary>
    public class PatternBeatIds : IEnumerable<BeatId>
    {
        public static PatternBeatIds Create(Pattern pattern)
        {
            return new PatternBeatIds(new PatternBeat[] { null }.Concat(pattern.AllBeats()).ToArray());
        }
        // first element is null, to be consistent with pattern beat ids indexing strarting from 1.
        private readonly PatternBeat[] beats;

        private PatternBeatIds(PatternBeat[] beats)
        {
            this.beats = beats;
        }
        public PatternBeat Beat(BeatId id)
        {
            var index = -id.Index;
            if (index < 1 || index >= beats.Length)
                throw new ArgumentException("Invalid pattern beat id: " + id.Index);
            return beats[index];
        }
        public IEnumerator<BeatId> GetEnumerator()
        {
            return Enumerable.Range(1, beats.Length-1).Select(i => new BeatId((short)-i)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public int Count { get
            { return beats.Length - 1; }
        }
    }
}
