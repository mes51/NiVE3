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
        private class AddCompositionHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddComposition);

            ProjectModel ProjectModel { get; }

            CompositionModel Composition { get; }

            public AddCompositionHistoryCommand(ProjectModel projectModel, CompositionModel composition)
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

        private class DeleteCompositionHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveComposition);

            ProjectModel ProjectModel { get; }

            CompositionModel Composition { get; }

            public DeleteCompositionHistoryCommand(ProjectModel projectModel, CompositionModel composition)
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

        private class ImportCompositionHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ImportProjectFile);

            ProjectModel Model { get; }

            CompositionModel[] Compositions { get; }

            public ImportCompositionHistoryCommand(ProjectModel model, CompositionModel[] compositions)
            {
                Model = model;
                Compositions = compositions;
            }

            public void Redo()
            {
                foreach (var c in Compositions)
                {
                    Model.CompositionModels.Add(c);
                }
            }

            public void Undo()
            {
                foreach (var c in Compositions)
                {
                    Model.CompositionModels.Remove(c);
                }
            }

            public void Dispose()
            {
                foreach (var c in Compositions)
                {
                    c.Dispose();
                }
            }
        }
    }
}
