using System;
using System.Collections.Generic;
using Drumz.Common.Beats;
using Drumz.Common.PlayAnalysis;
using Xunit;
namespace Drumz.Common.UnitTests.PlayAnalysis
{
    public class PatternMatcherTest
    {
        private Pattern BuildBinaryPattern()
        {
            var builder = new PatternInfo.Builder();
            builder.BarsCount = 1;
            builder.BeatsPerBar = 4;
            builder.UnitsPerBeat = new TimeInUnits(2);
            builder.SuggestedBpm = 60;
            // SN  --o---o-
            // BS  x---xx--
            var sn = new SimpleInstrumentId("SN");
            var bs = new SimpleInstrumentId("BS");
            var result = new Pattern(builder.Build(), new IInstrumentId[] {sn, bs });
            result.Add(new TimeInUnits(0), bs, Velocity.Medium);
            result.Add(new TimeInUnits(4), bs, Velocity.Medium);
            result.Add(new TimeInUnits(5), bs, Velocity.Medium);
            result.Add(new TimeInUnits(3), bs, Velocity.Medium);
            result.Add(new TimeInUnits(7), bs, Velocity.Medium);
            return result;
        }
        private class MatchResultCollector : IMatchResultsCollector
        {
            public readonly List<BeatsMatch> Matches = new List<BeatsMatch>();
            public readonly List<MissedBeat> Missed = new List<MissedBeat>();
            public void Match(BeatsMatch match)
            {
                Matches.Add(match);
            }

            public void MissedBeat(MissedBeat missed)
            {
                Missed.Add(missed);
            }
        }
        [Fact]
        public void TestReset()
        {
            var pattern = BuildBinaryPattern();
            var matchResults = new MatchResultCollector();
            var matcher = PatternMatcher.Create(pattern, new PatternMatcher.Settings { MaxMatchingTime = 0.25f }, matchResults);
            var sn = 0;
            var bs = 1;

            short i = 1;
            matcher.Tick(0f);
            var beat = new TimedBeat(0, new BeatId(i++));
            matcher.AddBeat(bs, beat, Velocity.Medium);
            Assert.Single(matchResults.Matches);

            matcher.Tick(100f);

            matcher.Reset();
            matchResults.Matches.Clear();
            matchResults.Missed.Clear();
            matcher.Tick(0f);
            matcher.AddBeat(bs, beat, Velocity.Medium);
            Assert.Single(matchResults.Matches);
        }
    }
}
