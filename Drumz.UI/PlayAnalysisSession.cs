using System;
using System.Collections.Generic;
using System.Text;
using Drumz.Common.Utils;
using Drumz.Common;
using Drumz.Common.Beats;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI
{
    public interface ITimeCounter
    {
        float T { get; }
    }
    public class PlayedBeatIds
    {
        private readonly List<TimedEvent<Beat>> beats = new List<TimedEvent<Beat>>();

        public BeatId New(TimedEvent<Beat> beat)
        {
            beats.Add(beat);
            return new BeatId((short)beats.Count);
        }
    }
    public enum BeatStatus
    {
        Pending,    // status not yet known
        Early,      // beat played too early
        Late,       // beat played too late
        Correct,    // beat played accurately
        MissedPlay, // played beat with no corresponding pattern beat
        MissedPattern// missed pattern beat
    };

    public delegate void NewPlayedBeatEventHandler(TimedBeat timedBeat, int instrumentIndexInPattern);
    public delegate void PlayedBeatStatusSetEventHandler(BeatId beatId, BeatStatus status);
    public delegate void MissedPatternBeatEventHandler(TimedBeat patternBeat, int instrumentIndexInPattern);
    public delegate void TickEventHandler(float t);

    public class PlayAnalysisSession : IMatchResultsCollector, ITimeCounter
    {
        public class Settings
        {
            public int RefreshTimeInMs = 20;
        }
        public event NewPlayedBeatEventHandler NewPlayedBeat;
        public event PlayedBeatStatusSetEventHandler PlayedBeatStatusSet;
        public event MissedPatternBeatEventHandler PatternMissed;
        public event TickEventHandler Tick;

        private readonly Settings settings;
        private readonly IDictionary<Drumz.Common.IInstrumentId, int> patternInstruments;
        private readonly PatternBeatIds patternBeats;
        private readonly PlayedBeatIds playedBeats;
        private readonly PatternMatcher matcher;
        private readonly float timeUnitInMs;
        private readonly System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        private bool isRunning = false;
        private System.Collections.Concurrent.ConcurrentQueue<TimedEvent<Beat>> playedBeatsBuffer = new System.Collections.Concurrent.ConcurrentQueue<TimedEvent<Beat>>();

        public PlayAnalysisSession(Settings settings, Pattern pattern)
        {
            this.settings = settings;
            patternInstruments = pattern.Instruments.IndexOfDictionary();
            patternBeats = PatternBeatIds.Create(pattern);
            playedBeats = new PlayedBeatIds();
            this.matcher = PatternMatcher.Create(patternBeats, pattern.Info, new PatternMatcher.Settings(), this);
            timeUnitInMs = pattern.Info.SuggestedBpm / 60000f;
        }
        public bool IsRunning { get { return isRunning; } }
        public void RegisterPlayedBeat(Beat beat)
        {
            playedBeatsBuffer.Enqueue(new TimedEvent<Beat>(T, beat));
        }

        public void Start()
        {
            isRunning = true;
            new System.Threading.Thread(Run).Start();
        }
        private void Run()
        {
            isRunning = true;
            timer.Start();
            while (isRunning)
            {
                var nextTick = settings.RefreshTimeInMs + timer.ElapsedMilliseconds;
                FlushBeatsBuffer();
                matcher.Tick(T);
                Tick(T);
                System.Threading.Thread.Sleep((int)Math.Max(0, nextTick - timer.ElapsedMilliseconds));
            }
            timer.Stop();
        }
        public void Stop()
        {
            isRunning = false;
        }
        public float T
        {
            get
            {
                return timeUnitInMs * timer.ElapsedMilliseconds;
            }
        }
        public void Reset()
        {
            timer.Reset();
            playedBeatsBuffer = new System.Collections.Concurrent.ConcurrentQueue<TimedEvent<Beat>>();
        }
        private void FlushBeatsBuffer()
        {
            while (playedBeatsBuffer.TryDequeue(out TimedEvent<Beat> beat))
            {
                var id = playedBeats.New(beat);
                var timedBeat = new TimedBeat((float)beat.Time, id);
                var instrumentIndex = patternInstruments[beat.Value.Instrument];
                NewPlayedBeat?.Invoke(timedBeat, instrumentIndex);
                matcher.AddBeat(instrumentIndex, timedBeat, beat.Value.Velocity);
            }
        }
        private static BeatStatus MatchStatus(float accuracy)
        {
            if (accuracy < -0.25f) return BeatStatus.Early;
            if (accuracy > 0.25f) return BeatStatus.Late;
            return BeatStatus.Correct;
        }

        void IMatchResultsCollector.Match(BeatsMatch match)
        {
            //todo: collect matches
            var status = MatchStatus(match.Accuracy);
            PlayedBeatStatusSet?.Invoke(match.PlayedBeat.Id, status);
        }

        void IMatchResultsCollector.MissedBeat(MissedBeat match)
        {
            //todo: collect misses
            if (match.Beat.Id.IsPattern)
                PatternMissed?.Invoke(match.Beat, match.InstrumentIndex);

            PlayedBeatStatusSet?.Invoke(match.Beat.Id, BeatStatus.MissedPlay);
        }
    }
}
