using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    class MultiMediaTimer : IDisposable
    {
        /// <summary>
        /// ms
        /// </summary>
        public int Interval { get; set; }

        uint TimerId { get; set; }

        bool Started { get; set; }

        public event EventHandler<EventArgs>? Tick;

        TimeProc Proc { get; }

        public MultiMediaTimer()
        {
            Proc = this.TimerProc;
        }

        public void Start()
        {
            Start(Interval);
        }

        public void Stop()
        {
            KillTimer();
        }

        void Start(int interval)
        {
            KillTimer();
            TimerId = NativeMethods.TimeSetEvent((uint)Interval, 0, Proc, nint.Zero, FuEvent.TIME_ONESHOT | FuEvent.TIME_CALLBACK_FUNCTION);
            Started = TimerId != 0;
        }

        void KillTimer()
        {
            if (TimerId != 0)
            {
                NativeMethods.TimeKillEvent(TimerId);
                TimerId = 0;
            }
            Started = false;
        }

        void TimerProc(uint uTimerID, uint uMsg, nint dwUser, nint dw1, nint dw2)
        {
            if (Started)
            {
                var sw = new Stopwatch();
                Tick?.Invoke(this, EventArgs.Empty);
                sw.Stop();
                if (Started)
                {
                    Start(Math.Max(Interval - sw.Elapsed.Milliseconds, 0));
                }
            }
        }

        public void Dispose()
        {
            KillTimer();
        }

        ~MultiMediaTimer()
        {
            Dispose();
        }
    }
}
