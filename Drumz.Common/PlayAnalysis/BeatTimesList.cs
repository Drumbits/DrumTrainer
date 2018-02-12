using System;
using System.Collections.Generic;
using System.Linq;

namespace Drumz.Common.PlayAnalysis
{
    public class BeatTimesList
    {
        private readonly float keepWindow;
        private readonly System.Collections.Generic.Queue<TimedBeat> times = new System.Collections.Generic.Queue<TimedBeat>();

        public BeatTimesList(float keepWindow)
        {
            this.keepWindow = keepWindow;
        }

        public void Tick(float time, Action<TimedBeat> discardBeat)
        {
            var timeLimit = time - keepWindow;
            while (times.Count > 0 && times.Peek().T < timeLimit)
                discardBeat(times.Dequeue());
        }
        public void Add(TimedBeat beat)
        {
            times.Enqueue(beat);
        }
        public bool IsEmpty { get { return times.Count == 0; } }
        public TimedBeat Next { get { return times.Peek(); } }
        public TimedBeat RemoveNext()
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
