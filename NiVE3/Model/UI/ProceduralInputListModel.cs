using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model.UI
{
    class ProceduralInputListModel : BindableBase
    {
        public ProceduralInputItem[] ProceduralFootageItems { get; private set; } = [];

        FootageModel[] ProceduralFootages { get; set; } = [];

        public void SetProceduralFootages(FootageModel[] proceduralFootages)
        {
            ProceduralFootages = proceduralFootages;
            ProceduralFootageItems = [..proceduralFootages.Select(f => new ProceduralInputItem(f.Name, f.FootageId))];
        }

        public FootageModel? FindFootage(Guid footageId)
        {
            return ProceduralFootages.FirstOrDefault(f => f.FootageId == footageId);
        }
    }

    record ProceduralInputItem(string Name, Guid FootageId);
}
