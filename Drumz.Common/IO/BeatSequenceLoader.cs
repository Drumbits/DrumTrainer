using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drumz.Common.Diagnostics;

namespace Drumz.Common.IO
{
    public class BeatSequenceLoader
    {
#if MOBILE
#else
        public static BeatSequence Load(string filePath)
        {
            return Load(new System.IO.FileInfo(filePath).OpenRead());
        }
#endif
        public static BeatSequence Load(System.IO.Stream stream)
        {
            return null;
        }
        public static BeatSequence LoadMidi(System.IO.Stream stream)
        {
            int beatsCount = 0;
            DateTime start = DateTime.Now;

            var result = new BeatSequence.BeatSequenceFactory();

            using (var reader = new System.IO.StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    double time = 0;
                    var beat = ParseBeat(line, out time);
                    if (beat != null)
                    {
                        result.AddBeat(time, beat);
                        ++beatsCount;
                    }
                }
            }
            Logger.Instance.Tell(Logger.Level.Info, "Loaded " + beatsCount + " beats in " + Math.Round((DateTime.Now - start).TotalSeconds, 2) + "s.");
            return result.Build();
        }
        const string RegExPattern = @"(?<time>\-?[0-9]+) (?<type>[A-Za-z]+) ch=[0-9]+ (c|n)=(?<ins>[0-9]+) v=(?<velocity>[0-9]+)";
        private static Beat ParseBeat(string line, out double beatTime)
        {
            beatTime = 0;
            var match = System.Text.RegularExpressions.Regex.Match(line, RegExPattern);
            if (!match.Success)
            {
                Logger.Instance.Tell(Logger.Level.Warning, "Invalid line format in beat sequence: " + line);
                return null;
            }
            if (match.Groups["type"].Value != "On") return null;
            var insName = match.Groups["ins"].Value;
            if (!InstrumentNameMap.ContainsKey(insName))
            {
                Logger.Instance.Tell(Logger.Level.Warning, "Unknown instrument in beat sequence: " + line);
                return null;
            }
            if (!double.TryParse(match.Groups["time"].Value, out beatTime))
            {
                Logger.Instance.Tell(Logger.Level.Warning, "Can't parse beatTime in beat sequence: " + line);
                return null;
            }
            Byte velocity = 0;
            if (!Byte.TryParse(match.Groups["velocity"].Value, out velocity))
            {
                Logger.Instance.Tell(Logger.Level.Warning, "Can't parse velocity at beat instrument in beat sequence: " + line);

            }
            insName = InstrumentNameMap[insName];
            return new Beat(InstrumentDataBase.Value.FromString(insName), new Velocity(velocity/256.0));
        }
        private static readonly Dictionary<string, string> InstrumentNameMap = new Dictionary<string, string>
        {
        {"22", "HH"},
        {"26", "HH"},
        {"36", "Kick"},
        {"38", "Snare"},
        {"42", "HH"},
        {"51", "Ride"}
        };
    }
}
