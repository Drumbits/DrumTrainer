namespace Drumz.Common.PlayAnalysis
{
    public class BeatsMatch
    {
        public readonly TimedBeatId PatternBeat;
        public readonly TimedBeatId PlayedBeat;
        public readonly float Accuracy;// from -1 (max too early) to +1 (max too late)

        public BeatsMatch(TimedBeatId patternBeat, TimedBeatId playedBeat, float accuracy)
        {
            PatternBeat = patternBeat;
            PlayedBeat = playedBeat;
            Accuracy = accuracy;
        }
    }
}
