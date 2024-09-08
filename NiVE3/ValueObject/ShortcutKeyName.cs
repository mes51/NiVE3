using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.ValueObject
{
    record ShortcutKeyName(string Category, string Name, DependencyProperty Property);
}
