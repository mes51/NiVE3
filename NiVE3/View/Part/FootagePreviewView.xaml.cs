using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// FootagePreviewView.xaml の相互作用ロジック
    /// </summary>
    public partial class FootagePreviewView : UserControl
    {
        internal static readonly DependencyProperty SelectedFootagesProperty = DependencyProperty.Register(
            nameof(SelectedFootages),
            typeof(ObservableCollection<IFootageViewModel>),
            typeof(FootagePreviewView),
            new FrameworkPropertyMetadata(new ObservableCollection<IFootageViewModel>(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, SelectedItemsChanged)
        );

        private static readonly DependencyPropertyKey SelectedFirstFootagePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(SelectedFirstFootage),
            typeof(IFootageViewModel),
            typeof(FootagePreviewView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        internal static readonly DependencyProperty SelectedFirstFootageProperty = SelectedFirstFootagePropertyKey.DependencyProperty;

        internal ObservableCollection<IFootageViewModel> SelectedFootages
        {
            get { return (ObservableCollection<IFootageViewModel>)GetValue(SelectedFootagesProperty); }
            set { SetValue(SelectedFootagesProperty, value); }
        }

        internal IFootageViewModel? SelectedFirstFootage
        {
            get { return (IFootageViewModel)GetValue(SelectedFirstFootageProperty); }
            private set { SetValue(SelectedFirstFootagePropertyKey, value); }
        }

        public FootagePreviewView()
        {
            InitializeComponent();
        }

        private void SelectedFootages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SelectedFirstFootage = SelectedFootages?.FirstOrDefault();
        }

        static void SelectedItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FootagePreviewView view)
            {
                if (e.OldValue is ObservableCollection<IFootageViewModel> oldItems)
                {
                    oldItems.CollectionChanged -= view.SelectedFootages_CollectionChanged;
                }
                if (e.NewValue is ObservableCollection<IFootageViewModel> newItems)
                {
                    newItems.CollectionChanged += view.SelectedFootages_CollectionChanged;
                }
            }
        }
    }
}
