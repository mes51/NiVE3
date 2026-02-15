using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;

namespace NiVE3.Plugin.ValueObject
{
    public record DecomposedTransform(Vector3d Position, Vector3d Direction, Vector3d Scale);
}
