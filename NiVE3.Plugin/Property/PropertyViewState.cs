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
        private string? displayName;
        /// <summary>
        /// プロパティの表示名
        /// </summary>
        public string? DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }

        private bool isEnabled;
        /// <summary>
        /// プロパティが操作可能かどうか
        /// </summary>
        public bool IsEnabled
        {
            get { return isEnabled; }
            set { SetProperty(ref isEnabled, value); }
        }

        private bool isVisible;
        /// <summary>
        /// プロパティを表示するかどうか
        /// </summary>
        public bool IsVisible
        {
            get { return isVisible; }
            set { SetProperty(ref isVisible, value); }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">プロパティのID</param>
        /// <param name="displayName">プロパティの表示名</param>
        /// <param name="isEnabled">プロパティが操作可能かどうか</param>
        /// <param name="isVisible">プロパティを表示するかどうか</param>
        public PropertyViewState(string displayName, bool isEnabled = true, bool isVisible = true)
        {
            DisplayName = displayName;
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
