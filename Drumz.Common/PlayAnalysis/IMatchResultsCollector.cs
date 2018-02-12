namespace Drumz.Common.PlayAnalysis
{
    public interface IMatchResultsCollector
    {
        void Match(BeatsMatch match);
        void MissedBeat(MissedBeat match);
    }
}
