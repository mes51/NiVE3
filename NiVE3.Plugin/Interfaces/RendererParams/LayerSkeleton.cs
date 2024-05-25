using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces.RendererParams
{
    public record LayerSkeleton(Guid LayerId, SourceFootageRect rect, bool IsEnable3D, PropertyValueGroup Transform, ParentTransform[] ParentTransform)
    {
    }
}
