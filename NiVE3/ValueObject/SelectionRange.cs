using System;
using System.Collections.Generic;
using System.Text;

namespace NiVE3.ValueObject
{
    readonly record struct SelectionRange(int Start, int Length);
}
