using System;

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
    public interface IInstrumentId
    {
        string Name { get; }
        //string ShortName { get; }
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
}
