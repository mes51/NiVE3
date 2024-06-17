using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Data.Json.Project
{
    public class EffectData
    {
        public Guid EffectId { get; set; }

        public Guid EffectPluginId { get; set; }

        public string Name { get; set; } = "";

        public bool IsEnable { get; set; }

        public PropertyData? Properties { get; set; }
    }
}
