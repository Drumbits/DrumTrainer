using System;
using System.Diagnostics;

namespace Drumz.Common.Utils
{
    public class FpsCounter
    {
        private readonly Stopwatch timer = new Stopwatch();
        private readonly float[] times;
        private int timeIndex;
        public FpsCounter(int frameWindow)
        {
            this.times = new float[frameWindow];
            timeIndex = 0;
        }
        public float AddFrame()
        {
            if (!timer.IsRunning)
                return -1f;
            var removed = times[timeIndex];
            var t = (float) timer.Elapsed.TotalSeconds;
            times[timeIndex] = t;
            timeIndex = (timeIndex + 1) % times.Length;
            if (removed == -1f) return -1f;
            return times.Length / (t - removed);
        }
        public void Start()
        {
            timer.Restart();
        }
        public void Stop()
        {
            timer.Stop();
        }
    }
}
