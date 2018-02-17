using System;
using System.Collections.Generic;
using System.Linq;
using Drumz.Common.Utils;

namespace Drumz.Common.Beats
{
    public class TimedEvent<T>
    {
        public static readonly IComparer<TimedEvent<T>> Comparer = new ComparerClass();

        public TimedEvent(float t, T value)
        {
            this.Time = t;
            this.Value = value;
        }
        public float Time { get; private set; }
        public T Value { get; private set; }

        private sealed class ComparerClass : IComparer<TimedEvent<T>>
        {
            public int Compare(TimedEvent<T> x, TimedEvent<T> y)
            {
                return System.Collections.Generic.Comparer<double>.Default.Compare(x.Time, y.Time);
            }
        }
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
    }
    public class PatternBeat
    {
        public readonly TimeInUnits T;
        public readonly ISoundId Sound;
        public readonly Velocity Velocity;

        public PatternBeat(TimeInUnits t, ISoundId sound, Velocity velocity)
        {
            T = t;
            Sound = sound;
            Velocity = velocity;
        }
    }
    public class Pattern
    {
        private readonly PatternBeat[] beats;

        private Pattern(PatternInfo info, PatternBeat[] beats, SoundsList sounds)
        {
            this.Info = info;
            this.Sounds = sounds;
            this.beats = beats;
        }
        public PatternInfo Info { get; private set; }
        public SoundsList Sounds { get; private set; }
        public PatternBeat Beat(BeatId id)
        {
            //todo: change this silly negative index thing
            var index = -(id.Index + 1);
            if (!id.IsPattern || index < 0 || index >= beats.Length)
                throw new ArgumentException("Invalid pattern beat id: " + id.Index);
            return beats[index];
        }
        public IEnumerable<BeatId> Ids
        {
            get
            {
                return Enumerable.Range(1, beats.Length).Select(i => new BeatId(-i));
            }
        }
        public IEnumerable<PatternBeat> AllBeats()
        {
            return beats;
        }
        public IEnumerable<TimedEvent<Beat>> ToBeatSequence()
        {
            return beats.Select(b => new TimedEvent<Beat>(Info.TimeInBeats(b.T), new Beat(b.Sound, b.Velocity)));
        }
        public class Builder
        {
            private readonly SortedList<TimeInUnits, List<PatternBeat>> beats = new SortedList<TimeInUnits, List<PatternBeat>>();
            private PatternInfo patternInfo;
            private readonly List<ISoundId> preferredInstrumentsOrder;

            public Builder() : this(new ISoundId[0]) { }

            public Builder(ISoundId[] preferredInstrumentsOrder)
            {
                this.preferredInstrumentsOrder = new List<ISoundId>(preferredInstrumentsOrder);
            }

            public PatternInfo PatternInfo { set { patternInfo = value; } }
            public void Add(TimeInUnits t, ISoundId sound, Velocity v)
            {
                List<PatternBeat> beatsAtT;
                if (!beats.TryGetValue(t, out beatsAtT))
                {
                    beatsAtT = new List<PatternBeat>(1);
                    beats.Add(t, beatsAtT);
                }
                var patternBeat = new PatternBeat(t, sound, v);
                if (beatsAtT.Any(p => Equals(p.Sound, sound)))
                    throw new ArgumentException(string.Format("Duplicate beat on {0} at t={1}", sound.Name(), t.Index));
                beatsAtT.Add(patternBeat);
                if (!preferredInstrumentsOrder.Contains(sound))
                    preferredInstrumentsOrder.Add(sound);
            }
            public Pattern Build()
            {
                if (patternInfo == null) throw new ArgumentException("PatternInfo not set");
                return new Pattern(patternInfo, beats.Values.SelectMany(b => b).ToArray(), new SoundsList(preferredInstrumentsOrder));
            }
        }
    }

}
