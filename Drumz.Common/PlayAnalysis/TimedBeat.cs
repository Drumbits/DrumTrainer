namespace Drumz.Common.PlayAnalysis
{
    public struct TimedBeat
    {
        public float T;
        public BeatId Id;
        public TimedBeat(float t, BeatId id)
        {
            this.T = t;
            this.Id = id;
        }
        public TimedBeat Offset(float offset)
        {
            return new TimedBeat(T + offset, Id);
        }
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType()) return false;
            var other = (TimedBeat)obj;
            return other.T == T && other.Id.Index == Id.Index;
        }
        public override int GetHashCode()
        {
            return Id.Index;
        }
    }
}
