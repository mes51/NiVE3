using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Input
{
    interface ISIngletonInput : IInput
    {
        static abstract IInput Instance { get; }
    }
}
