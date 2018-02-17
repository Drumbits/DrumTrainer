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
            Save(pattern, new System.IO.FileStream(path, System.IO.FileMode.Create));
        }
#endif
        public static void Save(Pattern pattern, System.IO.Stream saveTarget)
        {
            var data = ToData(pattern);
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(PatternData));
            using (saveTarget)
            {
                serializer.WriteObject(saveTarget, data);
            }
        }
#if MOBILE

#else
        public static Pattern Load(string path)
        {
            return Load(new System.IO.FileStream(path, System.IO.FileMode.Open));
        }
#endif
        public static Pattern Load(System.IO.Stream reader)
        {
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(PatternData));
            using (reader)
            {
                var data = (PatternData)serializer.ReadObject(reader);
                return ToPattern(data);
            }
        }
        private static InstrumentData ToData(IInstrumentId instrument, int index)
        {
            return new InstrumentData { Name = instrument.Name, Id = index };
        }
        private static BeatData ToData(PatternBeat beat, List<IInstrumentId> instruments)
        {
            return new BeatData { Time = beat.T.Index, Instrument = instruments.IndexOf(beat.Instrument), Velocity = beat.Velocity.Value };
        }
        public static PatternData ToData(Pattern pattern)
        {
            var instruments = pattern.Instruments;
            return new PatternData
            {
                BarsCount = pattern.Info.BarsCount,
                BeatsPerBar = pattern.Info.BeatsPerBar,
                TimeUnitsPerBeat = pattern.Info.UnitsPerBeat.Index,
                SuggestedBpm = pattern.Info.SuggestedBpm,
                Instruments = pattern.Instruments.Select(ToData).ToArray(),
                Beats = pattern.AllBeats().Select(b => ToData(b, instruments)).ToArray()
            };
        }
        public static Pattern ToPattern(PatternData data)
        {
            var idToInstrument = new Dictionary<int, IInstrumentId>();
            foreach (var instrumentData in data.Instruments)
            {
                if (idToInstrument.ContainsKey(instrumentData.Id))
                    throw new PatternParsingException("Duplicate instrument id: " + instrumentData.Id
                        + ". Used by \""
                        + idToInstrument[instrumentData.Id].Name
                        + "\" and \""
                        + instrumentData.Name
                        + "\"");
                idToInstrument.Add(instrumentData.Id, new SimpleInstrumentId(instrumentData.Name));
            }
            //var beats = new SortedDictionary<TimeInUnits, Velocity[]>();
            var builder = new Pattern.Builder(data.Instruments.Select(i => idToInstrument[i.Id]).ToArray());
            int index = 0;
            foreach (var beatData in data.Beats)
            {
                var t = new TimeInUnits(beatData.Time);
                IInstrumentId instrument;
                if (!idToInstrument.TryGetValue(beatData.Instrument, out instrument))
                    throw new PatternParsingException("Invalid instrument id for beat #" + index + "(t=" + beatData.Time + ", id=" + beatData.Instrument + ")");
                builder.Add(t, instrument, new Velocity(beatData.Velocity));
            }
            var patternInfo = new PatternInfo.Builder();
            patternInfo.BarsCount = data.BarsCount;
            patternInfo.BeatsPerBar = data.BeatsPerBar;
            patternInfo.SuggestedBpm = data.SuggestedBpm;
            patternInfo.UnitsPerBeat = new TimeInUnits(data.TimeUnitsPerBeat);
            builder.PatternInfo = patternInfo.Build();
            return builder.Build();
        }
    }
}
