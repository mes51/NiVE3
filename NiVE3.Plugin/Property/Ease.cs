using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Property
{
    public record Ease(double Speed, double Influence)
    {
        public double Influence { get; } = Math.Min(Math.Max(0.0, Influence), 100.0);
    }
}
