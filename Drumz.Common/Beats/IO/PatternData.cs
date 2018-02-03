using System;
using System.Collections.Generic;
using System.Text;

namespace Drumz.Common.Beats.IO
{
    public class PatternData
    {
        public int BarsCount;
        public int BeatsPerBar;
        public int TimeUnitsPerBeat;
        public int SuggestedBpm = 0;
        public InstrumentData[] Instruments;
        public BeatData[] Beats;

    }
    public class InstrumentData
    {
        public string Name;
        public int Id;
    }

    public class BeatData
    {
        public int Time;
        public int Instrument;
        public double Velocity;
    }

}
