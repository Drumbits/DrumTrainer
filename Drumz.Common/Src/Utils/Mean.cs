using System;
using System.Collections.Generic;
using System.Text;

namespace Drumz.Common.Utils
{
    public class MeanEstimation
    {
        private double total=0;
        private int count = 0;
        public MeanEstimation() { }
        public void Add(double value)
        {
            total += value;
            ++count;
        }
        public int Count { get { return count; } }
        public double Value { get { return count > 0 ? total / count : double.NaN; } }
    }
    public class ProbabilityEstimation
    {
        private readonly MeanEstimation mean = new MeanEstimation();

        public void Add(bool occured)
        {
            mean.Add(occured ? 1 : 0);
        }
        public int Count { get { return mean.Count; } }
        public double Value { get { return mean.Value; } }
    }
}
