using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class PropertyModel
    {
        private class ValueChangeHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue);

            PropertyModel Model { get; }

            object? OldValue { get; }

            object? NewValue { get; }

            public ValueChangeHistoryCommand(PropertyModel model, object? oldValue, object? newValue)
            {
                Model = model;
                OldValue = oldValue;
                NewValue = newValue;
            }

            public void Redo()
            {
                Model.Value = NewValue;
            }

            public void Undo()
            {
                Model.Value = OldValue;
            }

            public void Dispose() { }
        }
    }
}
