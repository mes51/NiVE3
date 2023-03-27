using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    class FootageViewModel : BindableBase
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        FootageModel Footage { get; }

        public FootageViewModel(FootageModel footage)
        {
            Footage = footage;
            Name = footage.Name;

            PropertyChanged += FootageViewModel_PropertyChanged;

            PropertyChangedEventManager.AddHandler(footage, FootageViewModel_PropertyChanged, nameof(Footage.Name));
        }

        private void FootageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Name):
                    Footage.Name = Name;
                    break;
            }
        }

        private void Footage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FootageModel.Name):
                    Name = Footage.Name;
                    break;
            }
        }
    }
}