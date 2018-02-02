using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Drumz.Common.Utils;

namespace Drumz.Common
{
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
    public sealed class Velocity
    {
        public static Velocity Softest = new Velocity(0.1);
        public static Velocity Soft = new Velocity(0.25);
        public static Velocity Medium = new Velocity(0.5);
        public static Velocity Loud = new Velocity(0.75);
        public static Velocity Loudest = new Velocity(1.0);
        public readonly double Value;
        public Velocity(double value)
        {
            this.Value = Math.Round(value, 3);
        }
        public override bool Equals(object obj)
        {
            var other = obj as Velocity;
            return other != null && other.Value == Value;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        public static bool operator ==(Velocity v1, Velocity v2)
        {
            return Equals(v1, v2);
        }
        public static bool operator !=(Velocity v1, Velocity v2)
        {
            return !Equals(v1, v2);
        }
        public override string ToString()
        {
            return "v:" + Value;
        }
    }
    public static class DrumInstruments
    {
        public static readonly IInstrumentId Crash = InstrumentDataBase.Value.FromString("Crash");
        public static readonly IInstrumentId Ride = InstrumentDataBase.Value.FromString("Ride");
        public static readonly IInstrumentId RideBell = InstrumentDataBase.Value.FromString("Ride (Bell)");
        public static readonly IInstrumentId HiHat = InstrumentDataBase.Value.FromString("HiHat (Closed)");
        public static readonly IInstrumentId HiHatOpen = InstrumentDataBase.Value.FromString("HiHat (Open)");
        public static readonly IInstrumentId HiHatFoot = InstrumentDataBase.Value.FromString("HiHat (Foot)");
        public static readonly IInstrumentId TomHigh = InstrumentDataBase.Value.FromString("High Tom");
        public static readonly IInstrumentId Snare = InstrumentDataBase.Value.FromString("Snare");
        public static readonly IInstrumentId TomMid = InstrumentDataBase.Value.FromString("Mid Tom");
        public static readonly IInstrumentId TomLow = InstrumentDataBase.Value.FromString("Low Tom");
        public static readonly IInstrumentId Kick = InstrumentDataBase.Value.FromString("Kick");
    }
    public interface IInstrumentId
    {
        string Name { get; }
        //string ShortName { get; }
    }
    public sealed class InstrumentDataBase
    {
        public static readonly InstrumentDataBase Value = new InstrumentDataBase();
        private readonly IDictionary<string, IInstrumentId> instruments = new Dictionary<string, IInstrumentId>(StringComparer.OrdinalIgnoreCase);

        public IInstrumentId FromString(string name)
        {
            IInstrumentId result;
            if (!instruments.TryGetValue(name, out result))
            {
                result = RegisterNewInstrument(name);
            }
            return result;
        }
        private IInstrumentId RegisterNewInstrument(string name)
        {
            lock (this)
            {
                IInstrumentId result;
                if (instruments.TryGetValue(name, out result)) return result;
                result = new InstrumentId(name, instruments.Count);
                instruments.Add(name, result);
                return result;
            }
        }
        private sealed class InstrumentId : IInstrumentId
        {
            private readonly string name;
            private readonly int uniqueId;
            public InstrumentId(string name, int uniqueId)
            {
                this.name = name;
                this.uniqueId = uniqueId;
            }
            public override int GetHashCode()
            {
                return uniqueId;
            }
            public override bool Equals(object obj)
            {
                var other = obj as InstrumentId;
                return other != null && other.uniqueId == uniqueId;
            }
            public override string ToString()
            {
                return name;
            }
            public string Name { get { return name; } }
        }
    }
    public class PatternInfo
    {
        public int BarsCount { get; private set; }
        public int BeatsPerBar { get; private set; }
        public int SuggestedBpm { get; private set; }
        public int UnitsPerBeat { get; private set; }
    }
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
        public static bool operator==(TimeInUnits t1, TimeInUnits t2)
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
    public class Pattern
    {
        public PatternInfo Info { get; private set; }
        public ImmutableArray<IInstrumentId> Instruments { get; private set; }
        private readonly ImmutableSortedDictionary<TimeInUnits, Velocity[]> beats;

        public IDictionary<TimeInUnits, Velocity> Beats(IInstrumentId instrument)
        {
            int index = Instruments.IndexOf(instrument);
            if (index == -1)
                throw new ArgumentException("Instrument not in pattern: " + instrument + ". Should be one of " + Instruments.ToNiceString());
            return beats.Where(kv => kv.Value[index] != null).ToDictionary(kv => kv.Key, kv => kv.Value[index]);
        }
        public TimeInUnits? NextEventTime(TimeInUnits t)
        {
            foreach (var eventTime in beats.Keys)
                if (eventTime > t) return eventTime;
            return null;
        }
    }
    
    public class PatternBuilder
    {
        private readonly IDictionary<IInstrumentId, bool[]> beats = new Dictionary<IInstrumentId, bool[]>();
        private readonly List<IInstrumentId> instruments = new List<IInstrumentId>();
        private readonly int barsCount;
        private readonly int beatsPerBar;
        private readonly int subBeatsPerBeat = 4;
        public int SuggestedBpm
        {
            get;
            set;
        }

        public void FillWith(Pattern pattern)
        {
            foreach (var instrument in pattern.Beats)
            {
                var beatsForInstrument = beats[instrument.Key];
                foreach (var beat in instrument.Value)
                {
                    double tIndex = beat.Time * subBeatsPerBeat;
                    int index = (int)Math.Round(tIndex, 0);
                    if (Math.Abs(index - tIndex) > 1e-6)
                        throw new ArgumentException("Incompatible beat time: " + beat.Time);
                    beatsForInstrument[index] = true;
                }
            }
            SuggestedBpm = pattern.SuggestedBpm;
        }
        public int SubBeatsPerBeat
        {
            get
            {
                return subBeatsPerBeat;
            }
        }
        public double Unit
        {
            get
            {
                return 1.0 / subBeatsPerBeat;
            }
        }
        public int BeatsPerBar
        {
            get
            {
                return beatsPerBar;
            }
        }
        public PatternBuilder(int barsCount, int beatsPerBar)
        {
            this.barsCount = barsCount;
            this.beatsPerBar = beatsPerBar;
        }
        public void Add(IInstrumentId instrument, int beatIndex)
        {
            if (!beats.ContainsKey(instrument))
                AddInstrument(instrument);
            beats[instrument][beatIndex] = true;
        }
        public void AddInstrument(IInstrumentId instrument)
        {
            beats.Add(instrument, new bool[BeatsCount]);
            instruments.Add(instrument);
        }
        public bool this[IInstrumentId instrument, int beatIndex]
        {
            get
            {
                return beats[instrument][beatIndex];
            }
        }
        public bool this[int instrument, int beatIndex]
        {
            get
            {
                return beats[instruments[instrument]][beatIndex];
            }
            set
            {
                beats[instruments[instrument]][beatIndex] = value;
            }
        }
        public bool[] this[IInstrumentId instrument]
        {
            get
            {
                return beats[instrument];
            }
        }
        public IEnumerable<IInstrumentId> Instruments
        {
            get
            {
                return beats.Keys;
            }
        }
        public int BeatsCount
        {
            get
            {
                return beatsPerBar * barsCount * subBeatsPerBeat;
            }
        }
        public Pattern Build()
        {
            var resultBeats = new Dictionary<IInstrumentId, IList<TimedEvent<Velocity>>>();
            foreach (var b in beats)
            {
                var beatsForInstrument = new List<TimedEvent<Velocity>>();
                for (int i = 0; i < b.Value.Length; ++i)
                    if (b.Value[i])
                        beatsForInstrument.Add(new TimedEvent<Velocity>(i / (double)subBeatsPerBeat, new Velocity(1.0)));
                if (beatsForInstrument.Count > 0)
                    resultBeats.Add(b.Key, beatsForInstrument);
            }
            return new Pattern(barsCount, beatsPerBar, resultBeats, SuggestedBpm);
        }
    }
    public static class PatternIO
    {
#if MOBILE
#else
        public static void Save(Pattern pattern, string path)
        {
            Save(pattern, new System.IO.StreamWriter(path));
        }
#endif
        public static void Save(Pattern pattern, System.IO.TextWriter saveTarget)
        {
            var data = ToData(pattern);
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PatternData));
            using (saveTarget)
            {
                serializer.Serialize(saveTarget, data);
            }
        }
#if MOBILE

#else
        public static Pattern Load(string path)
        {
            return Load(new System.IO.StreamReader(path));
        }
#endif
        public static Pattern Load(System.IO.TextReader reader)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PatternData));
            using (reader)
            {
                var data = (PatternData)serializer.Deserialize(reader);
                return ToPattern(data);
            }
        }
        public static BeatData ToData(TimedEvent<Velocity> instrumentBeat)
        {
            return new BeatData { Time = instrumentBeat.Time, Velocity = instrumentBeat.Value.Value };
        }
        public static InstrumentBeatsData ToData(KeyValuePair<IInstrumentId, IList<TimedEvent<Velocity>>> instrumentBeats)
        {
            return new InstrumentBeatsData
            {
                Instrument = instrumentBeats.Key.Name,
                Beats = instrumentBeats.Value.Select(ToData).ToArray()
            };
        }
        public static PatternData ToData(Pattern pattern)
        {
            return new PatternData { BarsCount = pattern.BarsCount, BeatsPerBar = pattern.beatsPerBar, SuggestedBpm = pattern.SuggestedBpm, Instruments = pattern.Beats.Select(ToData).ToArray() };
        }
        public static Pattern ToPattern(PatternData data)
        {
            var beats = new Dictionary<IInstrumentId, IList<TimedEvent<Velocity>>>(data.Instruments.Length);
            beats = data.Instruments.ToDictionary(
                kv => InstrumentDataBase.Value.FromString(kv.Instrument),
                kv => (IList<TimedEvent<Velocity>>)kv.Beats.Select(b => new TimedEvent<Velocity>(b.Time, new Velocity(b.Velocity))).ToList());
            return new Pattern(data.BarsCount, data.BeatsPerBar, beats, data.SuggestedBpm);
        }
    }
    public class PatternData
    {
        public int BarsCount;
        public int BeatsPerBar;
        public int SuggestedBpm = 0;
        public InstrumentBeatsData[] Instruments;

    }
    public class InstrumentBeatsData
    {
        public string Instrument;
        public BeatData[] Beats;
    }
    public class BeatData
    {
        public double Time;
        public double Velocity;
    }
}
