using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.Model;

namespace NiVE3.Data.Json.Preset
{
    public class PropertyPreset
    {
        public PropertyPresetType Type { get; set; }

        public string ParentPropertyId { get; set; } = "";

        public PropertyData? PropertyData { get; set; }
    }

    public enum PropertyPresetType
    {
        Property,
        PropertyGroup,
        AppendablePropertyChildren
    }
}
