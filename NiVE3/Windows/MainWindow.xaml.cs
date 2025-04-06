using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using AvalonDock.Layout.Serialization;
using NiVE3.Config;
using NiVE3.ViewModel;

namespace NiVE3.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        void SaveLayout()
        {
            var serializer = new XmlLayoutSerializer(Manager);
            using var writer = new StringWriter();
            serializer.Serialize(writer);

            WindowLayoutSetting.Setting.Location = new Point(Left, Top);
            WindowLayoutSetting.Setting.Size = new Size(Width, Height);
            WindowLayoutSetting.Setting.WindowState = WindowState;
            WindowLayoutSetting.Setting.DockingLayout = writer.ToString();
            WindowLayoutSetting.Setting.Save();
        }

        void LoadLayout()
        {
            Left = WindowLayoutSetting.Setting.Location.X;
            Top = WindowLayoutSetting.Setting.Location.Y;
            Width = WindowLayoutSetting.Setting.Size.Width;
            Height = WindowLayoutSetting.Setting.Size.Height;
            WindowState = WindowLayoutSetting.Setting.WindowState;

            if (!string.IsNullOrEmpty(WindowLayoutSetting.Setting.DockingLayout))
            {
                try
                {
                    using var reader = new StringReader(WindowLayoutSetting.Setting.DockingLayout);
                    var serializer = new XmlLayoutSerializer(Manager);
                    serializer.Deserialize(reader);
                }
                catch { }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                if (viewModel.IsRendering)
                {
                    viewModel.StopRenderingBeforeCloseCommand.Execute(null);
                    if (!viewModel.IsForceClosing)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                else if (viewModel.IsEdited)
                {
                    viewModel.SaveProjectBeforeCloseCommand.Execute(null);
                    if (!viewModel.IsForceClosing)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            ApplicationSetting.Setting.Save();
            ShortcutKeySetting.Setting.Save();
            SaveLayout();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LoadLayout();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
