using System;
using System.Linq;
using System.Collections.Generic;

namespace Drumz.Common
{
    public interface IInstrumentsDatabase
    {
        IInstrumentId Instrument(int id, string name);
    }
    public class SimpleInstrumentsDatabase : IInstrumentsDatabase
    {
        public IInstrumentId Instrument(int id, string name)
        {
            return new SimpleInstrumentId(name);
        }
    }
    public class SoundsList
    {
        private readonly ISoundId[] sounds;

        public SoundsList(IEnumerable<ISoundId> sounds)
        {
            this.sounds = sounds.ToArray();
        }
        public IEnumerable<ISoundId> Sounds { get { return sounds; } }
        public ISoundId this[int index]
        {
            get
            {
                return sounds[index];
            }
        }
        public int IndexOf(ISoundId sound)
        {
            return Array.IndexOf(sounds, sound);
        }
        public IEnumerable<IInstrumentId> Instruments
        {
            get
            {
                return sounds.Select(s => s.Instrument).Distinct();
            }
        }
        public int Count
        {
            get
            {
                return sounds.Length;
            }
        }
    }
    public interface IInstrumentId
    {
        string Name { get; }
        //string ShortName { get; }
    }
    public interface ISoundId
    {
        IInstrumentId Instrument { get; }
        string Technique { get; }
        string Mark { get; } // todo: improve this
    }
    public static class SoundIdExtensions
    {
        public static string Name(this ISoundId soundId)
        {
            return soundId.Instrument.Name + "." + soundId.Technique;
        }
    }
    public sealed class SimpleInstrumentId : IInstrumentId
    {
        public SimpleInstrumentId(string name)
        {
            Name = name;
        }
        public string Name { get; private set; }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            var other = obj as SimpleInstrumentId;
            return other != null && Equals(other);
        }
        public bool Equals(SimpleInstrumentId other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(other.Name, Name);
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
    public sealed class SimpleSoundId : ISoundId
    {
        public SimpleSoundId(string instrumentName, string techniqueName, string mark)
            : this(new SimpleInstrumentId(instrumentName), techniqueName, mark)
        {

        }
        public SimpleSoundId(IInstrumentId instrument, string technique, string mark)
        {
            Instrument = instrument;
            Technique = technique;
            Mark = mark;
        }

        public IInstrumentId Instrument { get; private set; }

        public string Technique { get; private set; }

        public string Mark { get; private set; }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            var other = obj as SimpleSoundId;
            return other != null && Equals(other);
        }
        public bool Equals(SimpleSoundId other)
        {
            return Equals(Instrument, other.Instrument) &&
                StringComparer.OrdinalIgnoreCase.Equals(other.Technique, Technique) &&
                StringComparer.OrdinalIgnoreCase.Equals(other.Mark, Mark);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return Instrument.GetHashCode() + Mark.GetHashCode();
            }
        }
    }
}
