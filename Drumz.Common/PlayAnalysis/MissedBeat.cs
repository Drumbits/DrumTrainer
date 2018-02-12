namespace Drumz.Common.PlayAnalysis
{
    public class MissedBeat
    {
        public readonly int InstrumentIndex;
        public readonly TimedBeat Beat;

        public MissedBeat(int instrumentIndex, TimedBeat beat)
        {
            InstrumentIndex = instrumentIndex;
            Beat = beat;
        }
    }
}
