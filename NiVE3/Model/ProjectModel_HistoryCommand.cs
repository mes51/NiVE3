using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class ProjectModel
    {
        private class AddCompositionCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddComposition);

            ProjectModel ProjectModel { get; set; }

            CompositionModel Composition { get; set; }

            public AddCompositionCommand(ProjectModel projectModel, CompositionModel composition)
            {
                ProjectModel = projectModel;
                Composition = composition;
            }

            public void Redo()
            {
                ProjectModel.AddCompositionModel(Composition);
            }

            public void Undo()
            {
                ProjectModel.RemoveCompositionModel(Composition);
            }

            public void Dispose()
            {
                Composition.Dispose();
            }
        }
        private class DeleteCompositionCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveComposition);

            ProjectModel ProjectModel { get; set; }

            CompositionModel Composition { get; set; }

            public DeleteCompositionCommand(ProjectModel projectModel, CompositionModel composition)
            {
                ProjectModel = projectModel;
                Composition = composition;
            }

            public void Redo()
            {
                ProjectModel.RemoveCompositionModel(Composition);
            }

            public void Undo()
            {
                ProjectModel.AddCompositionModel(Composition);
            }

            public void Dispose() { }
        }
    }
}
