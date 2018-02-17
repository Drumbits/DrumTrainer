namespace Drumz.Common.PlayAnalysis
{
    public class MissedBeat
    {
        public readonly int InstrumentIndex;
        public readonly TimedBeatId Beat;

        public MissedBeat(int instrumentIndex, TimedBeatId beat)
        {
            InstrumentIndex = instrumentIndex;
            Beat = beat;
        }
    }
}
