namespace Drumz.Common.PlayAnalysis
{
    public class BeatsMatch
    {
        public readonly int InstrumentIndex;
        public readonly TimedBeat PatternBeat;
        public readonly TimedBeat PlayedBeat;
        public readonly float Accuracy;// from -1 (max too early) to +1 (max too late)

        public BeatsMatch(int instrumentIndex, TimedBeat patternBeat, TimedBeat playedBeat, float accuracy)
        {
            InstrumentIndex = instrumentIndex;
            PatternBeat = patternBeat;
            PlayedBeat = playedBeat;
            Accuracy = accuracy;
        }
    }
}
