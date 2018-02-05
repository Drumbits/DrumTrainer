using System;
using System.Collections.Generic;
using System.Text;
using Drumz.Common.Beats;

namespace Drumz.Common.PlayAnalysis
{
    public delegate void MatchFoundEventHandler(PatternBeatsMatchReport report);
    public class PatternMatch
    {
        public readonly TimedEvent<Beat> PatternBeat;
        public readonly TimedEvent<Beat> RealizedBeat;
        public PatternMatch(TimedEvent<Beat> patternBeat, TimedEvent<Beat> realizedBeat)
        {
            this.RealizedBeat = realizedBeat;
            this.PatternBeat = patternBeat;
        }
    }
    public class PatternBeatsMatchReport
    {
        public readonly List<PatternMatch> Matches = new List<PatternMatch>();
        public readonly List<TimedEvent<Beat>> PatternUnMatched = new List<TimedEvent<Beat>>();
        public readonly List<TimedEvent<Beat>> RealizedUnMatched = new List<TimedEvent<Beat>>();

        public bool IsEmpty
        {
            get
            {
                return Matches.Count == 0 && PatternUnMatched.Count == 0 && RealizedUnMatched.Count == 0;
            }
        }
    }
    class PatternMatcher
    {
        public class Settings
        {
            public float MaxMatchingTime = 0.25f;
        }
        private readonly Settings settings;
        private readonly IBeatSequence patternAsBeatSequence;
        private readonly List<TimedEvent<Beat>> unmatchedRealizedBeats = new List<TimedEvent<Beat>>();
        private readonly List<TimedEvent<Beat>> unmatchedPatternBeats = new List<TimedEvent<Beat>>();
        private float t;
        private int nextPatternBeatIndex;

        public event MatchFoundEventHandler MatchFound;

        public void Tick(float newTime)
        {
            for (; nextPatternBeatIndex < patternAsBeatSequence.Size; ++nextPatternBeatIndex)
            {
                var beat = patternAsBeatSequence[nextPatternBeatIndex];
                if (beat.Time > newTime) break;
                unmatchedPatternBeats.Add(beat);
            }
            MatchBeats();
        }
        private void MatchBeats()
        {
            var report = new PatternBeatsMatchReport();
            var patternBeatIndex = 0;
            while (patternBeatIndex < unmatchedPatternBeats.Count)
            {
                int match;
                var patternBeat = unmatchedPatternBeats[patternBeatIndex];
                switch (TryFindMatch(patternBeat, unmatchedRealizedBeats, out match))
                {
                    case MatchFindResult.Found:
                        report.Matches.Add(new PatternMatch(patternBeat, unmatchedRealizedBeats[match]));
                        unmatchedPatternBeats.RemoveAt(patternBeatIndex);
                        unmatchedRealizedBeats.RemoveAt(match);
                        break;
                    case MatchFindResult.Out:
                        report.PatternUnMatched.Add(patternBeat);
                        unmatchedPatternBeats.RemoveAt(patternBeatIndex);
                        break;
                    case MatchFindResult.Pending:
                        ++patternBeatIndex;
                        break;
                    default:
                        throw new NotSupportedException("Unknown MatchFindResult");
                }
            }
            var realisedBeatIndex = 0;
            while (realisedBeatIndex < unmatchedRealizedBeats.Count)
            {
                int match;
                var realizedBeat = unmatchedRealizedBeats[realisedBeatIndex];
                switch (TryFindMatch(realizedBeat, unmatchedPatternBeats, out match))
                {
                    case MatchFindResult.Found:
                        throw new ArgumentOutOfRangeException("Unexpected match found");
                    case MatchFindResult.Out:
                        report.RealizedUnMatched.Add(realizedBeat);
                        unmatchedRealizedBeats.RemoveAt(realisedBeatIndex);
                        break;
                    case MatchFindResult.Pending:
                        ++realisedBeatIndex;
                        break;
                    default:
                        throw new NotSupportedException("Unknown MatchFindResult");
                }
            }
            if (!report.IsEmpty)
                OnMatchFound(report);
        }
        private void OnMatchFound(PatternBeatsMatchReport report)
        {
            MatchFound?.Invoke(report);
        }
        private MatchFindResult TryFindMatch(TimedEvent<Beat> beat, List<TimedEvent<Beat>> potentialMatches, out int match)
        {
            match = 0;
            foreach (var potentialMatch in potentialMatches)
            {
                if (potentialMatch.Time > beat.Time + settings.MaxMatchingTime)
                    return MatchFindResult.Out;
                if (Equals(potentialMatch.Value.Instrument, beat.Value.Instrument))
                    return MatchFindResult.Found;
                ++match;
            }
            return MatchFindResult.Pending;
        }
        private enum MatchFindResult { Found, Pending, Out};
    }
}
