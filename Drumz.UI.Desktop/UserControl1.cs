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
            bpm = pattern.Info.SuggestedBpm;
            var settings = new GridDrawer.Settings { BeatWidth = 64, FontSize = 20, LineHeight = 24 };
            patternDrawer = new PatternDrawer(pattern, settings, 2);
            playAnalysis = new PlayAnalysisSession(new PlayAnalysisSession.Settings(), pattern);
            for (var index = 0; index < pattern.Instruments.Count; ++index)
                instrumentKeys.Add(pattern.Instruments[index].Name.Substring(0, 1), pattern.Instruments[index]);
            playAnalysis.NewPlayedBeat += patternDrawer.AddPlayedBeat;
            playAnalysis.PlayedBeatStatusSet += patternDrawer.SetPlayedBeatStatus;
            playAnalysis.Tick += PlayAnalysis_Tick;        
            Drumz.Common.Diagnostics.Logger.Instance.Log += Instance_Log;
        }

        private void PlayAnalysis_Tick(float t)
        {
            if (!chrono.IsRunning)
                chrono.Start();

            //Log?.Invoke("t: " + chrono.ElapsedMilliseconds * bpm / 60000f);
            patternDrawer.Tick(t);
            Invalidate();
        }

        private void Instance_Log(Common.Diagnostics.Logger.Level level, string message)
        {
            Log?.Invoke(message);
        }

        public event MessagerHandler Log;

        private readonly System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();
        private readonly int bpm;
        private readonly PatternDrawer patternDrawer;
        private readonly PlayAnalysisSession playAnalysis;
        private readonly Metronome metronome;
        private readonly IDictionary<string, IInstrumentId> instrumentKeys = new Dictionary<string, IInstrumentId>(StringComparer.OrdinalIgnoreCase);

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            //var localChrono = new System.Diagnostics.Stopwatch();
            //var start = chrono.ElapsedMilliseconds;
            //localChrono.Start();
            patternDrawer.Draw(e.Surface, playAnalysis.T);
            //localChrono.Stop();
            //Log?.Invoke(start + " " + chrono.ElapsedMilliseconds + " " + localChrono.ElapsedMilliseconds);
        }
        protected override void OnClick(EventArgs e)
        {
            if (playAnalysis.IsRunning)
            {
                Log("Stopping");
                playAnalysis.Stop();
                chrono.Stop();
                return;
            }
            playAnalysis.Reset();
            this.patternDrawer.Clear();
            Refresh();
            playAnalysis.Start();
            Log("Started");
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (playAnalysis.IsRunning)
            {
                if (instrumentKeys.TryGetValue(e.KeyChar.ToString(), out IInstrumentId instrument))
                {
                    var t = chrono.ElapsedMilliseconds * bpm / 60000f;
                    Log?.Invoke(e.KeyChar + ": " + t);
                    playAnalysis.RegisterPlayedBeat(new Beat(instrument, Velocity.Medium));
                }
            }
            base.OnKeyPress(e);
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            playAnalysis.Stop();
        }
    }
}
