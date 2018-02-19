using System;
using System.Collections.Generic;
using System.Text;

namespace Drumz.Common.Instruments
{
    public static class DrumInstruments
    {
        public static readonly IInstrumentId Crash = InstrumentsDataBase.Value.FromString("Crash");
        public static readonly IInstrumentId Ride = InstrumentsDataBase.Value.FromString("Ride");
        public static readonly IInstrumentId RideBell = InstrumentsDataBase.Value.FromString("Ride (Bell)");
        public static readonly IInstrumentId HiHat = InstrumentsDataBase.Value.FromString("HiHat (Closed)");
        public static readonly IInstrumentId HiHatOpen = InstrumentsDataBase.Value.FromString("HiHat (Open)");
        public static readonly IInstrumentId HiHatFoot = InstrumentsDataBase.Value.FromString("HiHat (Foot)");
        public static readonly IInstrumentId TomHigh = InstrumentsDataBase.Value.FromString("High Tom");
        public static readonly IInstrumentId Snare = InstrumentsDataBase.Value.FromString("Snare");
        public static readonly IInstrumentId TomMid = InstrumentsDataBase.Value.FromString("Mid Tom");
        public static readonly IInstrumentId TomLow = InstrumentsDataBase.Value.FromString("Low Tom");
        public static readonly IInstrumentId Kick = InstrumentsDataBase.Value.FromString("Kick");
    }
}
