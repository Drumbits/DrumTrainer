using System;
using System.Collections.Generic;
using System.Linq;
using Drumz.Common.Utils;

namespace Drumz.Common.Beats
{
    public sealed class DiscretizedBeat
    {
        public static DiscretizedBeat New(IInstrumentId instrument, Velocity velocity, TimeInUnits t)
        {
            return new DiscretizedBeat(new Beat(instrument, velocity), t);
        }
        public readonly Beat Beat;
        public readonly TimeInUnits T;

        public DiscretizedBeat(Beat beat, TimeInUnits t)
        {
            this.Beat = beat;
            this.T = t;
        }
    }
    public class TimedEvent<T>
    {
        public static readonly IComparer<TimedEvent<T>> Comparer = new ComparerClass();

        public TimedEvent(double t, T value)
        {
            this.Time = t;
            this.Value = value;
        }
        public double Time { get; private set; }
        public T Value { get; private set; }

        private sealed class ComparerClass : IComparer<TimedEvent<T>>
        {
            public int Compare(TimedEvent<T> x, TimedEvent<T> y)
            {
                return System.Collections.Generic.Comparer<double>.Default.Compare(x.Time, y.Time);
            }
        }
    }
    public interface IPattern
    {
        PatternInfo Info { get;}
        IReadOnlyList<IInstrumentId> Instruments { get;}
        IDictionary<TimeInUnits, Velocity> Beats(int instrumentIndex);
        TimeInUnits? NextEventTime(TimeInUnits t);
    }
    public static class PatternExtensionMethods
    {
        public static int IndexOf(this IReadOnlyList<IInstrumentId> instruments, IInstrumentId searched)
        {
            var result = 0;
            foreach (var ins in instruments)
            {
                if (Equals(ins, searched))
                    return result;
                ++result;
            }
            return -1;
        }
        public static IDictionary<TimeInUnits, Velocity> Beats(this IPattern pattern, IInstrumentId instrument)
        {
            int index = pattern.Instruments.IndexOf(instrument);
            if (index == -1)
                throw new ArgumentException("Instrument not in pattern: " + instrument + ". Should be one of " + pattern.Instruments.ToNiceString());
            return pattern.Beats(index);
        }
    }
    public class Pattern : IPattern
    {
        private readonly SortedDictionary<TimeInUnits, Velocity[]> beats;
        public Pattern(PatternInfo info, IEnumerable<IInstrumentId> instruments) : this(info, new List<IInstrumentId>(instruments), new SortedDictionary<TimeInUnits, Velocity[]>())
        {
        }
        internal Pattern(PatternInfo info, List<IInstrumentId> Instruments, SortedDictionary<TimeInUnits, Velocity[]> beats)
        {
            this.Info = info;
            this.Instruments = Instruments;
            this.beats = beats;
        }
        public PatternInfo Info { get; private set; }
        public List<IInstrumentId> Instruments { get; private set; }

        public IDictionary<TimeInUnits, Velocity> Beats(int instrumentIndex)
        {
            return beats.Where(kv => kv.Value[instrumentIndex] != null).ToDictionary(kv => kv.Key, kv => kv.Value[instrumentIndex]);
        }
        public Velocity[] Beats(TimeInUnits t)
        {
            return beats.TryGetValue(t, out Velocity[] result) ? result : null;
        }
        public TimeInUnits? NextEventTime(TimeInUnits t)
        {
            foreach (var eventTime in beats.Keys)
                if (eventTime > t) return eventTime;
            return null;
        }
        public IEnumerable<Tuple<TimeInUnits, int, Velocity>> AllBeats()
        {
            return beats.SelectMany(tv => tv.Value.Select((v,i) => v!= null ? new Tuple<TimeInUnits, int, Velocity>(tv.Key,i,v) : null).Where(r => r!= null));
        }
        public void Add(TimeInUnits t, IInstrumentId instrument, Velocity v)
        {
            var instrumentIndex = Instruments.IndexOf(instrument);
            if (instrumentIndex == -1)
            {
                throw new NotSupportedException("Pattern class does not support adding instruments. Build a new one instead");
            }
            Velocity[] beatsAtT;
            if (!beats.TryGetValue(t, out beatsAtT))
            {
                beatsAtT = new Velocity[Instruments.Count];
                beats.Add(t, beatsAtT);
            }
            beatsAtT[instrumentIndex] = v;
        }
        IReadOnlyList<IInstrumentId> IPattern.Instruments { get; }
    }

}
