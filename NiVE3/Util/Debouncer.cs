using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NiVE3.Util
{
    class Debouncer
    {
        public event EventHandler? Tick;

        Dispatcher Dispatcher { get; }

        DispatcherTimer Timer { get; }

        public Debouncer(int delay)
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
            Timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, delay)
            };
            Timer.Tick += Timer_Tick;
        }

        public void ResetAndStart()
        {
            Timer.Stop();
            Timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Timer.Stop();
            Dispatcher.BeginInvoke((Action)(() => Tick?.Invoke(this, EventArgs.Empty)));
        }
    }
}
