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
        public SoundData[] Sounds;
        [DataMember]
        public BeatData[] Beats;

    }
    [DataContract]
    public class SoundData
    {
        [DataMember]
        public int Id;
        [DataMember]
        public string Instrument;
        [DataMember]
        public string Technique;
        [DataMember]
        public string Mark;

        public override string ToString()
        {
            return string.Format("[{0}] {1}.{2} ({3})", Id, Instrument, Technique, Mark);
        }
    }

    [DataContract]
    public class BeatData
    {
        [DataMember]
        public int Time;
        [DataMember]
        public int Sound;
        [DataMember]
        public double Velocity;

        public override string ToString()
        {
            return string.Format("{0}: {1}/{2}", Time, Sound, Velocity);
        }
    }
}
