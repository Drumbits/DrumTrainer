using System;
using Drumz.Common.Utils;

namespace Drumz.Common.PlayAnalysis
{
    public class SingleInstrumentBeatsMatcher
    {
        private readonly int instrumentIndex;
        private readonly PatternBeatsTimesList patternBeats;
        private readonly BeatTimesList playedBeats;
        private readonly PatternMatcher.Settings settings;

        public SingleInstrumentBeatsMatcher(int instrumentIndex, PatternBeatsTimesList patternBeats, BeatTimesList playedBeats, PatternMatcher.Settings settings)
        {
            this.instrumentIndex = instrumentIndex;
            this.patternBeats = patternBeats;
            this.playedBeats = playedBeats;
            this.settings = settings;
        }

        public void AddPlayed(TimedBeat beat, IMatchResultsCollector results)
        {
            if (!patternBeats.Beats.IsEmpty)
            {
                var diff = beat.T - patternBeats.Beats.Next.T;
                if (Math.Abs(diff) <= settings.MaxMatchingTime)
                {
                    var match = new BeatsMatch(instrumentIndex, patternBeats.Beats.RemoveNext(), beat, Accuracy(diff));
                    results.Match(match);
                    return;
                }
            }
            playedBeats.Add(beat);
        }
        private float previousT = 0f;
        public void Tick(float t, IMatchResultsCollector results)
        {
            //if (instrumentIndex == 2)
            //    Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Debug, "t: {0}, previousT: {1}", t, previousT);

            lock (playedBeats)
            {
                /*if (instrumentIndex == 2 && t - previousT > 0.1f)
                {
                    previousT = t;
                    Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Debug, "Pattern: {0}", patternBeats.Beats.Content.ToNiceString());
                    Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Debug, "Played: {0}", playedBeats.Content.ToNiceString());
                }*/
                patternBeats.Tick(t, b => results.MissedBeat(new MissedBeat(instrumentIndex, b)));
                playedBeats.Tick(t, b => results.MissedBeat(new MissedBeat(instrumentIndex, b)));

                LookForMatches(results.Match);
            }
        }
        public void Reset()
        {
            playedBeats.Clear();
            patternBeats.Reset();
            previousT = 0f;
            if (instrumentIndex == 2)
            {
                Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Debug, "Reset");
            }
        }
        private void LookForMatches(Action<BeatsMatch> matchFound)
        {
            if (patternBeats.Beats.IsEmpty || playedBeats.IsEmpty) return;
            var diff = playedBeats.Next.T - patternBeats.Beats.Next.T;
            if (Math.Abs(diff) > settings.MaxMatchingTime) return;
            var match = new BeatsMatch(instrumentIndex, patternBeats.Beats.RemoveNext(), playedBeats.RemoveNext(), Accuracy(diff));
            matchFound(match);
            LookForMatches(matchFound);
        }
        private float Accuracy(float diff)
        {
            return diff / (1.01f * settings.MaxMatchingTime);
        }
    }
}
