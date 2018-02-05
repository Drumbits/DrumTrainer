using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Drumz.Common.Beats.IO
{
    [DataContract]
    public class PatternData
    {
        [DataMember]
        public int BarsCount;
        [DataMember]
        public int BeatsPerBar;
        [DataMember]
        public int TimeUnitsPerBeat;
        [DataMember]
        public int SuggestedBpm = 0;
        [DataMember]
        public InstrumentData[] Instruments;
        [DataMember]
        public BeatData[] Beats;

    }
    [DataContract]
    public class InstrumentData
    {
        [DataMember]
        public string Name;
        [DataMember]
        public int Id;
    }

    [DataContract]
    public class BeatData
    {
        [DataMember]
        public int Time;
        [DataMember]
        public int Instrument;
        [DataMember]
        public double Velocity;
    }
}
