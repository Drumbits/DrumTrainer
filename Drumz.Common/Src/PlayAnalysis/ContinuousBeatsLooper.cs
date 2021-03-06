﻿using System.Collections.Generic;
using System.Linq;
using Drumz.Common.Beats;

namespace Drumz.Common.PlayAnalysis
{
    public class ContinuousBeatsLooper
    {
        public static IDictionary<IInstrumentId, ContinuousBeatsLooper> FromPattern(Pattern pattern)
        {
            var info = pattern.Info;
            var allBeats = new Dictionary<IInstrumentId, List<TimedBeatId>>();
            foreach (var beatId in pattern.Ids)
            {
                var beat = pattern.Beat(beatId);
                if (!allBeats.TryGetValue(beat.Sound.Instrument, out List<TimedBeatId> beatsForInstrument))
                {
                    beatsForInstrument = new List<TimedBeatId>();
                    allBeats.Add(beat.Sound.Instrument, beatsForInstrument);
                }
                beatsForInstrument.Add(new TimedBeatId(info.TimeInBeats(beat.T), beatId));
            }
            var totalLength = info.BarsCount * info.BeatsPerBar;
            return allBeats.ToDictionary(kv => kv.Key, kv => new ContinuousBeatsLooper(kv.Value.ToArray(), totalLength));
        }
        private readonly TimedBeatId[] onePassBeats;
        private readonly float repeatLength;
        private short nextBeatIndex;
        private int currentLoops;

        private ContinuousBeatsLooper(TimedBeatId[] onePassBeats, float repeatLength)
        {
            this.onePassBeats = onePassBeats;
            this.repeatLength = repeatLength;
            nextBeatIndex = 0;
            currentLoops = 0;
        }

        public void FillBeatsUntil(float t, BeatTimesList list)
        {
            if (t > repeatLength * (currentLoops + 1))
            {
                FillToEnd(list);
            }
            t -= Offset;
            for (; nextBeatIndex < onePassBeats.Length && onePassBeats[nextBeatIndex].T <= t; ++nextBeatIndex)
            {
                list.Add(onePassBeats[nextBeatIndex].Offset(Offset));
            }
        }
        public float Offset
        {
            get
            {
                return currentLoops * repeatLength;
            }
        }
        private void FillToEnd(BeatTimesList list)
        {
            for (; nextBeatIndex < onePassBeats.Length; ++nextBeatIndex)
            {
                list.Add(onePassBeats[nextBeatIndex].Offset(Offset));
            }
            nextBeatIndex = 0;
            ++currentLoops;
        }
        public void Reset()
        {
            nextBeatIndex = 0;
            currentLoops = 0;
        }
    }
}
