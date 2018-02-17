using Drumz.Common.Beats;

namespace Drumz.Common.PlayAnalysis
{
    public struct TimedBeatId
    {
        public float T;
        public BeatId Id;
        public TimedBeatId(float t, BeatId id)
        {
            this.T = t;
            this.Id = id;
        }
        public TimedBeatId Offset(float offset)
        {
            return new TimedBeatId(T + offset, Id);
        }
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType()) return false;
            var other = (TimedBeatId)obj;
            return other.T == T && other.Id.Index == Id.Index;
        }
        public override int GetHashCode()
        {
            return Id.Index;
        }
    }
}
