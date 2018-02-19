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
        private static SoundData ToData(ISoundId sound, SoundsList sounds)
        {
            return new SoundData { Technique = sound.Technique, Instrument = sound.Instrument.Name, Id = sounds.IndexOf(sound), Mark = sound.Mark };
        }
        private static BeatData ToData(PatternBeat beat, SoundsList patternSounds)
        {
            return new BeatData { Time = beat.T.Index, Sound = patternSounds.IndexOf(beat.Sound), Velocity = beat.Velocity.Value };
        }
        public static PatternData ToData(Pattern pattern)
        {
            return new PatternData
            {
                BarsCount = pattern.Info.BarsCount,
                BeatsPerBar = pattern.Info.BeatsPerBar,
                TimeUnitsPerBeat = pattern.Info.UnitsPerBeat.Index,
                SuggestedBpm = pattern.Info.SuggestedBpm,
                Sounds = pattern.Sounds.Sounds.Select(s => ToData(s, pattern.Sounds)).ToArray(),
                Beats = pattern.AllBeats().Select(b => ToData(b, pattern.Sounds)).ToArray()
            };
        }
        public static Pattern ToPattern(PatternData data)
        {
            var idToSound = new Dictionary<int, ISoundId>();
            foreach (var soundData in data.Sounds)
            {
                if (idToSound.ContainsKey(soundData.Id))
                    throw new PatternParsingException("Duplicate sound id: " + soundData.Id
                        + ". Used by \""
                        + idToSound[soundData.Id].Name()
                        + "\" and \""
                        + soundData.Instrument + "." + soundData.Technique
                        + "\"");
                idToSound.Add(soundData.Id, new SimpleSoundId(soundData.Instrument, soundData.Technique, soundData.Mark));
            }
            var builder = new Pattern.Builder(data.Sounds.Select(i => idToSound[i.Id]).ToArray());
            int index = 0;
            foreach (var beatData in data.Beats)
            {
                var t = new TimeInUnits(beatData.Time);
                ISoundId sound;
                if (!idToSound.TryGetValue(beatData.Sound, out sound))
                    throw new PatternParsingException("Invalid instrument id for beat #" + index + "(t=" + beatData.Time + ", id=" + beatData.Sound + ")");
                builder.Add(t, sound, new Velocity(beatData.Velocity));
            }
            var patternInfo = new PatternInfo.Builder
            {
                BarsCount = data.BarsCount,
                BeatsPerBar = data.BeatsPerBar,
                SuggestedBpm = data.SuggestedBpm,
                UnitsPerBeat = new TimeInUnits(data.TimeUnitsPerBeat)
            };
            builder.PatternInfo = patternInfo.Build();
            return builder.Build();
        }
    }
}