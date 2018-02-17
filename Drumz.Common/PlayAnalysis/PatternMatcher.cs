using System.Collections.Generic;
using System.Linq;
using Drumz.Common.Beats;

namespace Drumz.Common.PlayAnalysis
{
    public class PatternMatcher
    {
        public static PatternMatcher Create(Pattern pattern, Settings settings, IMatchResultsCollector resultsCollector)
        {
            var patternBeatLists = ContinuousBeatsLooper.FromPattern(pattern).ToDictionary(
                kv => kv.Key,
                kv => new SingleInstrumentBeatsMatcher(kv.Key,
                    new PatternBeatsTimesList(
                        new BeatTimesList(settings.MaxMatchingTime),
                        kv.Value),
                    new BeatTimesList(settings.MaxMatchingTime),
                    settings));
            return new PatternMatcher(settings, patternBeatLists, resultsCollector);
        }
        public class Settings
        {
            public float MaxMatchingTime = 0.5f;
        }
        private readonly Settings settings;
        private readonly Dictionary<IInstrumentId, SingleInstrumentBeatsMatcher> perInstrumentMatchers;
        private readonly IMatchResultsCollector resultsCollector;

        private PatternMatcher(Settings settings, Dictionary<IInstrumentId, SingleInstrumentBeatsMatcher> perInstrumentMatchers, IMatchResultsCollector resultsCollector)
        {
            this.settings = settings;
            this.perInstrumentMatchers = perInstrumentMatchers;
            this.resultsCollector = resultsCollector;
        }
        public void Reset()
        {
            foreach (var matcher in perInstrumentMatchers.Values)
                matcher.Reset();
        }
        public void Tick(float newTime)
        {
            foreach (var matcher in perInstrumentMatchers.Values)
                matcher.Tick(newTime, resultsCollector);
        }
        public void AddBeat(IInstrumentId instrumentId, TimedBeatId beat, Velocity v)
        {
            perInstrumentMatchers[instrumentId].AddPlayed(beat, resultsCollector);
        }
    }
}
