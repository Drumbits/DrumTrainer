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

namespace Drumz.UI.Desktop
{
    public delegate void MessagerHandler(string message);
    public partial class UserControl1 : SkiaSharp.Views.Desktop.SKControl
    {
        public UserControl1()
        {
            InitializeComponent();
            metronome.Tick += Metronome_Tick;
            var path = @"..\..\..\Samples\RHCP_GiveItAway1.drumz.pat.json";
            var pattern = Drumz.Common.Beats.IO.PatternIO.Load(path);
            var settings = new GridDrawer.Settings { BeatWidth = 64, FontSize = 20, LineHeight = 24 };
            patternDrawer = new PatternDrawer(pattern, settings, 2);
        }

        private void Metronome_Tick(int ellapsedTicks)
        {
            //Log?.Invoke(chrono.ElapsedMilliseconds.ToString());
            this.Invalidate();
        }

        public event MessagerHandler Log;

        private readonly System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();
        private readonly PatternDrawer patternDrawer;

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var localChrono = new System.Diagnostics.Stopwatch();
            var start = chrono.ElapsedMilliseconds;
            localChrono.Start();
            patternDrawer.Draw(e.Surface, new Common.Beats.TimeInUnits(metronome.EllapsedTicks));
            localChrono.Stop();
            Log?.Invoke(start + " " + chrono.ElapsedMilliseconds + " " + localChrono.ElapsedMilliseconds);
        }
        private readonly Metronome metronome = new Metronome(Class1.Tpm);
        protected override void OnClick(EventArgs e)
        {
            if (metronome.IsRunning)
            {
                Log("Stopping");
                metronome.Stop();
                chrono.Stop();
                return;
            }
            Log?.Invoke("Interval: " + metronome.IntervalinMilliseconds.ToString("0.0") + "ms");
            chrono.Start();
            metronome.Start();
            Log("Started");
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            metronome.Tick -= Metronome_Tick;
            metronome.Dispose();
        }
    }
}
