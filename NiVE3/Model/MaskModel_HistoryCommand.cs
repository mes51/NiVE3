using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class MaskModel
    {
        private class ChangeNameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeMaskName);

            MaskModel Model { get; }

            string OldName { get; }

            string NewName { get; }

            public ChangeNameHistoryCommand(MaskModel model, string oldName, string newName)
            {
                Model = model;
                OldName = oldName;
                NewName = newName;
            }

            public void Redo()
            {
                Model.Name = NewName;
            }

            public void Undo()
            {
                Model.Name = OldName;
            }

            public void Dispose() { }
        }
    }
}
