using System;
using System.Collections.Generic;
using System.Text;

namespace Drumz.Common.Beats
{
    public struct TimeInUnits : IEquatable<TimeInUnits>, IComparable<TimeInUnits>
    {
        public int Index;
        public TimeInUnits(int index)
        {
            this.Index = index;
        }
        public override int GetHashCode()
        {
            return Index;
        }
        public override bool Equals(object obj)
        {
            if (obj is TimeInUnits) return Equals((TimeInUnits)obj);
            return false;
        }

        public bool Equals(TimeInUnits other)
        {
            return other.Index == Index;
        }

        public int CompareTo(TimeInUnits other)
        {
            return Index.CompareTo(other.Index);
        }
        public static bool operator ==(TimeInUnits t1, TimeInUnits t2)
        {
            return t1.Index == t2.Index;
        }
        public static bool operator !=(TimeInUnits t1, TimeInUnits t2)
        {
            return t1.Index != t2.Index;
        }
        public static bool operator <(TimeInUnits t1, TimeInUnits t2)
        {
            return t1.Index < t2.Index;
        }
        public static bool operator >(TimeInUnits t1, TimeInUnits t2)
        {
            return t1.Index > t2.Index;
        }
    }
}
