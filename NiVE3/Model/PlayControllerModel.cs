using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class PlayControllerModel : BindableBase
    {
        private double currentTime;
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private bool isPlaying;
        public bool IsPlaying
        {
            get { return isPlaying; }
            set { SetProperty(ref isPlaying, value); }
        }

        private bool isPaused;
        public bool IsPaused
        {
            get { return isPaused; }
            set { SetProperty(ref isPaused, value); }
        }

        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
        }
    }
}
