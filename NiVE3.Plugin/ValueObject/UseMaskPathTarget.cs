using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.ValueObject
{
    public record UseMaskPathTarget(Guid MaskId)
    {
        public static readonly UseMaskPathTarget Empty = new UseMaskPathTarget(Guid.Empty);
    }
}
