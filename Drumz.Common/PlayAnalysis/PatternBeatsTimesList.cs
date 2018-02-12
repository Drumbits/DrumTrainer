using System;

namespace Drumz.Common.PlayAnalysis
{
    public class PatternBeatsTimesList
    {
        private readonly BeatTimesList beatsList;
        private readonly ContinuousBeatsLooper patternBeats;

        public PatternBeatsTimesList(BeatTimesList beatsList, ContinuousBeatsLooper patternBeats)
        {
            this.beatsList = beatsList;
            this.patternBeats = patternBeats;
        }

        public void Tick(float time, Action<TimedBeat> discardBeat)
        {
            beatsList.Tick(time, discardBeat);
            patternBeats.FillBeatsUntil(time + beatsList.KeepWindow, beatsList);
        }
        public BeatTimesList Beats { get { return beatsList; } }
        public void Reset()
        {
            this.beatsList.Clear();
            patternBeats.Reset();
        }
        public float Offset
        {
            get
            {
                return patternBeats.Offset;
            }
        }
    }
}
