using System.Collections.Generic;
using System.Linq;
using Drumz.Common.Beats;

namespace Drumz.Common.PlayAnalysis
{
    public class PatternMatcher
    {
        public static PatternMatcher Create(PatternBeatIds pattern, PatternInfo patternInfo, Settings settings, IMatchResultsCollector resultsCollector)
        {
            var patternBeatLists = ContinuousBeatsLooper.FromPattern(pattern, patternInfo).ToDictionary(
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
        private readonly Dictionary<int, SingleInstrumentBeatsMatcher> perInstrumentMatchers;
        private readonly IMatchResultsCollector resultsCollector;

        private PatternMatcher(Settings settings, Dictionary<int, SingleInstrumentBeatsMatcher> perInstrumentMatchers, IMatchResultsCollector resultsCollector)
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
        public void AddBeat(int instrumentIndex, TimedBeat beat, Velocity v)
        {
            perInstrumentMatchers[instrumentIndex].AddPlayed(beat, resultsCollector);
        }
    }
}
