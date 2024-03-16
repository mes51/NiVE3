using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NiVE3.Util
{
    class MultiMediaTimer : IDisposable
    {
        const int InputProcessSkipCountThreshold = 10;

        const double InputProcessIntervalThreshold = 10.0;

        /// <summary>
        /// ms
        /// </summary>
        public double Interval { get; set; }

        uint TimerId { get; set; }

        bool TimerStarted { get; set; }

        public event EventHandler<EventArgs>? Tick;

        TimeProc Proc { get; }

        long StartTime { get; set; }

        double WaitingTime { get; set; }

        int InputProcessSkipCount { get; set; }

        public MultiMediaTimer()
        {
            Proc = this.TimerProc;
        }

        public void Start()
        {
            TimerStarted = true;
            Start(Interval);
        }

        public void Stop()
        {
            TimerStarted = false;
            KillTimer();
        }

        void Start(double interval)
        {
            var startTime = Stopwatch.GetTimestamp();
            KillTimer();
            if (InputProcessSkipCount >= InputProcessSkipCountThreshold)
            {
                // あまりにもintervalが短いと入力を受け付けずUIスレッドが止まったように見えるため、入力を処理できるようにする
                var yieldTime = Stopwatch.GetTimestamp();
                Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.Input);
                InputProcessSkipCount = 0;
                interval = Math.Max(interval - Stopwatch.GetElapsedTime(yieldTime).TotalMilliseconds, 1.0);
            }
            WaitingTime = interval;
            TimerId = NativeMethods.TimeSetEvent((uint)WaitingTime, 0, Proc, nint.Zero, FuEvent.TIME_ONESHOT | FuEvent.TIME_CALLBACK_FUNCTION);
            if (TimerStarted && TimerId != 0)
            {
                StartTime = startTime;
            }
            if (interval < InputProcessIntervalThreshold)
            {
                InputProcessSkipCount++;
            }
            else
            {
                InputProcessSkipCount = 0;
            }
        }

        void KillTimer()
        {
            if (TimerId != 0)
            {
                NativeMethods.TimeKillEvent(TimerId);
                TimerId = 0;
            }
        }

        void TimerProc(uint uTimerID, uint uMsg, nint dwUser, nint dw1, nint dw2)
        {
            if (TimerStarted)
            {
                var waitedTime = Stopwatch.GetElapsedTime(StartTime);
                var spinWait = WaitingTime - waitedTime.TotalMilliseconds;
                while (spinWait > 0.0)
                {
                    SpinWait.SpinUntil(() => true);
                    waitedTime = Stopwatch.GetElapsedTime(StartTime);
                    spinWait = WaitingTime - waitedTime.TotalMilliseconds;
                }

                var processStart = Stopwatch.GetTimestamp();
                Tick?.Invoke(this, EventArgs.Empty);
                var processTime = Stopwatch.GetElapsedTime(processStart);
                if (TimerStarted)
                {
                    Start(Math.Max(Interval - processTime.TotalMilliseconds + Math.Min(spinWait, 0.0), 1.0));
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        ~MultiMediaTimer()
        {
            Dispose();
        }
    }
}
