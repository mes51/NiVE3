using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Wpf.Input;
using System.Windows.Input;
using System.Text.Json.Serialization;

namespace NiVE3.Data.Config
{
    class ShortcutKeyData
    {
        public string Name { get; set; } = "";

        public string Type { get; set; } = "";

        public string KeyString { get; set; } = "";

        public string ModifierString { get; set; } = "";

        [JsonIgnore]
        public Key GestureKey
        {
            get => Enum.TryParse(KeyString, out Key key) ? key : Key.None;
            set => KeyString = value.ToString();
        }

        [JsonIgnore]
        public ModifierKeys Modifier
        {
            get => Enum.TryParse(ModifierString, out ModifierKeys modifier) ? modifier : ModifierKeys.None;
            set => ModifierString = value.ToString();
        }

        public void CorrectData()
        {
            switch (Type)
            {
                case nameof(KeyGesture) when Modifier != ModifierKeys.None && Modifier != ModifierKeys.Shift && GestureKey != Key.None:
                case nameof(SingleKeyGesture) when (Modifier == ModifierKeys.None || Modifier == ModifierKeys.Shift) && GestureKey != Key.None:
                    break;
                default:
                    Type = nameof(KeyGesture);
                    GestureKey = Key.None;
                    Modifier = ModifierKeys.None;
                    break;
            }
        }
    }
}
