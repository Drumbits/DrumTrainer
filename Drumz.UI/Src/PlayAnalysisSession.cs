using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            return new BeatId(beats.Count);
        }
        public TimedEvent<Beat> Beat(BeatId id) { return beats[id.Index - 1]; }
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

    public delegate void NewPlayedBeatEventHandler(TimedBeatId timedBeat, ISoundId sound);
    public delegate void PlayedBeatStatusSetEventHandler(BeatId beatId, BeatStatus status);
    public delegate void MissedPatternBeatEventHandler(TimedBeatId patternBeat);
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
        private readonly Pattern pattern;
        private readonly PlayedBeatIds playedBeats;
        private readonly PatternMatcher matcher;
        private readonly float timeUnitInMs;
        private readonly System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        private PerformanceSummary summary = null;
        private System.Collections.Concurrent.ConcurrentQueue<TimedEvent<Beat>> playedBeatsBuffer = new System.Collections.Concurrent.ConcurrentQueue<TimedEvent<Beat>>();
        //threading stuff
        private Task task;
        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        public PlayAnalysisSession(Settings settings, Pattern pattern)
        {
            this.settings = settings;
            this.pattern = pattern;
            playedBeats = new PlayedBeatIds();
            this.matcher = PatternMatcher.Create(pattern, new PatternMatcher.Settings(), this);
            timeUnitInMs = pattern.Info.SuggestedBpm / 60000f;
        }
        
        public bool IsRunning { get { return task != null && !task.IsCompleted; } }

        public void RegisterPlayedBeat(Beat beat)
        {
            playedBeatsBuffer.Enqueue(new TimedEvent<Beat>(T, beat));
        }

        public void Start()
        {
            var token = cancelSource.Token;
            task = Task.Factory.StartNew(() => Run(token), token);
        }
        private void Run(CancellationToken cancel)
        {
            timer.Start();
            while (!cancel.IsCancellationRequested)
            {
                var nextTick = settings.RefreshTimeInMs + timer.ElapsedMilliseconds;
                FlushBeatsBuffer();
                matcher.Tick(T);
                Tick(T);
                Thread.Sleep((int)Math.Max(0, nextTick - timer.ElapsedMilliseconds));
            }
            timer.Stop();
        }
        public void Stop()
        {
            cancelSource.Cancel();
        }
        public float T
        {
            get
            {
                return timeUnitInMs * timer.ElapsedMilliseconds;
            }
        }
        public PerformanceSummary Summary
        {
            get { return summary; }
        }
        public void Reset()
        {
            this.summary = new PerformanceSummary(pattern);
            timer.Reset();
            matcher.Reset();
            playedBeatsBuffer = new System.Collections.Concurrent.ConcurrentQueue<TimedEvent<Beat>>();
        }
        private void FlushBeatsBuffer()
        {
            while (playedBeatsBuffer.TryDequeue(out TimedEvent<Beat> beat))
            {
                var id = playedBeats.New(beat);
                var timedBeat = new TimedBeatId(beat.Time, id);
                NewPlayedBeat?.Invoke(timedBeat, beat.Value.Sound);
                matcher.AddBeat(beat.Value.Sound, timedBeat, beat.Value.Velocity);
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
            summary.BeatSummary(match.PatternBeat.Id).Add(match.Accuracy);
            var status = MatchStatus(match.Accuracy);
            PlayedBeatStatusSet?.Invoke(match.PlayedBeat.Id, status);
        }

        void IMatchResultsCollector.MissedBeat(TimedBeatId beat)
        {
            if (beat.Id.IsPattern)
            {
                summary.BeatSummary(beat.Id).AddMiss();
                PatternMissed?.Invoke(beat);
            }
            PlayedBeatStatusSet?.Invoke(beat.Id, BeatStatus.MissedPlay);
        }
    }
}
