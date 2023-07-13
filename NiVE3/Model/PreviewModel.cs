using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class PreviewModel : BindableBase
    {
        private bool isFootage = true;
        public bool IsFootage
        {
            get { return isFootage; }
            set { SetProperty(ref isFootage, value); }
        }

        private SourceType sourceType = SourceType.Video;
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private double duration = 60.0;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }
    }
}
