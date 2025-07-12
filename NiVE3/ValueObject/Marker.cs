using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.ValueObject
{
    record Marker(Guid MarkerId, Time Time, string Name)
    {
    }
}
