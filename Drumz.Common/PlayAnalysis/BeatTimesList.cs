using System;
using System.Collections.Generic;
using System.Linq;

namespace Drumz.Common.PlayAnalysis
{
    public class BeatTimesList
    {
        private readonly float keepWindow;
        private readonly System.Collections.Generic.Queue<TimedBeatId> times = new System.Collections.Generic.Queue<TimedBeatId>();

        public BeatTimesList(float keepWindow)
        {
            this.keepWindow = keepWindow;
        }

        public void Tick(float time, Action<TimedBeatId> discardBeat)
        {
            var timeLimit = time - keepWindow;
            while (times.Count > 0 && times.Peek().T < timeLimit)
                discardBeat(times.Dequeue());
        }
        public void Add(TimedBeatId beat)
        {
            times.Enqueue(beat);
        }
        public bool IsEmpty { get { return times.Count == 0; } }
        public TimedBeatId Next { get { return times.Peek(); } }
        public TimedBeatId RemoveNext()
        {
            return times.Dequeue();
        }
        public float KeepWindow
        {
            get
            {
                return keepWindow;
            }
        }
        public IEnumerable<float> Content
        {
            get
            {
                return times.Select(idAndT => idAndT.T);
            }
        }
        public int Count
        {
            get
            {
                return times.Count;
            }
        }
        public void Clear()
        {
            times.Clear();
        }
    }
}
