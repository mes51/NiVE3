using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;

namespace NiVE3.Plugin.ValueObject
{
    public record ParentTransform(ParentType ParentType, PropertyValueGroup Transform)
    {
    }
}
