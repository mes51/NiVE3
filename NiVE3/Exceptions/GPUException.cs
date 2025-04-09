using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Exceptions
{
    class GPUException : Exception
    {
        public GPUException(Exception inner) : base(inner.Message, inner) { }
    }
}
