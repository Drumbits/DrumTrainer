
using System;
using System.Collections.Generic;
using System.Text;

namespace Drumz.Common.Instruments
{
    public sealed class InstrumentsDataBase
    {
        public static readonly InstrumentsDataBase Value = new InstrumentsDataBase();
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
}
