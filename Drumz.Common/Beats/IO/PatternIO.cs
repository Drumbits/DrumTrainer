using System;
using System.Collections.Generic;
using System.Linq;

namespace Drumz.Common.Beats.IO
{
    public class PatternParsingException : Exception
    {
        public PatternParsingException(string message) : base(message) { }
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
        private static InstrumentData ToData(IInstrumentId instrument, int index)
        {
            return new InstrumentData { Name = instrument.Name, Id = index };
        }
        private static BeatData ToData(Tuple<TimeInUnits, int, Velocity> beat)
        {
            return new BeatData { Time = beat.Item1.Index, Instrument = beat.Item2, Velocity = beat.Item3.Value };
        }
        public static PatternData ToData(Pattern pattern)
        {
            return new PatternData
            {
                BarsCount = pattern.Info.BarsCount,
                BeatsPerBar = pattern.Info.BeatsPerBar,
                TimeUnitsPerBeat = pattern.Info.UnitsPerBeat.Index,
                SuggestedBpm = pattern.Info.SuggestedBpm,
                Instruments = pattern.Instruments.Select(ToData).ToArray(),
                Beats = pattern.AllBeats().Select(ToData).ToArray()
            };
        }
        public static Pattern ToPattern(PatternData data)
        {
            var idToIndex = new Dictionary<int, int>();
            foreach (var instrumentData in data.Instruments)
            {
                if (idToIndex.ContainsKey(instrumentData.Id))
                    throw new PatternParsingException("Duplicate instrument id: " + instrumentData.Id
                        + ". Used by \""
                        + data.Instruments[idToIndex[instrumentData.Id]].Name
                        + "\" and \""
                        + instrumentData.Name
                        + "\"");
                idToIndex.Add(instrumentData.Id, idToIndex.Count);
            }
            var beats = new SortedDictionary<TimeInUnits, Velocity[]>();
            int index = 0;
            foreach (var beatData in data.Beats)
            {
                var t = new TimeInUnits(beatData.Time);
                Velocity[] beatsAtT;
                if (!beats.TryGetValue(t, out beatsAtT))
                {
                    beatsAtT = new Velocity[data.Instruments.Length];
                    beats.Add(t, beatsAtT);
                }
                int instrumentIndex;
                if (!idToIndex.TryGetValue(beatData.Instrument, out instrumentIndex))
                    throw new PatternParsingException("Invalid instrument id for beat #" + index + "(t=" + beatData.Time + ", id=" + beatData.Instrument + ")");
                if (beatsAtT[instrumentIndex] != null)
                    throw new PatternParsingException("Duplicate beat on " + data.Instruments[instrumentIndex].Name + " at t=" + beatData.Time + "(beat #" + index+ ")");
                beatsAtT[instrumentIndex] = new Velocity(beatData.Velocity);
                ++index;
            }
            var patternInfo = new PatternInfo.Builder();
            patternInfo.BarsCount = data.BarsCount;
            patternInfo.BeatsPerBar = data.BeatsPerBar;
            patternInfo.SuggestedBpm = data.SuggestedBpm;
            patternInfo.UnitsPerBeat = new TimeInUnits(data.TimeUnitsPerBeat);
            var instruments = new List<IInstrumentId>(data.Instruments.Select(i => new SimpleInstrumentId(i.Name)));
            return new Pattern(patternInfo.Build(), instruments, beats);
        }
    }
}
