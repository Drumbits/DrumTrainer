using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Drumz.Common.Beats;
using Drumz.Common.Utils;

namespace Drumz.Common.PlayAnalysis
{
    public interface IMatchResultsCollector
    {
        void Match(BeatsMatch match);
        void MissedBeat(MissedBeat match);
    }
    public struct BeatId
    {
        public short Index;
        public BeatId(short index) { this.Index = index; }
        public bool IsPattern { get { return Index < 0; } }
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType()) return false;
            var other = (BeatId)obj;
            return other.Index == Index;
        }
        public override int GetHashCode()
        {
            return Index;
        }
    }
    /// <summary>
    /// (tricky) convention: pattern beat ids are negative, starting from -1
    /// This means there should be not overlap with played beats (which are positive starting from 1)
    /// </summary>
    public class PatternBeatIds : IEnumerable<BeatId>
    {
        public static PatternBeatIds Create(Pattern pattern)
        {
            return new PatternBeatIds(new PatternBeat[] { null }.Concat(pattern.AllBeats()).ToArray());
        }
        // first element is null, to be consistent with pattern beat ids indexing strarting from 1.
        private readonly PatternBeat[] beats;

        private PatternBeatIds(PatternBeat[] beats)
        {
            this.beats = beats;
        }
        public PatternBeat Beat(BeatId id)
        {
            var index = -id.Index;
            if (index < 1 || index >= beats.Length)
                throw new ArgumentException("Invalid pattern beat id: " + id.Index);
            return beats[index];
        }
        public IEnumerator<BeatId> GetEnumerator()
        {
            return Enumerable.Range(1, beats.Length-1).Select(i => new BeatId((short)-i)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class BeatsRegister
    {
        private readonly float[] times;
        private short nextIndex;

        public BeatsRegister(int maxConcurrenBeats)
        {
            this.times = Enumerable.Repeat(-1f, maxConcurrenBeats).ToArray();
            nextIndex = 0;
        }

        public short Register(float time)
        {
            if (times[nextIndex] != -1f)
                throw new ArgumentException("Access to non released beat: [" + nextIndex + "]");
            var result = nextIndex;
            times[nextIndex++] = time;
            if (nextIndex == times.Length)
                nextIndex = 0;
            return result;
        }

        public float Time(short beatId)
        {
            return times[beatId];
        }
        public void Release(short beatIndex)
        {
            if (times[beatIndex] == -1f)
                throw new ArgumentException("Trying to release already released beat: [" + beatIndex + "]");
            times[beatIndex] = -1f;

        }
    }
    public class BeatsMatch
    {
        public readonly int InstrumentIndex;
        public readonly TimedBeat PatternBeat;
        public readonly TimedBeat PlayedBeat;
        public readonly float Accuracy;// from -1 (max too early) to +1 (max too late)

        public BeatsMatch(int instrumentIndex, TimedBeat patternBeat, TimedBeat playedBeat, float accuracy)
        {
            InstrumentIndex = instrumentIndex;
            PatternBeat = patternBeat;
            PlayedBeat = playedBeat;
            Accuracy = accuracy;
        }
    }
    public class MissedBeat
    {
        public readonly int InstrumentIndex;
        public readonly TimedBeat Beat;

        public MissedBeat(int instrumentIndex, TimedBeat beat)
        {
            InstrumentIndex = instrumentIndex;
            Beat = beat;
        }
    }
    public class SingleInstrumentBeatsMatcher
    {
        private readonly int instrumentIndex;
        private readonly PatternBeatsTimesList patternBeats;
        private readonly BeatTimesList playedBeats;
        private readonly PatternMatcher.Settings settings;

        public SingleInstrumentBeatsMatcher(int instrumentIndex, PatternBeatsTimesList patternBeats, BeatTimesList playedBeats, PatternMatcher.Settings settings)
        {
            this.instrumentIndex = instrumentIndex;
            this.patternBeats = patternBeats;
            this.playedBeats = playedBeats;
            this.settings = settings;
        }

        public void AddPlayed(TimedBeat beat, IMatchResultsCollector results)
        {
            if (!patternBeats.Beats.IsEmpty)
            {
                var diff = beat.T - patternBeats.Beats.Next.T;
                if (Math.Abs(diff) <= settings.MaxMatchingTime)
                {
                    var match = new BeatsMatch(instrumentIndex, patternBeats.Beats.RemoveNext(), beat, Accuracy(diff));
                    results.Match(match);
                    return;
                }
            }
            playedBeats.Add(beat);
        }
        public void Tick(float t, IMatchResultsCollector results)
        {
            lock (playedBeats)
            {
                patternBeats.Tick(t, b => results.MissedBeat(new MissedBeat(instrumentIndex, b)));
                playedBeats.Tick(t, b => results.MissedBeat(new MissedBeat(instrumentIndex, b)));
                /*
                if (instrumentIndex == 2)
                {
                    Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Debug, "Pattern: {0}", patternBeats.Beats.Content.ToNiceString());
                    Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Debug, "Played: {0}", playedBeats.Content.ToNiceString());
                }*/
                LookForMatches(results.Match);
            }
        }
        public void Reset()
        {
            playedBeats.Clear();
            patternBeats.Reset();
        }
        private void LookForMatches(Action<BeatsMatch> matchFound)
        {
            if (patternBeats.Beats.IsEmpty || playedBeats.IsEmpty) return;
            var diff = playedBeats.Next.T - patternBeats.Beats.Next.T;
            if (Math.Abs(diff) > settings.MaxMatchingTime) return;
            var match = new BeatsMatch(instrumentIndex, patternBeats.Beats.RemoveNext(), playedBeats.RemoveNext(), Accuracy(diff));
            matchFound(match);
            LookForMatches(matchFound);
        }
        private float Accuracy(float diff)
        {
            return diff / (1.01f * settings.MaxMatchingTime);
        }
    }
    public class ContinuousBeatsLooper
    {
        public static IDictionary<int, ContinuousBeatsLooper> FromPattern(PatternBeatIds pattern, PatternInfo info)
        {
            var allBeats = new Dictionary<int, List<TimedBeat>>();
            foreach (var beatId in pattern)
            {
                var beat = pattern.Beat(beatId);
                if (!allBeats.TryGetValue(beat.Instrument, out List<TimedBeat> beatsForInstrument))
                {
                    beatsForInstrument = new List<TimedBeat>();
                    allBeats.Add(beat.Instrument, beatsForInstrument);
                }
                beatsForInstrument.Add(new TimedBeat(info.TimeInBeats(beat.T), beatId));
            }
            var totalLength = info.BarsCount * info.BeatsPerBar;
            return allBeats.ToDictionary(kv => kv.Key, kv => new ContinuousBeatsLooper(kv.Value.ToArray(), totalLength));
        }
        private readonly TimedBeat[] onePassBeats;
        private readonly float repeatLength;
        private short nextBeatIndex;
        private int currentLoops;

        private ContinuousBeatsLooper(TimedBeat[] onePassBeats, float repeatLength)
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
    public struct TimedBeat
    {
        public float T;
        public BeatId Id;
        public TimedBeat(float t, BeatId id)
        {
            this.T = t;
            this.Id = id;
        }
        public TimedBeat Offset(float offset)
        {
            return new TimedBeat(T + offset, Id);
        }
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType()) return false;
            var other = (TimedBeat)obj;
            return other.T == T && other.Id.Index == Id.Index;
        }
        public override int GetHashCode()
        {
            return Id.Index;
        }
    }
    public class BeatTimesList
    {
        private readonly float keepWindow;
        private readonly System.Collections.Generic.Queue<TimedBeat> times = new System.Collections.Generic.Queue<TimedBeat>();

        public BeatTimesList(float keepWindow)
        {
            this.keepWindow = keepWindow;
        }

        public void Tick(float time, Action<TimedBeat> discardBeat)
        {
            var timeLimit = time - keepWindow;
            while (times.Count > 0 && times.Peek().T < timeLimit)
                discardBeat(times.Dequeue());
        }
        public void Add(TimedBeat beat)
        {
            times.Enqueue(beat);
        }
        public bool IsEmpty { get { return times.Count == 0; } }
        public TimedBeat Next { get { return times.Peek(); } }
        public TimedBeat RemoveNext()
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
                return times.Select(idAndT => idAndT.T);
            }
        }
        public int Count
        {
            get
            {
                return times.Count;
            }
        }
        public void Clear()
        {
            times.Clear();
        }
    }
    
    public class PatternMatcher
    {
        public static PatternMatcher Create(PatternBeatIds pattern, PatternInfo patternInfo, Settings settings, IMatchResultsCollector resultsCollector)
        {
            var patternBeatLists = ContinuousBeatsLooper.FromPattern(pattern, patternInfo).ToDictionary(
                kv => kv.Key,
                kv => new SingleInstrumentBeatsMatcher(kv.Key,
                    new PatternBeatsTimesList(
                        new BeatTimesList(settings.MaxMatchingTime),
                        kv.Value),
                    new BeatTimesList(settings.MaxMatchingTime),
                    settings));
            return new PatternMatcher(settings, patternBeatLists, resultsCollector);
        }
        public class Settings
        {
            public float MaxMatchingTime = 0.5f;
        }
        private readonly Settings settings;
        private readonly Dictionary<int, SingleInstrumentBeatsMatcher> perInstrumentMatchers;
        private readonly IMatchResultsCollector resultsCollector;

        private PatternMatcher(Settings settings, Dictionary<int, SingleInstrumentBeatsMatcher> perInstrumentMatchers, IMatchResultsCollector resultsCollector)
        {
            this.settings = settings;
            this.perInstrumentMatchers = perInstrumentMatchers;
            this.resultsCollector = resultsCollector;
        }
        /*
        private void OnMatchFound(BeatsMatch match)
        {
            Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Info, "MatchFound: {0} at {1:0.00}", match.InstrumentIndex, match.PlayedBeat.T);
            MatchFound?.Invoke(match);
        }
        private void OnMissedBeat(MissedBeat beat)
        {
            Drumz.Common.Diagnostics.Logger.TellF(Diagnostics.Logger.Level.Info, "Missed beat: {0}({2}) at {1:0.00}", beat.InstrumentIndex, beat.Beat.T, beat.Beat.Id.IsPattern ? "pattern" : "played");
            MissedBeat?.Invoke(beat);
        }*/
        public void Reset()
        {
            foreach (var matcher in perInstrumentMatchers.Values)
                matcher.Reset();
        }
        public void Tick(float newTime)
        {
            foreach (var matcher in perInstrumentMatchers.Values)
                matcher.Tick(newTime, resultsCollector);
        }
        public void AddBeat(int instrumentIndex, TimedBeat beat, Velocity v)
        {
            perInstrumentMatchers[instrumentIndex].AddPlayed(beat, resultsCollector);
        }
    }
}
