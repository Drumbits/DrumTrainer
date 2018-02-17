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
            bpm = pattern.Info.SuggestedBpm;
            var settings = new GridDrawer.Settings { BeatWidth = 64, FontSize = 20, LineHeight = 24 };
            patternDrawer = new PatternDrawer(pattern, settings, 2);
            playAnalysis = new PlayAnalysisSession(new PlayAnalysisSession.Settings(), pattern);
            foreach (var sound in pattern.Sounds.Sounds)
            {
                char key = sound.Instrument.Name[0];
                while (instrumentKeys.ContainsKey(key))
                    ++key;
                instrumentKeys.Add(key, sound);
            }
            playAnalysis.NewPlayedBeat += patternDrawer.AddPlayedBeat;
            playAnalysis.PlayedBeatStatusSet += patternDrawer.SetPlayedBeatStatus;
            playAnalysis.Tick += PlayAnalysis_Tick;        
            Drumz.Common.Diagnostics.Logger.Instance.Log += Instance_Log;
        }
        //private bool refreshRunning = false;
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
        private readonly Drumz.Common.Utils.FpsCounter fpsCounter = new Common.Utils.FpsCounter(50);
        private readonly int bpm;
        private readonly PatternDrawer patternDrawer;
        private readonly PlayAnalysisSession playAnalysis;
        private readonly IDictionary<char, ISoundId> instrumentKeys = new Dictionary<char, ISoundId>();

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            //var localChrono = new System.Diagnostics.Stopwatch();
            //var start = chrono.ElapsedMilliseconds;
            //localChrono.Start();
            patternDrawer.Draw(e.Surface, e.Info.Rect, playAnalysis.T);
            var fps = fpsCounter.AddFrame();
            if (fps >= 0f)
            {
                fps = (float)Math.Round(fps,0);
                e.Surface.Canvas.DrawText(fps.ToString(), 0, 10, new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.White, TextSize = 8 });
            }
            e.Surface.Canvas.DrawText(playAnalysis.T.ToString(), 20, 10, new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.White, TextSize = 8 });


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
                fpsCounter.Stop();
                patternDrawer.SetSummary(playAnalysis.Summary);
                Invalidate();
                return;
            }
            playAnalysis.Reset();
            this.patternDrawer.Clear();
            Refresh();
            fpsCounter.Start();
            playAnalysis.Start();
            fpsCounter.Start();
            Log("Started");
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (playAnalysis.IsRunning)
            {
                var key = e.KeyChar.ToString().ToUpper()[0];
                if (instrumentKeys.TryGetValue(key, out ISoundId sound))
                {
                    var t = playAnalysis.T;
                    Log?.Invoke(e.KeyChar + ": " + t);
                    playAnalysis.RegisterPlayedBeat(new Beat(sound, Velocity.Medium));
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
