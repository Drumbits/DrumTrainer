using System.Collections.Generic;
using Xunit;
using Drumz.Common.Beats;
using Drumz.Common.PlayAnalysis;

namespace Drumz.Common.UnitTests.PlayAnalysis
{
    public class BeatTimesListTest
    {
        [Fact]
        public void Test()
        {
            var b1 = new TimedBeatId(0f, new BeatId(1));
            var b2 = new TimedBeatId(0.4f, new BeatId(2));
            var b3 = new TimedBeatId(0.6f, new BeatId(3));
            var beatTimeList = new BeatTimesList(0.5f);
            beatTimeList.Add(b1);
            beatTimeList.Add(b2);
            beatTimeList.Add(b3);
            Assert.Equal(3, beatTimeList.Count);
            var next = beatTimeList.Next;
            Assert.Equal(b1, beatTimeList.Next);
            var discarded = new List<TimedBeatId>();

            beatTimeList.Tick(0.6f, discarded.Add);
            Assert.Equal(new [] {b1}, discarded);
            Assert.Equal(2, beatTimeList.Count);

            beatTimeList.Tick(0.9f, discarded.Add);
            Assert.Equal(new[] { b1 }, discarded);
            Assert.Equal(2, beatTimeList.Count);

            beatTimeList.Tick(0.91f, discarded.Add);
            Assert.Equal(new[] { b1, b2 }, discarded);
            Assert.Equal(1, beatTimeList.Count);

            beatTimeList.Tick(1.2f, discarded.Add);
            Assert.Equal(new[] { b1, b2, b3 }, discarded);
            Assert.Equal(0, beatTimeList.Count);
            Assert.True(beatTimeList.IsEmpty);
        }
    }
}
