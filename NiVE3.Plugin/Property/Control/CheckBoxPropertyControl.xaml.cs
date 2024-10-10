using System;
using System.Collections.Generic;
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
using NiVE3.UI.Command;

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// CheckBoxPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class CheckBoxPropertyControl : PropertyControlBase
    {
        public ICommand ChangeCheckedCommand { get; }

        public CheckBoxPropertyControl()
        {
            ChangeCheckedCommand = new ActionCommand(() =>
            {
                var viewModel = ViewModel;
                if (viewModel == null)
                {
                    return;
                }

                viewModel.BeginEditCommand.Execute(null);
                viewModel.CurrentTimeRawValue = !(bool)(viewModel.CurrentTimeRawValue ?? false);
                viewModel.EndEditCommand.Execute(null);
            });

            InitializeComponent();
        }
    }
}
