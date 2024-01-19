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

        HistoryModel HistoryModel { get; }

        public PlayControllerModel(HistoryModel historyModel)
        {
            HistoryModel = historyModel;

            historyModel.HistoryChanged += HistoryModel_HistoryChanged;
            historyModel.HistoryGroupChanging += HistoryModel_HistoryGroupChanging;
        }

        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
        }

        private void HistoryModel_HistoryChanged(object? sender, EventArgs e)
        {
            Stop();
        }

        private void HistoryModel_HistoryGroupChanging(object? sender, EventArgs e)
        {
            Stop();
        }
    }
}
