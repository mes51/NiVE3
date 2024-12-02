using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.PresetPlugin.Input
{
    interface IAcceralatableProceduralFootageSource : ICustomizableFootageSource
    {
        void SetupAccelerator(IAcceleratorObject accelerator);
    }
}
