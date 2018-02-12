namespace Drumz.Common.PlayAnalysis
{
    public struct BeatId
    {
        public short Index;
        public BeatId(short index) { this.Index = index; }
        public bool IsPattern { get { return Index < 0; } }
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType()) return false;
            var other = (BeatId)obj;
            return other.Index == Index;
        }
        public override int GetHashCode()
        {
            return Index;
        }
    }
}
