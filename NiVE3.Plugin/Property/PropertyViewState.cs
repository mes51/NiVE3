using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Property
{
    /// <summary>
    /// プロパティの表示状態等を管理します
    /// </summary>
    public class PropertyViewState : INotifyPropertyChanged
    {
        /// <summary>
        /// プロパティの元の表示名
        /// </summary>
        public string SourceDisplayName
        {
            get;
            set { SetProperty(ref field, value); }
        }

        /// <summary>
        /// プロパティが操作可能かどうか
        /// </summary>
        public bool IsEnabled
        {
            get;
            set { SetProperty(ref field, value); }
        }

        /// <summary>
        /// プロパティを表示するかどうか
        /// </summary>
        public bool IsVisible
        {
            get;
            set { SetProperty(ref field, value); }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sourceDisplayName">プロパティの元の表示名</param>
        /// <param name="isEnabled">プロパティが操作可能かどうか</param>
        /// <param name="isVisible">プロパティを表示するかどうか</param>
        public PropertyViewState(string sourceDisplayName, bool isEnabled = true, bool isVisible = true)
        {
            SourceDisplayName = sourceDisplayName;
            IsEnabled = isEnabled;
            IsVisible = isVisible;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return;
            }

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
