using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp.Views.Desktop;
using Drumz.Common;
using Drumz.Common.Beats;
using Drumz.Common.PlayAnalysis;

namespace Drumz.UI.Desktop
{
    public delegate void MessagerHandler(string message);
    public partial class UserControl1 : SkiaSharp.Views.Desktop.SKControl
    {
        public UserControl1()
        {
            InitializeComponent();
            var path = @"..\..\..\Samples\RHCP_GiveItAway1.drumz.pat.json";
            var pattern = Drumz.Common.Beats.IO.PatternIO.Load(path);
            //pattern.Info.SuggestedBpm = 6;
            metronome = new Metronome(pattern.Info.SuggestedBpm * pattern.Info.UnitsPerBeat.Index);
            metronome.Tick += Metronome_Tick;
            bpm = pattern.Info.SuggestedBpm;
            var settings = new GridDrawer.Settings { BeatWidth = 64, FontSize = 20, LineHeight = 24 };
            patternDrawer = new PatternDrawer(pattern, settings, 2);
            patternMatcher = PatternMatcher.Create(pattern, new PatternMatcher.Settings());
            patternMatcher.MatchFound += patternDrawer.MatchFoundEventHandler;
            for (var index = 0; index < pattern.Instruments.Count; ++index)
                instrumentKeys.Add(pattern.Instruments[index].Name.Substring(0, 1), index);
            Drumz.Common.Diagnostics.Logger.Instance.Log += Instance_Log;
        }

        private void Instance_Log(Common.Diagnostics.Logger.Level level, string message)
        {
            Log?.Invoke(message);
        }

        private void Metronome_Tick(int ellapsedTicks)
        {
            if (!chrono.IsRunning)
                chrono.Start();

            Log?.Invoke("t: " + chrono.ElapsedMilliseconds * bpm / 60000f);
            lock (patternMatcher)
            {
                patternMatcher.Tick(chrono.ElapsedMilliseconds * bpm / 60000f);
            }
            this.Invalidate();
        }

        public event MessagerHandler Log;

        private readonly System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();
        private readonly int bpm;
        private readonly PatternDrawer patternDrawer;
        private readonly PatternMatcher patternMatcher;
        private readonly Metronome metronome;
        private readonly IDictionary<string, int> instrumentKeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var localChrono = new System.Diagnostics.Stopwatch();
            var start = chrono.ElapsedMilliseconds;
            localChrono.Start();
            patternDrawer.Draw(e.Surface, new Common.Beats.TimeInUnits(metronome.EllapsedTicks));
            localChrono.Stop();
            //Log?.Invoke(start + " " + chrono.ElapsedMilliseconds + " " + localChrono.ElapsedMilliseconds);
        }
        protected override void OnClick(EventArgs e)
        {
            if (metronome.IsRunning)
            {
                Log("Stopping");
                metronome.Stop();
                chrono.Stop();
                return;
            }
            patternMatcher.Reset();
            this.patternDrawer.Clear();
            //Log?.Invoke("Interval: " + metronome.IntervalinMilliseconds.ToString("0.0") + "ms");
            metronome.Start();
            Log("Started");
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (metronome.IsRunning)
            {
                int instrument;
                if (instrumentKeys.TryGetValue(e.KeyChar.ToString(), out instrument))
                {
                    var t = chrono.ElapsedMilliseconds * bpm / 60000f;
                    Log?.Invoke(e.KeyChar + ": " + t);
                    lock (patternMatcher)
                    {
                        patternMatcher.AddBeat(instrument, t, Velocity.Medium);
                    }
                }
            }
            base.OnKeyPress(e);
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            metronome.Stop();
            metronome.Tick -= Metronome_Tick;
            metronome.Dispose();
        }
    }
}
