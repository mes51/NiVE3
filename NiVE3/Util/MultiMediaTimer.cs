using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    class MultiMediaTimer : IDisposable
    {
        /// <summary>
        /// ms
        /// </summary>
        public double Interval { get; set; }

        uint TimerId { get; set; }

        bool Started { get; set; }

        public event EventHandler<EventArgs>? Tick;

        TimeProc Proc { get; }

        long StartTime { get; set; }

        double WaitingTime { get; set; }

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

        void Start(double interval)
        {
            KillTimer();
            WaitingTime = interval;
            TimerId = NativeMethods.TimeSetEvent((uint)Interval, 0, Proc, nint.Zero, FuEvent.TIME_ONESHOT | FuEvent.TIME_CALLBACK_FUNCTION);
            Started = TimerId != 0;
            if (Started)
            {
                StartTime = Stopwatch.GetTimestamp();
            }
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
                var waitedTime = Stopwatch.GetElapsedTime(StartTime);
                var spinWait = WaitingTime * 1000.0 - waitedTime.TotalMicroseconds;
                while (spinWait > 0.0)
                {
                    SpinWait.SpinUntil(() => true);
                    waitedTime = Stopwatch.GetElapsedTime(StartTime);
                    spinWait = WaitingTime * 1000.0 - waitedTime.TotalMicroseconds;
                }

                var processStart = Stopwatch.GetTimestamp();
                Tick?.Invoke(this, EventArgs.Empty);
                var processTime = Stopwatch.GetElapsedTime(processStart);
                if (Started)
                {
                    Start(Math.Max(Interval - processTime.TotalMicroseconds * 0.001, 1.0));
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
