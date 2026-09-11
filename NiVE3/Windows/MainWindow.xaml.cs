using System;
using System.Collections.Generic;
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
using AvalonDock;
using AvalonDock.Core;
using AvalonDock.Layout;
using AvalonDock.Serializer.Xml;
using NiVE3.Config;
using NiVE3.View.Dock;
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

        Point OriginalLocation { get; set; }

        Size OriginalSize { get; set; }

        bool IsLoadingLayout {  get; set; }

        void SaveLayout()
        {
            var serializedLayoutJson = SerializeLayout();

            if (WindowState == WindowState.Maximized)
            {
                WindowLayoutSetting.Setting.Location = OriginalLocation;
                WindowLayoutSetting.Setting.Size = OriginalSize;
            }
            else
            {
                WindowLayoutSetting.Setting.Location = new Point(Left, Top);
                WindowLayoutSetting.Setting.Size = new Size(Width, Height);
            }
            WindowLayoutSetting.Setting.WindowState = WindowState;
            WindowLayoutSetting.Setting.DockingLayoutJson = serializedLayoutJson;
            // NOTE: JSON 形式で保存した後は AvalonDock 4 系の XML レイアウトは不要になるため破棄する
            WindowLayoutSetting.Setting.DockingLayout = "";
            WindowLayoutSetting.Setting.Save();
        }

        void LoadLayout()
        {
            IsLoadingLayout = true;

            Left = WindowLayoutSetting.Setting.Location.X;
            Top = WindowLayoutSetting.Setting.Location.Y;
            Width = WindowLayoutSetting.Setting.Size.Width;
            Height = WindowLayoutSetting.Setting.Size.Height;
            if (WindowLayoutSetting.Setting.WindowState == WindowState.Maximized)
            {
                OriginalLocation = WindowLayoutSetting.Setting.Location;
                OriginalSize = WindowLayoutSetting.Setting.Size;
            }
            WindowState = WindowLayoutSetting.Setting.WindowState;

            if (!string.IsNullOrEmpty(WindowLayoutSetting.Setting.DockingLayoutJson))
            {
                RestoreDockingWindowLayout(new DockingLayoutJsonSerializer(Manager), WindowLayoutSetting.Setting.DockingLayoutJson);
            }
            else if (!string.IsNullOrEmpty(WindowLayoutSetting.Setting.DockingLayout))
            {
                // NOTE: AvalonDock 4 系で保存された XML 形式のレイアウトを読み込む
                RestoreDockingWindowLayout(new XmlLayoutSerializer(Manager), WindowLayoutSetting.Setting.DockingLayout);
            }

            IsLoadingLayout = false;
        }

        void RestoreDockingWindowLayout(LayoutSerializerBase serializer, string layout)
        {
            try
            {
                using var reader = new StringReader(layout);
                // NOTE: レイアウトのデシリアライズ時にAnchorableやDocumentを再生成され、LayoutInitializerで設定したイベントハンドラが消えるので再設定する
                //       また、Documentは起動時には存在しないため、レイアウト復元時にはパネル自体表示しないようにする
                serializer.LayoutSerializationCallback += (sender, e) =>
                {
                    if (e.Model is LayoutDocument)
                    {
                        e.Cancel = true;
                    }
                    else if (e.Content is not SingletonePaneViewModelBase && e.Model is LayoutContent layoutContent)
                    {
                        LayoutInitializer.BindClosed(() => DataContext as MainWindowViewModel, layoutContent);
                    }
                };
                serializer.Deserialize(reader);
            }
            catch { }
        }

        string SerializeLayout()
        {
            var serializer = new DockingLayoutJsonSerializer(Manager);
            using var writer = new StringWriter();
            serializer.Serialize(writer);

            return writer.ToString();
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

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (!IsLoadingLayout && WindowState == WindowState.Maximized)
            {
                OriginalLocation = new Point(Left, Top);
                OriginalSize = new Size(Width, Height);
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.InitialLayout = SerializeLayout();
            }
            LoadLayout();
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainWindowViewModel oldViewModel)
            {
                oldViewModel.ResetWindowLayoutRequest -= ViewModel_ResetWindowLayoutRequest;
            }
            if (e.NewValue is MainWindowViewModel newViewModel)
            {
                newViewModel.ResetWindowLayoutRequest += ViewModel_ResetWindowLayoutRequest;
            }
        }

        private void ViewModel_ResetWindowLayoutRequest(object? sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                IsLoadingLayout = true;

                RestoreDockingWindowLayout(new DockingLayoutJsonSerializer(Manager), viewModel.InitialLayout);

                IsLoadingLayout = false;
            }
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
