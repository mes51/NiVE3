using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
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
    /// DebugMessageBox.xaml の相互作用ロジック
    /// </summary>
    public partial class DebugMessageBox : Window
    {
        public const double IconSize = 32.0;

        private static readonly DependencyPropertyKey MessagePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Message),
            typeof(string),
            typeof(DebugMessageBox),
            new FrameworkPropertyMetadata("")
        );

        private static readonly DependencyPropertyKey MessageBoxButtonPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MessageBoxButton),
            typeof(MessageBoxButton),
            typeof(DebugMessageBox),
            new FrameworkPropertyMetadata(MessageBoxButton.OK)
        );

        private static readonly DependencyPropertyKey MessageBoxIconPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MessageBoxIcon),
            typeof(BitmapSource),
            typeof(DebugMessageBox),
            new FrameworkPropertyMetadata(null)
        );

        private static readonly DependencyPropertyKey ExceptionInfoPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ExceptionInfo),
            typeof(string),
            typeof(DebugMessageBox),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty MessageProperty = MessagePropertyKey.DependencyProperty;

        public static readonly DependencyProperty MessageBoxButtonProperty = MessageBoxButtonPropertyKey.DependencyProperty;

        public static readonly DependencyProperty MessageBoxIconProperty = MessageBoxIconPropertyKey.DependencyProperty;

        public static readonly DependencyProperty ExceptionInfoProperty = ExceptionInfoPropertyKey.DependencyProperty;

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            private set { SetValue(MessagePropertyKey, value); }
        }

        public MessageBoxButton MessageBoxButton
        {
            get { return (MessageBoxButton)GetValue(MessageBoxButtonProperty); }
            private set { SetValue(MessageBoxButtonPropertyKey, value); }
        }

        public BitmapSource MessageBoxIcon
        {
            get { return (BitmapSource)GetValue(MessageBoxIconProperty); }
            private set { SetValue(MessageBoxIconPropertyKey, value); }
        }

        public string? ExceptionInfo
        {
            get { return (string)GetValue(ExceptionInfoProperty); }
            private set { SetValue(ExceptionInfoPropertyKey, value); }
        }

        public MessageBoxResult Result { get; private set; }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand YesCommand { get; }

        public ICommand NoCommand {  get; }

        SystemSound? Sound { get; set; }

        private DebugMessageBox(string message, string title, MessageBoxButton messageBoxButton, MessageBoxImage messageBoxImage, Exception? exception)
        {
            Message = message;
            Title = title;
            MessageBoxButton = messageBoxButton;

            Icon? icon = null;
            switch (messageBoxImage)
            {
                case MessageBoxImage.Error:
                    icon = SystemIcons.GetStockIcon(StockIconId.Error, (int)IconSize);
                    Sound = SystemSounds.Hand;
                    break;
                case MessageBoxImage.Warning:
                    icon = SystemIcons.GetStockIcon(StockIconId.Warning, (int)IconSize);
                    Sound = SystemSounds.Exclamation;
                    break;
                case MessageBoxImage.Information:
                    icon = SystemIcons.GetStockIcon(StockIconId.Info, (int)IconSize);
                    Sound = SystemSounds.Asterisk;
                    break;
                case MessageBoxImage.Question:
                    icon = SystemIcons.GetStockIcon(StockIconId.Help, (int)IconSize);
                    Sound = SystemSounds.Question;
                    break;
            }

            if (icon != null)
            {
                MessageBoxIcon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                icon.Dispose();
            }

            ExceptionInfo = FormatException(exception);

            OKCommand = new DelegateCommand(() =>
            {
                Result = MessageBoxResult.OK;
                Close();
            });

            CancelCommand = new DelegateCommand(() =>
            {
                Result = MessageBoxResult.Cancel;
                Close();
            });

            YesCommand = new DelegateCommand(() =>
            {
                Result = MessageBoxResult.Yes;
                Close();
            });

            NoCommand = new DelegateCommand(() =>
            {
                Result = MessageBoxResult.No;
                Close();
            });

            InitializeComponent();
        }

        private void Root_Activated(object sender, EventArgs e)
        {
            Sound?.Play();
            Sound = null;
        }

        public static MessageBoxResult Show(string message, string title = "", MessageBoxButton messageBoxButton = MessageBoxButton.OK, MessageBoxImage messageBoxImage = MessageBoxImage.None, Exception? exception = null)
        {
            var messageBox = new DebugMessageBox(message, title, messageBoxButton, messageBoxImage, exception);
            messageBox.ShowDialog();
            return messageBox.Result;
        }

        static string? FormatException(Exception? exception, int indent = 0)
        {
            if (exception == null)
            {
                return null;
            }

            var innerException = FormatException(exception.InnerException, indent + 1);

            var indentSpace = string.Join("", Enumerable.Repeat("    ", indent));
            var result = $"""
            {indentSpace}{exception.GetType().Name}: {exception.Message}

            {indentSpace}StackTrace:
            {indentSpace}{exception.StackTrace}
            """;

            if (innerException != null)
            {
                result += Environment.NewLine + $"""
                {indentSpace}InnerException:
                {innerException}
                """;
            }

            return result;
        }
    }
}
