using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Drumz.Common.Utils;

namespace Drumz.Common
{
    public class Metronome : IDisposable
    {
        public delegate void TickEventHandler(int ellapsedTicks);

        private int bpm;
        private float intervalInMilliseconds;
        private int ellapsedTicks;
        private HighResolutionTimer timer;
        private readonly Mutex mutex = new Mutex();

        public event TickEventHandler Tick;


        public Metronome(int bpm)
        {
            Bpm = bpm;
        }
        public double IntervalinMilliseconds
        {
            get
            {
                return intervalInMilliseconds;
            }
        }
        public int Bpm
        {
            get
            {
                return bpm;
            }
            set
            {
                if (IsRunning)
                    throw new ArgumentException("Metronome should be spotted before changing bpm");
                bpm = value;
                intervalInMilliseconds = 60000f / bpm;
            }
        }
        public int EllapsedTicks
        {
            get
            {
                return ellapsedTicks;
            }
        }
        public bool IsRunning
        {
            get
            {
                return timer != null;
            }
        }
        public void Start()
        {
            if (timer != null) return;
            lock (mutex)
            {
                if (timer != null) return;
                timer = new HighResolutionTimer(intervalInMilliseconds);
                timer.Elapsed += OnTick;
                ellapsedTicks = 0;
                timer.Start();
            }
        }
        public void Stop()
        {
            if (timer == null) return;
            lock(mutex)
            {
                if (timer == null) return;
                timer.Stop();
                timer.Elapsed -= OnTick;
                timer = null;
            }
        }
        protected virtual void OnTick(object sender, HighResolutionTimerElapsedEventArgs e)
        {
            ++ellapsedTicks;
            Tick?.Invoke(ellapsedTicks);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
