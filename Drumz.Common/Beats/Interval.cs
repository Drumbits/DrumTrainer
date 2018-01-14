using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drumz.Common
{
    public class RealInterval
    {
        public readonly double Start;
        public readonly double End;

        public RealInterval(double start, double end)
        {
            this.Start = start;
            this.End = end;
        }
        public bool Contains(double t)
        {
            return Start <= t && t <= End;
        }
        public double Length { get { return End - Start; } }
        public override string ToString()
        {
            return "[" + Start.ToString("0.0") + "; " + End.ToString("0.0") + "]";
        }
    }

    public class IntegerInterval
    {
        public readonly int Start;
        public readonly int End;

        public IntegerInterval(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }
        public bool Contains(int t)
        {
            return Start <= t && t <= End;
        }
        public int Length { get { return End - Start; } }
    }
}
