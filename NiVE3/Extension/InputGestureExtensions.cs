using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Wpf.Input;
using System.Windows.Input;

namespace NiVE3.Extension
{
    static class InputGestureExtensions
    {
        public static bool IsSameKeyGesture(this KeyGesture source, KeyGesture target)
        {
            return source.Key == target.Key && source.Modifiers == target.Modifiers;
        }

        public static bool IsSameKeyGesture(this KeyGesture source, SingleKeyGesture target)
        {
            return source.Key == target.Key && ((source.Modifiers == ModifierKeys.Shift && target.IsUseShift) || (source.Modifiers == ModifierKeys.None && !target.IsUseShift));
        }

        public static bool IsSameKeyGesture(this SingleKeyGesture source, SingleKeyGesture target)
        {
            return source.Key == target.Key && source.IsUseShift == target.IsUseShift;
        }

        public static bool IsSameKeyGesture(this SingleKeyGesture source, KeyGesture target)
        {
            return target.IsSameKeyGesture(source);
        }

        public static bool IsSameKeyGesture(this InputGesture source, InputGesture target)
        {
            switch (source)
            {
                case KeyGesture ka:
                    {
                        if (target is KeyGesture kb)
                        {
                            return ka.IsSameKeyGesture(kb);
                        }
                        else if (target is SingleKeyGesture sb)
                        {
                            return ka.IsSameKeyGesture(sb);
                        }
                    }
                    break;
                case SingleKeyGesture sa:
                    {
                        if (target is KeyGesture kb)
                        {
                            return sa.IsSameKeyGesture(kb);
                        }
                        else if (target is SingleKeyGesture sb)
                        {
                            return sa.IsSameKeyGesture(sb);
                        }
                    }
                    break;
            }

            return false;
        }
    }
}
