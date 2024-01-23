using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Extension
{
    static class FootageSourceGroupExtension
    {
        public static IEnumerable<IFootageSource> Flatten(this FootageSourceGroup group)
        {
            return group.Sources.Concat(group.ChildrenGroup.SelectMany(c => c.Flatten()));
        }
    }
}
