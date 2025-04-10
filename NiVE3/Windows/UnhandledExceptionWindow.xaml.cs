using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NiVE3.Util;
using Prism.Commands;

namespace NiVE3.Windows
{
    /// <summary>
    /// UnhandledExceptionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class UnhandledExceptionWindow : Window
    {
        public const double IconSize = 32.0;

        public static BitmapSource ErrorIcon { get; }

        private static readonly DependencyPropertyKey ExceptionInfoPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ExceptionInfo),
            typeof(string),
            typeof(UnhandledExceptionWindow),
            new FrameworkPropertyMetadata("")
        );

        public static readonly DependencyProperty ExceptionInfoProperty = ExceptionInfoPropertyKey.DependencyProperty;

        public string ExceptionInfo
        {
            get { return (string)GetValue(ExceptionInfoProperty); }
            private set { SetValue(ExceptionInfoPropertyKey, value); }
        }

        public ICommand CloseButton { get; }

        static UnhandledExceptionWindow()
        {
            using var icon = SystemIcons.GetStockIcon(StockIconId.Error, (int)IconSize);
            ErrorIcon = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Hand.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        public UnhandledExceptionWindow(Exception ex)
        {
            ExceptionInfo = ErrorLog.FormatExceptionMessage(ex);

            CloseButton = new DelegateCommand(() => Close());

            InitializeComponent();
        }
    }
}
