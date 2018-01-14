using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drumz.Common
{
    public class Beat
    {
        public readonly IInstrumentId Instrument;
        public readonly Velocity Velocity;
        public Beat(IInstrumentId instrument, Velocity velocity)
        {
            this.Instrument = instrument;
            this.Velocity = velocity;
            if (velocity.Value == 0) throw new ArgumentException("Velocity should not be null " + this);
        }
        public override string ToString()
        {
            return Instrument.Name + ";" + Velocity.Value.ToString("0");
        }
    }
    public interface IBeatSequence
    {
        TimedEvent<Beat> this[int beatIndex] { get; }
        IEnumerable<TimedEvent<Beat>> Beats { get; }
        int Size { get; }
        RealInterval Span { get; }
        RealInterval BeatSpan { get; }
    }
    public static class BeatSequenceExtensionMethods
    {
        public static IEnumerable<IInstrumentId> Instruments(this IBeatSequence sq)
        {
            return sq.Beats.Select(beat => beat.Value.Instrument).Distinct();
        }
        public static IEnumerable<double> Times(this IBeatSequence sq)
        {
            return sq.Beats.Select(beat => beat.Time).Distinct();
        }
    }

    public class BeatSequence : IBeatSequence
    {
        private readonly TimedEvent<Beat>[] beats;
        private readonly RealInterval span;

        private BeatSequence(TimedEvent<Beat>[] beats, RealInterval span)
        {
            this.beats = beats;
            this.span = span;
        }
        public TimedEvent<Beat> this[int beatId]
        {
            get
            {
                return beats[beatId];
            }
        }
        public int Size { get { return beats.Length; } }
        public RealInterval BeatsInterval
        {
            get
            {
                return new RealInterval(beats[0].Time, beats[beats.Length - 1].Time);
            }
        }
        public int BeatsAfter(double t)
        {
            return beats.Aggregate(0, (count, beat) => (beat.Time >= t ? count + 1 : count));
        }
        public RealInterval Span
        {
            get
            {
                return span;
            }
        }
        public RealInterval BeatSpan
        {
            get { return new RealInterval(beats.First().Time, beats.Last().Time); }
        }
        public class BeatSequenceFactory
        {
            //private readonly RealInterval span;
            private readonly List<TimedEvent<Beat>> beats = new List<TimedEvent<Beat>>();
            private readonly List<IInstrumentId> instruments = new List<IInstrumentId>();

            public BeatSequenceFactory()
            {
            }
            public void AddBeat(double time, Beat beat)
            {
                if (!instruments.Contains(beat.Instrument)) instruments.Add(beat.Instrument);
                beats.Add(new TimedEvent<Beat>(time, beat));
            }
            public BeatSequence Build()
            {
                return Build(null);
            }
            public BeatSequence Build(RealInterval span)
            {
                beats.Sort(TimedEvent<Beat>.Comparer);
                var finalSpan = span ?? new RealInterval(beats.First().Time, beats.Last().Time);
                return new BeatSequence(beats.ToArray(), finalSpan);
            }
        }
        public IEnumerable<TimedEvent<Beat>> Beats
        {
            get
            {
                return beats;
            }
        }
    }
}
