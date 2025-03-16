using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;

namespace NiVE3.Data.Json.Project
{
    public class MaskData
    {
        public Guid MaskId { get; set; }

        public bool IsBezierPath { get; set; }

        public MaskShapeType DefaultShapeType { get; set; }

        public string Name { get; set; } = "";

        public bool IsEnabled { get; set; }

        public PropertyData? Properties { get; set; }
    }
}
