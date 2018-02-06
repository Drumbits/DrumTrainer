using System;
using System.Collections.Generic;
using System.Linq;
using Drumz.Common.Beats;
using Drumz.Common.Utils;

namespace Drumz.Common.PlayAnalysis
{
    public class BeatsMatch
    {
        public readonly int InstrumentIndex;
        public readonly float PatternTime;
        public readonly float PlayedTime;
        public readonly float LoopOffset;
        public readonly float Accuracy;// from -1 (max too early) to +1 (max too late)

        public BeatsMatch(int instrumentIndex, float patternTime, float playedTime, float accuracy, float loopOffset)
        {
            this.InstrumentIndex = instrumentIndex;
            this.PatternTime = patternTime;
            this.PlayedTime = playedTime;
            this.Accuracy = accuracy;
            this.LoopOffset = loopOffset;
        }
    }
    public delegate void BeatsMatchEventHandler(BeatsMatch match);
    public class SingleInstrumentBeatsMatcher
    {
        private readonly int instrumentIndex;
        private readonly PatternBeatsTimesList patternBeats;
        private readonly BeatTimesList playedBeats;
        private readonly PatternMatcher.Settings settings;

        public event BeatsMatchEventHandler BeatsMatched;

        public SingleInstrumentBeatsMatcher(int instrumentIndex, PatternBeatsTimesList patternBeats, BeatTimesList playedBeats, PatternMatcher.Settings settings)
        {
            this.instrumentIndex = instrumentIndex;
            this.patternBeats = patternBeats;
            this.playedBeats = playedBeats;
            this.settings = settings;
        }

        public void AddPlayed(float t, Velocity v)
        {
            lock (playedBeats)
            {
                if (!patternBeats.Beats.IsEmpty)
                {
                    var diff = t - patternBeats.Beats.Next;
                    if (Math.Abs(diff) <= settings.MaxMatchingTime)
                    {
                        OnMatchFound(diff, t);
                        return;
                    }
                }
                playedBeats.Add(t);
            }
        }
        public void Tick(float t)
        {
            lock (playedBeats)
            {
                patternBeats.Tick(t);
                playedBeats.Tick(t);
                /*
                if (instrumentIndex == 2)
                {
                    Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Debug, "Pattern: {0}", patternBeats.Beats.Content.ToNiceString());
                    Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Debug, "Played: {0}", playedBeats.Content.ToNiceString());
                }*/
                LookForMatches();
            }
        }
        public void Reset()
        {
            playedBeats.Clear();
            patternBeats.Reset();
        }
        private void LookForMatches()
        {
            if (patternBeats.Beats.IsEmpty || playedBeats.IsEmpty) return;
            var diff = playedBeats.Next - patternBeats.Beats.Next;
            if (Math.Abs(diff) > settings.MaxMatchingTime) return;
            OnMatchFound(diff);
            LookForMatches();
        }
        private void OnMatchFound(float diff)
        {
            OnMatchFound(diff, playedBeats.Dequeue());
        }
        private void OnMatchFound(float diff, float playedTime)
        {
            var match = new BeatsMatch(instrumentIndex,
                patternBeats.Beats.Dequeue(),
                playedTime,
                diff / (1.01f * settings.MaxMatchingTime),
                patternBeats.Offset);
            BeatsMatched?.Invoke(match);
        }
    }
    public class ContinuousBeatsLooper
    {
        public static ContinuousBeatsLooper FromPattern(Pattern pattern, int instrumentIndex)
        {
            var beats = pattern.Beats(pattern.Instruments[instrumentIndex]).Select(b => new TimedEvent<Velocity>(b.Key.Index / (double)pattern.Info.UnitsPerBeat.Index, b.Value));
            return new ContinuousBeatsLooper(beats.ToArray(), pattern.Info.BeatsPerBar * pattern.Info.BarsCount);
        }
        private readonly TimedEvent<Velocity>[] onePassBeats;
        private readonly float repeatLength;
        private int nextIndex;
        private int currentLoops;

        public ContinuousBeatsLooper(TimedEvent<Velocity>[] onePassBeats, float repeatLength)
        {
            this.onePassBeats = onePassBeats;
            this.repeatLength = repeatLength;
            nextIndex = 0;
            currentLoops = 0;
        }

        public void FillBeatsUntil(float t, BeatTimesList list)
        {
            if (t > repeatLength * (currentLoops + 1))
            {
                FillToEnd(list);
            }
            t -= (currentLoops * repeatLength);
            for (; nextIndex < onePassBeats.Length && onePassBeats[nextIndex].Time <= t; ++nextIndex)
            {
                list.Add(Offset + (float)onePassBeats[nextIndex].Time);
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
            for (; nextIndex < onePassBeats.Length; ++nextIndex)
            {
                list.Add(Offset + (float)onePassBeats[nextIndex].Time);
            }
            nextIndex = 0;
            ++currentLoops;
        }
        public void Reset()
        {
            nextIndex = 0;
            currentLoops = 0;
        }
    }
    public class PatternBeatsTimesList
    {
        private readonly BeatTimesList beatsList;
        private readonly ContinuousBeatsLooper patternBeats;

        public PatternBeatsTimesList(BeatTimesList beatsList, ContinuousBeatsLooper patternBeats)
        {
            this.beatsList = beatsList;
            this.patternBeats = patternBeats;
        }

        public void Tick(float time)
        {
            beatsList.Tick(time);
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

    public class BeatTimesList
    {
        private readonly float keepWindow;
        private readonly System.Collections.Generic.Queue<float> times = new System.Collections.Generic.Queue<float>();

        public BeatTimesList(float keepWindow)
        {
            this.keepWindow = keepWindow;
        }

        public void Tick(float time)
        {
            var timeLimit = time - keepWindow;
            while (times.Count > 0 && times.Peek() < timeLimit)
                times.Dequeue();
        }
        public void Add(float time)
        {
            times.Enqueue(time);
        }
        public bool IsEmpty { get { return times.Count == 0; } }
        public float Next { get { return times.Peek(); } }
        public float Dequeue()
        {
            return times.Dequeue();
        }
        public float KeepWindow
        {
            get
            {
                return keepWindow;
            }
        }
        public IEnumerable<float> Content
        {
            get
            {
                return times;
            }
        }
        public void Clear()
        {
            times.Clear();
        }
    }

    public class PatternMatcher
    {
        public static PatternMatcher Create(Pattern pattern, Settings settings)
        {
            var instrumentMatchers = pattern.Instruments.Select(
                (ins, index) => new SingleInstrumentBeatsMatcher(index,
                new PatternBeatsTimesList(new BeatTimesList(settings.MaxMatchingTime), ContinuousBeatsLooper.FromPattern(pattern, index)),
                new BeatTimesList(settings.MaxMatchingTime),
                settings));
            return new PatternMatcher(settings, instrumentMatchers.ToArray());
        }
        public class Settings
        {
            public float MaxMatchingTime = 0.5f;
        }
        private readonly Settings settings;
        private readonly SingleInstrumentBeatsMatcher[] perInstrumentMatchers;

        public PatternMatcher(Settings settings, SingleInstrumentBeatsMatcher[] perInstrumentMatchers)
        {
            this.settings = settings;
            this.perInstrumentMatchers = perInstrumentMatchers;
            foreach (var matcher in perInstrumentMatchers)
                matcher.BeatsMatched += OnMatchFound;
        }

        public event BeatsMatchEventHandler MatchFound;
        private void OnMatchFound(BeatsMatch match)
        {
            Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Info, "MatchFound: {0} at {1:0.00}", match.InstrumentIndex, match.PlayedTime);
            MatchFound?.Invoke(match);
        }
        public void Reset()
        {
            foreach (var matcher in perInstrumentMatchers)
                matcher.Reset();
        }
        public void Tick(float newTime)
        {
            foreach (var matcher in perInstrumentMatchers)
                matcher.Tick(newTime);
        }
        public void AddBeat(int instrumentIndex, float time, Velocity v)
        {
            perInstrumentMatchers[instrumentIndex].AddPlayed(time, v);
        }
    }
}
