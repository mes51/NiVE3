using System;
using System.Collections.Generic;
using System.Text;
using NiVE3.Numerics;

namespace NiVE3.Plugin.ValueObject
{
    public record TwoNodeValue(Vector3d Position, Vector3d PointOfInterest)
    {
    }
}
