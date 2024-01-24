using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.Data.Json.Project
{
    public class ProjectData
    {
        public FootageListData FootageList { get; set; }

        public CompositionData[] Compositions { get; set; }

        [JsonConstructor]
        public ProjectData(FootageListData footageList, CompositionData[] compositions)
        {
            FootageList = footageList;
            Compositions = compositions;
        }
    }
}
