using System;
using System.Linq;

namespace Drumz.Common.PlayAnalysis
{
    public class BeatsRegister
    {
        private readonly float[] times;
        private short nextIndex;

        public BeatsRegister(int maxConcurrenBeats)
        {
            this.times = Enumerable.Repeat(-1f, maxConcurrenBeats).ToArray();
            nextIndex = 0;
        }

        public short Register(float time)
        {
            if (times[nextIndex] != -1f)
                throw new ArgumentException("Access to non released beat: [" + nextIndex + "]");
            var result = nextIndex;
            times[nextIndex++] = time;
            if (nextIndex == times.Length)
                nextIndex = 0;
            return result;
        }

        public float Time(short beatId)
        {
            return times[beatId];
        }
        public void Release(short beatIndex)
        {
            if (times[beatIndex] == -1f)
                throw new ArgumentException("Trying to release already released beat: [" + beatIndex + "]");
            times[beatIndex] = -1f;

        }
    }
}
