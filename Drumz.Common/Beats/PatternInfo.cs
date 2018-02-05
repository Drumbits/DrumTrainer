
namespace Drumz.Common.Beats
{
    public class PatternInfo
    {
        public int BarsCount { get; private set; }
        public int BeatsPerBar { get; private set; }
        public int SuggestedBpm { get; private set; }
        public TimeInUnits UnitsPerBeat { get; private set; }
        public TimeInUnits LastTime
        {
            get
            {
                return new TimeInUnits(UnitsPerBeat.Index * BarsCount * BeatsPerBar);
            }
        }

        private PatternInfo() { }

        public class Builder
        {
            private int? barsCount;
            private int? beatsPerBar;
            private int? suggestedBpm;
            private TimeInUnits? unitsPerBeat;

            public int BarsCount { set { barsCount = value; } }
            public int BeatsPerBar { set { beatsPerBar = value; } }
            public int SuggestedBpm { set { suggestedBpm = value; } }
            public TimeInUnits UnitsPerBeat { set { unitsPerBeat = value; } }

            public PatternInfo Build()
            {
                var result = new PatternInfo();
                result.BarsCount = GetValueOrFail(barsCount, nameof(barsCount));
                result.BeatsPerBar = GetValueOrFail(beatsPerBar, nameof(beatsPerBar));
                result.SuggestedBpm = GetValueOrFail(suggestedBpm, nameof(suggestedBpm));
                result.UnitsPerBeat = GetValueOrFail(unitsPerBeat, nameof(unitsPerBeat));
                return result;
            }
            private T GetValueOrFail<T>(System.Nullable<T> value, string name) where T: struct
            {
                if (!value.HasValue)
                    throw new System.InvalidOperationException(nameof(PatternInfo) + "." + name + " not set");
                return value.Value;
            }
        }
    }
}
